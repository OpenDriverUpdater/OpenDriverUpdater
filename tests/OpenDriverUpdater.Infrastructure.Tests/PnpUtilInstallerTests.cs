using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Infrastructure.Install;
using Xunit;

namespace OpenDriverUpdater.Infrastructure.Tests;

public sealed class PnpUtilInstallerTests
{
    private readonly TestLogger _logger = new();

    [Fact]
    public async Task InstallAsync_InfFile_UsesPnpUtilArgumentListAndTrustedSystemPath()
    {
        using var temp = new TempDirectory("driver package with spaces");
        var infPath = temp.File("driver.inf");
        File.WriteAllText(infPath, "; fake inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromResult(new ProcessRunResult(
                0,
                "Total driver packages: 1\nAdded driver packages: 1\n",
                string.Empty)));

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = infPath });

        Assert.True(result.Success);
        Assert.Equal(InstallStatus.Success, result.Status);
        Assert.Single(runner.Calls);

        var call = runner.Calls[0];
        Assert.Equal("pnputil.exe", Path.GetFileName(call.FileName));
        Assert.Equal(Environment.SystemDirectory, Path.GetDirectoryName(call.FileName));
        Assert.Equal(
            new[] { "/add-driver", infPath, "/subdirs", "/install" },
            call.Arguments);
        Assert.Equal(TimeSpan.FromMinutes(10), call.Timeout);
    }

    [Fact]
    public async Task InstallAsync_CabFile_UsesExpandThenPnpUtilWithSeparateArguments()
    {
        using var temp = new TempDirectory("cab package with spaces");
        var cabPath = temp.File("driver package.cab");
        File.WriteAllBytes(cabPath, new byte[] { 1, 2, 3 });
        var extractDir = Path.Combine(temp.Path, "driver package");

        var runner = new FakeProcessRunner((fileName, args, _, _) =>
        {
            if (Path.GetFileName(fileName).Equals("expand.exe", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(new[] { "-F:*", cabPath, extractDir }, args);
                File.WriteAllText(Path.Combine(args[2], "driver.inf"), "; fake inf");
                File.WriteAllText(Path.Combine(args[2], "driver.cat"), "; fake catalog");
                return Task.FromResult(new ProcessRunResult(0, "expanded", string.Empty));
            }

            return Task.FromResult(new ProcessRunResult(
                0,
                "Total driver packages: 1\nAdded driver packages: 1\n",
                string.Empty));
        });

        var signatureValidator = new FakeSignatureValidator();
        var installer = new PnpUtilInstaller(_logger, runner, signatureValidator);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = cabPath });

        Assert.True(result.Success);
        Assert.Equal(2, runner.Calls.Count);
        Assert.Single(signatureValidator.Calls);
        Assert.EndsWith(".cat", signatureValidator.Calls[0], StringComparison.OrdinalIgnoreCase);

        var expandCall = runner.Calls[0];
        Assert.Equal("expand.exe", Path.GetFileName(expandCall.FileName));
        Assert.Equal(Environment.SystemDirectory, Path.GetDirectoryName(expandCall.FileName));
        Assert.Equal(new[] { "-F:*", cabPath, extractDir }, expandCall.Arguments);
        Assert.Equal(TimeSpan.FromMinutes(2), expandCall.Timeout);

        var pnpCall = runner.Calls[1];
        Assert.Equal("pnputil.exe", Path.GetFileName(pnpCall.FileName));
        Assert.Equal(
            new[] { "/add-driver", Path.Combine(extractDir, "*.inf"), "/subdirs", "/install" },
            pnpCall.Arguments);
        Assert.Equal(TimeSpan.FromMinutes(10), pnpCall.Timeout);
    }

    [Fact]
    public async Task InstallAsync_CabWithoutCatalog_FailsClosedAndRefusesInstall()
    {
        using var temp = new TempDirectory("catless cab");
        var cabPath = temp.File("catless.cab");
        File.WriteAllBytes(cabPath, new byte[] { 1, 2, 3 });

        var runner = new FakeProcessRunner((fileName, args, _, _) =>
        {
            if (Path.GetFileName(fileName).Equals("expand.exe", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(Path.Combine(args[2], "driver.inf"), "; fake inf");
                return Task.FromResult(new ProcessRunResult(0, "expanded", string.Empty));
            }

            throw new InvalidOperationException("pnputil must not run without a validated catalog.");
        });

        var signatureValidator = new FakeSignatureValidator();
        var installer = new PnpUtilInstaller(_logger, runner, signatureValidator);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = cabPath });

        Assert.False(result.Success);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains(
            "no Authenticode",
            result.Message ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.Single(runner.Calls);
        Assert.Empty(signatureValidator.Calls);
    }

    [Fact]
    public async Task InstallAsync_CatalogSignatureInvalid_FailsClosedAndRefusesInstall()
    {
        using var temp = new TempDirectory();
        var cabPath = temp.File("forsigned.cab");
        File.WriteAllBytes(cabPath, new byte[] { 1, 2, 3 });

        var runner = new FakeProcessRunner((fileName, args, _, _) =>
        {
            if (Path.GetFileName(fileName).Equals("expand.exe", StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(Path.Combine(args[2], "driver.inf"), "; fake inf");
                File.WriteAllText(Path.Combine(args[2], "driver.cat"), "; fake catalog");
                return Task.FromResult(new ProcessRunResult(0, "expanded", string.Empty));
            }

            throw new InvalidOperationException("pnputil must not run after signature validation failed.");
        });

        var signatureValidator = new FakeSignatureValidator { OnValidate = _ => false };
        var installer = new PnpUtilInstaller(_logger, runner, signatureValidator);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = cabPath });

        Assert.False(result.Success);
        Assert.Contains("Signature validation failed", result.Message ?? string.Empty);
        Assert.Single(runner.Calls);
    }

    [Fact]
    public async Task InstallAsync_ExpandFailure_ReturnsFailureAndDoesNotCallPnpUtil()
    {
        using var temp = new TempDirectory();
        var cabPath = temp.File("broken.cab");
        File.WriteAllBytes(cabPath, new byte[] { 1, 2, 3 });

        var runner = new FakeProcessRunner((fileName, _, _, _) =>
        {
            Assert.Equal("expand.exe", Path.GetFileName(fileName));
            return Task.FromResult(new ProcessRunResult(1, string.Empty, "bad cab"));
        });

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = cabPath });

        Assert.False(result.Success);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("bad cab", result.Message);
        Assert.Single(runner.Calls);
    }

    [Fact]
    public async Task InstallAsync_PnpUtilFailure_ReturnsFailureAndIncludesStdErr()
    {
        using var temp = new TempDirectory();
        var infPath = temp.File("driver.inf");
        File.WriteAllText(infPath, "; fake inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromResult(new ProcessRunResult(
                1,
                "Total driver packages: 1\nAdded driver packages: 0\n",
                "Access denied")));

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = infPath });

        Assert.False(result.Success);
        Assert.Equal(InstallStatus.Failed, result.Status);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Access denied", result.Message);
    }

    [Fact]
    public async Task InstallAsync_RebootRequired_ReturnsRequiresRestart()
    {
        using var temp = new TempDirectory();
        var infPath = temp.File("driver.inf");
        File.WriteAllText(infPath, "; fake inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromResult(new ProcessRunResult(3010, "A restart is required.", string.Empty)));

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = infPath });

        Assert.True(result.Success);
        Assert.True(result.RequiresRestart);
        Assert.Equal(InstallStatus.RequiresRestart, result.Status);
    }

    [Fact]
    public async Task InstallAsync_Cancellation_Propagates()
    {
        using var temp = new TempDirectory();
        var infPath = temp.File("driver.inf");
        File.WriteAllText(infPath, "; fake inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromException<ProcessRunResult>(new OperationCanceledException()));

        var installer = new PnpUtilInstaller(_logger, runner);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => installer.InstallAsync(new DownloadedPackage { FilePath = infPath }));
    }

    [Fact]
    public async Task InstallAsync_Timeout_ReturnsFailure()
    {
        using var temp = new TempDirectory();
        var infPath = temp.File("driver.inf");
        File.WriteAllText(infPath, "; fake inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromException<ProcessRunResult>(new TimeoutException("Process timed out.")));

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = infPath });

        Assert.False(result.Success);
        Assert.Contains("timed out", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InstallAsync_MissingFile_DoesNotStartProcess()
    {
        using var temp = new TempDirectory();
        var missing = temp.File("missing.inf");

        var runner = new FakeProcessRunner((_, _, _, _) =>
            throw new InvalidOperationException("No process should be started for a missing file."));

        var installer = new PnpUtilInstaller(_logger, runner);

        var result = await installer.InstallAsync(new DownloadedPackage { FilePath = missing });

        Assert.False(result.Success);
        Assert.Equal(-1, result.ExitCode);
        Assert.Empty(runner.Calls);
    }

    [Fact]
    public void SystemProcessRunner_CreateStartInfo_UsesArgumentListAndPreservesSpecialCharacters()
    {
        var startInfo = SystemProcessRunner.CreateStartInfo(
            "pnputil.exe",
            new[]
            {
                "/add-driver",
                @"C:\Program Files\Sürücü Paketi\driver.inf",
                "/subdirs",
                "/install",
            });

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal("pnputil.exe", startInfo.FileName);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.Equal(4, startInfo.ArgumentList.Count);
        Assert.Equal(@"C:\Program Files\Sürücü Paketi\driver.inf", startInfo.ArgumentList[1]);
    }
}
