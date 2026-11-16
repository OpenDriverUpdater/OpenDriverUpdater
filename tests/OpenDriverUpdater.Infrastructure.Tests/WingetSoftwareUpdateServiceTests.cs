using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Infrastructure.Install;
using OpenDriverUpdater.Infrastructure.Software;
using Xunit;

namespace OpenDriverUpdater.Infrastructure.Tests;

public class WingetSoftwareUpdateServiceTests
{
    private const string Winget = @"C:\fake\winget.exe";

    private const string ListOutput =
        "Name      Id        Version Available Source\n" +
        "--------------------------------------------\n" +
        "Git       Git.Git   2.44.0  2.47.1    winget\n" +
        "7-Zip     7zip.7zip 24.08             winget\n";

    private static WingetSoftwareUpdateService Create(FakeProcessRunner runner, string? wingetPath = Winget) =>
        new(new TestLogger(), runner, () => wingetPath);

    [Fact]
    public async Task GetInstalledApps_RunsListNonInteractively()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => Task.FromResult(new ProcessRunResult(0, ListOutput, "")));
        var apps = await Create(runner).GetInstalledAppsAsync(refreshSources: false);

        Assert.Equal(2, apps.Count);
        var call = Assert.Single(runner.Calls);
        Assert.Equal(Winget, call.FileName);
        Assert.Equal("list", call.Arguments[0]);
        Assert.Contains("--accept-source-agreements", call.Arguments);
        Assert.Contains("--disable-interactivity", call.Arguments);
    }

    [Fact]
    public async Task GetInstalledApps_RefreshSources_UpdatesSourcesFirst()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => Task.FromResult(new ProcessRunResult(0, ListOutput, "")));
        await Create(runner).GetInstalledAppsAsync(refreshSources: true);

        Assert.Equal(new[] { "source", "update" }, runner.Calls[0].Arguments.Take(2).ToArray());
        Assert.Equal("list", runner.Calls[1].Arguments[0]);
    }

    [Fact]
    public async Task GetInstalledApps_NonZeroExitWithResults_StillReturnsApps()
    {
        // e.g. the msstore source failed but the winget source answered
        var runner = new FakeProcessRunner((_, _, _, _) =>
            Task.FromResult(new ProcessRunResult(unchecked((int)0x8A15000F), ListOutput, "")));
        var apps = await Create(runner).GetInstalledAppsAsync(false);
        Assert.Equal(2, apps.Count);
    }

    [Fact]
    public async Task GetInstalledApps_NoWinget_Throws()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => throw new InvalidOperationException("should not run"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(runner, wingetPath: null).GetInstalledAppsAsync(false));
        Assert.Contains("App Installer", ex.Message);
        Assert.Empty(runner.Calls);
    }

    [Fact]
    public async Task Update_UsesExactIdAndSource()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => Task.FromResult(new ProcessRunResult(0, "Successfully installed", "")));
        var app = new InstalledApp { Name = "Git", Id = "Git.Git", InstalledVersion = "2.44.0", AvailableVersion = "2.47.1", Source = "winget" };

        var result = await Create(runner).UpdateAsync(app);

        Assert.True(result.Success);
        var args = Assert.Single(runner.Calls).Arguments;
        Assert.Equal(new[] { "upgrade", "--id", "Git.Git", "--exact", "--source", "winget" }, args.Take(6).ToArray());
        Assert.Contains("--silent", args);
        Assert.Contains("--accept-package-agreements", args);
    }

    [Fact]
    public async Task Update_TruncatedId_DropsEllipsisAndExact()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => Task.FromResult(new ProcessRunResult(0, "", "")));
        var app = new InstalledApp { Name = "X", Id = "Contoso.VeryLongIdenti\u2026", AvailableVersion = "2", Source = "winget" };

        await Create(runner).UpdateAsync(app);

        var args = Assert.Single(runner.Calls).Arguments;
        Assert.Equal("Contoso.VeryLongIdenti", args[2]);
        Assert.DoesNotContain("--exact", args);
    }

    [Fact]
    public async Task Update_Failure_ReportsLastMessage()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => Task.FromResult(
            new ProcessRunResult(1603, "Downloading\n  \u2588\u2588\u2588\u2592\u2592  1 MB / 2 MB\nInstaller failed with exit code: 1603\n", "")));
        var app = new InstalledApp { Name = "Git", Id = "Git.Git", AvailableVersion = "2.47.1", Source = "winget" };

        var result = await Create(runner).UpdateAsync(app);

        Assert.False(result.Success);
        Assert.StartsWith("Installer failed with exit code: 1603", result.Message);
    }

    [Fact]
    public async Task Update_UntrackedApp_DoesNotRunWinget()
    {
        var runner = new FakeProcessRunner((_, _, _, _) => throw new InvalidOperationException("should not run"));
        var app = new InstalledApp { Name = "Legacy", Id = @"ARP\Machine\X64\Legacy", AvailableVersion = "2" };

        var result = await Create(runner).UpdateAsync(app);

        Assert.False(result.Success);
        Assert.Empty(runner.Calls);
    }
}
