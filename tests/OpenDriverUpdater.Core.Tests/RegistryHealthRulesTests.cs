using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Registry;
using Xunit;

namespace OpenDriverUpdater.Core.Tests;

public class CommandLinePathTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\" --tray", @"C:\Program Files\App\app.exe")]
    [InlineData("C:\\Program Files\\App\\app.exe -minimized", @"C:\Program Files\App\app.exe")]
    [InlineData("\"C:\\Program Files\\App\\app.exe", @"C:\Program Files\App\app.exe")]
    [InlineData("rundll32.exe C:\\Windows\\System32\\foo.dll,Start", @"C:\Windows\System32\foo.dll")]
    [InlineData("C:\\Windows\\System32\\rundll32.exe \"C:\\Program Files\\X\\x.dll\",Run", @"C:\Program Files\X\x.dll")]
    [InlineData("C:\\Windows\\system32\\rundll32 C:\\x\\y.dll,E", @"C:\x\y.dll")]
    [InlineData("\\\\server\\share\\app.exe /s", @"\\server\share\app.exe")]
    [InlineData("C:\\ProgramData\\Package Cache\\{1234}\\setup.exe /uninstall", @"C:\ProgramData\Package Cache\{1234}\setup.exe")]
    public void Extracts_AbsoluteProgramPath(string command, string expected)
    {
        Assert.Equal(expected, CommandLinePath.ExtractProgramPath(command));
    }

    [Theory]
    [InlineData("MsiExec.exe /X{0A1B2C3D-0000-0000-0000-000000000000}")]   // Windows Installer: resolved by Windows
    [InlineData("ctfmon.exe")]                                                // bare name: found via system folders
    [InlineData("rundll32 shell32.dll,Control_RunDLL")]                       // bare DLL name
    [InlineData("C:\\Tools\\tool -cfg C:\\x\\a.cmd")]                         // ambiguous: don't guess
    [InlineData("\"\" -x")]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsNull_WhenPathCannotBeJudged(string command)
    {
        Assert.Null(CommandLinePath.ExtractProgramPath(command));
    }
}

public class RegistryHealthRulesTests
{
    private static RegistryEntry Startup(string command, bool is32 = false) =>
        new(RegistryFindingKind.StartupEntry, "App", @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "App", command, is32);

    private static string Expand(string s) => s
        .Replace("%ProgramFiles(x86)%", @"C:\Program Files (x86)", StringComparison.OrdinalIgnoreCase)
        .Replace("%ProgramFiles%", @"C:\Program Files", StringComparison.OrdinalIgnoreCase)
        .Replace("%SystemRoot%", @"C:\Windows", StringComparison.OrdinalIgnoreCase);

    private static bool LocalDrive(string path) => path.StartsWith("C:", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void MissingFile_IsReported()
    {
        var finding = RegistryHealthRules.Check(Startup("\"C:\\Gone\\app.exe\" -x"), Expand, _ => false, LocalDrive);
        Assert.NotNull(finding);
        Assert.Equal(@"C:\Gone\app.exe", finding!.MissingPath);
    }

    [Fact]
    public void ExistingFile_IsNotReported()
    {
        Assert.Null(RegistryHealthRules.Check(Startup(@"C:\App\app.exe"), Expand, _ => true, LocalDrive));
    }

    [Fact]
    public void RemovableOrNetworkDrive_IsNeverReported()
    {
        Assert.Null(RegistryHealthRules.Check(Startup(@"E:\Portable\app.exe"), Expand, _ => false, LocalDrive));
        Assert.Null(RegistryHealthRules.Check(Startup(@"\\nas\apps\app.exe"), Expand, _ => false, LocalDrive));
    }

    [Fact]
    public void BareProgramName_IsNotReported()
    {
        Assert.Null(RegistryHealthRules.Check(Startup("ctfmon.exe"), Expand, _ => false, LocalDrive));
    }

    [Fact]
    public void EnvironmentVariables_AreExpanded()
    {
        var finding = RegistryHealthRules.Check(Startup(@"%ProgramFiles%\App\app.exe"), Expand, _ => false, LocalDrive);
        Assert.Equal(@"C:\Program Files\App\app.exe", finding!.MissingPath);
    }

    [Fact]
    public void ThirtyTwoBit_ProgramFilesMeansX86()
    {
        var finding = RegistryHealthRules.Check(Startup(@"%ProgramFiles%\App\app.exe", is32: true), Expand, _ => false, LocalDrive);
        Assert.Equal(@"C:\Program Files (x86)\App\app.exe", finding!.MissingPath);
    }

    [Fact]
    public void ThirtyTwoBit_System32_IsRedirectedToSysWow64()
    {
        bool Exists(string p) => p.Equals(@"C:\Windows\SysWOW64\tool.exe", StringComparison.OrdinalIgnoreCase);
        Assert.Null(RegistryHealthRules.Check(Startup(@"C:\Windows\System32\tool.exe", is32: true), Expand, Exists, LocalDrive));
        Assert.NotNull(RegistryHealthRules.Check(Startup(@"C:\Windows\System32\tool.exe", is32: false), Expand, Exists, LocalDrive));
    }
}
