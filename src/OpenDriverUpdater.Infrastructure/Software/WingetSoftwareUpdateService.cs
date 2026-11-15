using System.Text;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Software;
using OpenDriverUpdater.Infrastructure.Install;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace OpenDriverUpdater.Infrastructure.Software;

/// <summary>
/// Lists installed applications and updates them with winget (Windows Package
/// Manager, part of "App Installer").
///
/// <c>winget list</c> reports every installed app (Add/Remove Programs and
/// Store apps). Apps winget can match to a source (winget, msstore) carry a
/// Source and, when newer, an Available version. Apps with no Source aren't
/// tracked by winget, so their update status is unknown.
///
/// Only one winget operation runs at a time: installers (MSI in particular)
/// can't run concurrently.
/// </summary>
public sealed class WingetSoftwareUpdateService : ISoftwareUpdateService
{
    private static readonly TimeSpan SourceUpdateTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ListTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan UpgradeTimeout = TimeSpan.FromMinutes(30);

    private static readonly string[] NonInteractive = ["--accept-source-agreements", "--disable-interactivity"];

    private readonly ILoggerService _logger;
    private readonly IProcessRunner _runner;
    private readonly Func<string?> _resolveWinget;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public WingetSoftwareUpdateService(ILoggerService logger)
        : this(logger, new SystemProcessRunner(new UTF8Encoding(false)), ResolveWingetPath)
    {
    }

    public WingetSoftwareUpdateService(ILoggerService logger, IProcessRunner runner, Func<string?> resolveWinget)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _resolveWinget = resolveWinget ?? throw new ArgumentNullException(nameof(resolveWinget));
    }

    public async Task<IReadOnlyList<InstalledApp>> GetInstalledAppsAsync(
        bool refreshSources,
        CancellationToken cancellationToken = default)
    {
        var winget = RequireWinget();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (refreshSources)
            {
                _logger.Info("Refreshing winget sources");
                try
                {
                    var src = await _runner.RunAsync(winget, ["source", "update", .. NonInteractive],
                        SourceUpdateTimeout, cancellationToken).ConfigureAwait(false);
                    if (src.ExitCode != 0)
                        _logger.Warn("winget source update exited with {Code}: {Msg}", src.ExitCode, LastMessage(src));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Stale sources still give a usable answer.
                    _logger.Warn("winget source update failed: {Error}", ex.Message);
                }
            }

            _logger.Info("Listing installed applications with winget");
            var result = await _runner.RunAsync(winget, ["list", .. NonInteractive],
                ListTimeout, cancellationToken).ConfigureAwait(false);

            var apps = WingetTableParser.Parse(result.StdOut).ToList();

            // winget exits non-zero when one source (often msstore) fails but
            // still prints results from the others, so only fail on no output.
            if (apps.Count == 0 && result.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"winget couldn't list installed apps: {LastMessage(result)} (exit code 0x{result.ExitCode:X8})");
            }

            if (result.ExitCode != 0)
                _logger.Warn("winget list exited with 0x{Code:X8} but returned {Count} apps", result.ExitCode, apps.Count);

            ExpandTruncatedNames(apps);

            _logger.Info("winget reported {Count} installed apps, {Updates} with updates",
                apps.Count, apps.Count(a => a.HasUpdate));
            return apps;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SoftwareUpdateResult> UpdateAsync(
        InstalledApp app,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.HasUpdate)
            return new SoftwareUpdateResult(false, "No update is available for this app.", -1);
        if (!app.IsTracked)
            return new SoftwareUpdateResult(false, "winget doesn't know where this app comes from, so it can't update it.", -1);

        var winget = RequireWinget();

        // winget shortens long IDs in its table. A shortened ID can't be
        // matched exactly, so it's passed without --exact; winget then
        // matches on the prefix and refuses if more than one package matches.
        var args = new List<string> { "upgrade", "--id" };
        if (app.IsIdTruncated)
        {
            args.Add(WingetTextUtil.StripEllipsis(app.Id));
        }
        else
        {
            args.Add(app.Id);
            args.Add("--exact");
        }
        args.AddRange(["--source", app.Source!, "--silent", "--accept-package-agreements", .. NonInteractive]);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.Info("Updating {Name} ({Id}) {From} -> {To}", app.Name, app.Id, app.InstalledVersion ?? "?", app.AvailableVersion!);
            var result = await _runner.RunAsync(winget, args, UpgradeTimeout, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode == 0)
            {
                _logger.Info("Updated {Name} to {Version}", app.Name, app.AvailableVersion!);
                return new SoftwareUpdateResult(true, $"Updated to {app.AvailableVersion}", 0);
            }

            var message = $"{LastMessage(result)} (exit code 0x{result.ExitCode:X8})";
            _logger.Warn("Updating {Name} failed: {Message}", app.Name, message);
            return new SoftwareUpdateResult(false, message, result.ExitCode);
        }
        catch (TimeoutException)
        {
            return new SoftwareUpdateResult(false, $"The update didn't finish within {UpgradeTimeout.TotalMinutes:0} minutes.", -1);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string RequireWinget() =>
        _resolveWinget() ?? throw new InvalidOperationException(
            "winget (Windows Package Manager) wasn't found. Install or update \"App Installer\" from the Microsoft Store, then try again.");

    /// <summary>Last line of winget output that is an actual message (not a progress bar or spinner).</summary>
    internal static string LastMessage(ProcessRunResult result)
    {
        var lines = WingetTableParser.SplitLines(result.StdOut + "\n" + result.StdErr);
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.Length <= 1) continue;                                   // spinner frame
            if (line.Contains('█') || line.Contains('▒')) continue; // progress bar
            return line;
        }
        return "winget returned no message";
    }

    /// <summary>
    /// Finds winget. Elevated processes often don't have the per-user
    /// WindowsApps folder on PATH, so known locations are checked first.
    /// </summary>
    public static string? ResolveWingetPath()
    {
        if (!OperatingSystem.IsWindows()) return null;

        var alias = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (File.Exists(alias)) return alias;

        try
        {
            var windowsApps = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            var latest = Directory.EnumerateDirectories(windowsApps, "Microsoft.DesktopAppInstaller_*_8wekyb3d8bbwe")
                .Select(dir => (Dir: dir, Version: ParsePackageVersion(Path.GetFileName(dir))))
                .Where(x => File.Exists(Path.Combine(x.Dir, "winget.exe")))
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();
            if (latest.Dir is not null) return Path.Combine(latest.Dir, "winget.exe");
        }
        catch
        {
            // WindowsApps is ACL-restricted; fall through to PATH lookup.
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "winget.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { /* malformed PATH entry */ }
        }

        return null;
    }

    private static Version ParsePackageVersion(string folderName)
    {
        // Microsoft.DesktopAppInstaller_1.24.25200.0_x64__8wekyb3d8bbwe
        var parts = folderName.Split('_');
        return parts.Length > 1 && Version.TryParse(parts[1], out var v) ? v : new Version(0, 0);
    }

    /// <summary>
    /// winget shortens long names to fit its table ("Microsoft Visual C++ 2015-2022 Redist…").
    /// Restore the full name from Add/Remove Programs when exactly one entry matches.
    /// </summary>
    private void ExpandTruncatedNames(List<InstalledApp> apps)
    {
#if WINDOWS
        if (!apps.Any(a => WingetTextUtil.IsTruncated(a.Name))) return;

        HashSet<string> names;
        try
        {
            names = ReadUninstallDisplayNames();
        }
        catch (Exception ex)
        {
            _logger.Warn("Couldn't read installed program names from the registry: {Error}", ex.Message);
            return;
        }

        foreach (var app in apps.Where(a => WingetTextUtil.IsTruncated(a.Name)))
        {
            var prefix = WingetTextUtil.StripEllipsis(app.Name);
            var matches = names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if (matches.Count == 1)
                app.Name = matches[0];
        }
#endif
    }

#if WINDOWS
    private static HashSet<string> ReadUninstallDisplayNames()
    {
        const string uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        const string uninstall32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hive, path) in new[]
                 {
                     (Microsoft.Win32.Registry.LocalMachine, uninstall),
                     (Microsoft.Win32.Registry.LocalMachine, uninstall32),
                     (Microsoft.Win32.Registry.CurrentUser, uninstall),
                 })
        {
            // Read-only access; nothing in the registry is modified.
            using var root = hive.OpenSubKey(path, writable: false);
            if (root is null) continue;

            foreach (var subName in root.GetSubKeyNames())
            {
                using var sub = root.OpenSubKey(subName, writable: false);
                if (sub?.GetValue("DisplayName") is string name && !string.IsNullOrWhiteSpace(name))
                    names.Add(name.Trim());
            }
        }
        return names;
    }
#endif
}
