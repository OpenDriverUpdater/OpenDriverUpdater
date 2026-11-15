using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;
using OpenDriverUpdater.Core.Registry;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace OpenDriverUpdater.Infrastructure.Registry;

/// <summary>
/// READ-ONLY registry health report. Every key is opened with
/// <c>writable: false</c>; this class never creates, changes or deletes
/// anything in the registry.
///
/// Checks two kinds of entry, both shown elsewhere in Windows so the user
/// can act on them there:
///   - Startup programs (Run / RunOnce, HKLM + HKCU, 64- and 32-bit views)
///     → Settings &gt; Apps &gt; Startup / Task Manager
///   - Installed programs (Uninstall keys) whose uninstaller file is gone
///     → Settings &gt; Apps &gt; Installed apps
///
/// Only absolute paths on present, fixed local drives are judged (see
/// <see cref="RegistryHealthRules"/>); anything uncertain is left out.
/// </summary>
public sealed class WindowsRegistryHealthScanner : IRegistryHealthScanner
{
    private const string CurrentVersion = @"SOFTWARE\Microsoft\Windows\CurrentVersion";
    private const string CurrentVersion32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion";

    private readonly ILoggerService _logger;

    public WindowsRegistryHealthScanner(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<RegistryScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
#if WINDOWS
        return Task.Run(() => Scan(cancellationToken), cancellationToken);
#else
        return Task.FromResult(new RegistryScanResult(0, 0, []));
#endif
    }

#if WINDOWS
    private RegistryScanResult Scan(CancellationToken ct)
    {
        var startup = ReadStartupEntries().ToList();
        ct.ThrowIfCancellationRequested();
        var programs = ReadInstalledPrograms().ToList();
        ct.ThrowIfCancellationRequested();

        var findings = new List<RegistryFinding>();
        foreach (var entry in startup.Concat(programs))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var finding = RegistryHealthRules.Check(
                    entry, Environment.ExpandEnvironmentVariables, File.Exists, IsOnCheckableDrive);
                if (finding is not null) findings.Add(finding);
            }
            catch (Exception ex)
            {
                _logger.Debug("Skipped registry entry {Name}: {Error}", entry.Name, ex.Message);
            }
        }

        _logger.Info("Registry report: checked {Startup} startup entries and {Programs} programs, {Findings} point to missing files",
            startup.Count, programs.Count, findings.Count);

        return new RegistryScanResult(startup.Count, programs.Count, findings);
    }

    private IEnumerable<RegistryEntry> ReadStartupEntries()
    {
        var locations = new (RegistryKey Hive, string HiveName, string Path, bool Is32, string ApprovedName)[]
        {
            (Microsoft.Win32.Registry.CurrentUser,  "HKEY_CURRENT_USER",  CurrentVersion   + @"\Run",     false, "Run"),
            (Microsoft.Win32.Registry.CurrentUser,  "HKEY_CURRENT_USER",  CurrentVersion   + @"\RunOnce", false, ""),
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion   + @"\Run",     false, "Run"),
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion   + @"\RunOnce", false, ""),
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion32 + @"\Run",     true,  "Run32"),
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion32 + @"\RunOnce", true,  ""),
        };

        foreach (var loc in locations)
        {
            RegistryKey? key;
            try { key = loc.Hive.OpenSubKey(loc.Path, writable: false); }
            catch (Exception ex) { _logger.Debug("Can't open {Path}: {Error}", loc.Path, ex.Message); continue; }
            if (key is null) continue;

            using (key)
            {
                var disabled = ReadDisabledStartupNames(loc.Hive, loc.ApprovedName);
                foreach (var valueName in key.GetValueNames())
                {
                    if (string.IsNullOrEmpty(valueName)) continue;
                    // DoNotExpandEnvironmentNames: expansion is done by the rules, which
                    // adjust %ProgramFiles% for 32-bit entries first.
                    if (key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) is not string command)
                        continue;

                    yield return new RegistryEntry(
                        RegistryFindingKind.StartupEntry,
                        valueName,
                        $@"{loc.HiveName}\{loc.Path}",
                        valueName,
                        command,
                        loc.Is32,
                        disabled.Contains(valueName));
                }
            }
        }
    }

    /// <summary>
    /// Startup entries turned off in Task Manager / Settings are recorded in
    /// Explorer\StartupApproved: an odd first byte means disabled.
    /// </summary>
    private static HashSet<string> ReadDisabledStartupNames(RegistryKey hive, string approvedName)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(approvedName)) return names;

        try
        {
            using var key = hive.OpenSubKey($@"{CurrentVersion}\Explorer\StartupApproved\{approvedName}", writable: false);
            if (key is null) return names;
            foreach (var name in key.GetValueNames())
            {
                if (key.GetValue(name) is byte[] { Length: > 0 } data && (data[0] & 1) == 1)
                    names.Add(name);
            }
        }
        catch
        {
            // Informational only.
        }
        return names;
    }

    private IEnumerable<RegistryEntry> ReadInstalledPrograms()
    {
        var locations = new (RegistryKey Hive, string HiveName, string Path, bool Is32)[]
        {
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion   + @"\Uninstall", false),
            (Microsoft.Win32.Registry.LocalMachine, "HKEY_LOCAL_MACHINE", CurrentVersion32 + @"\Uninstall", true),
            (Microsoft.Win32.Registry.CurrentUser,  "HKEY_CURRENT_USER",  CurrentVersion   + @"\Uninstall", false),
        };

        foreach (var loc in locations)
        {
            RegistryKey? root;
            try { root = loc.Hive.OpenSubKey(loc.Path, writable: false); }
            catch (Exception ex) { _logger.Debug("Can't open {Path}: {Error}", loc.Path, ex.Message); continue; }
            if (root is null) continue;

            using (root)
            {
                foreach (var subName in root.GetSubKeyNames())
                {
                    RegistryEntry? entry = null;
                    try
                    {
                        using var sub = root.OpenSubKey(subName, writable: false);
                        if (sub is null) continue;

                        // Only entries Windows actually lists under Installed apps.
                        if (sub.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name)) continue;
                        if (sub.GetValue("SystemComponent") is int sc && sc == 1) continue;
                        if (sub.GetValue("ParentKeyName") is string) continue;   // update/patch of another product
                        if (sub.GetValue("UninstallString", null, RegistryValueOptions.DoNotExpandEnvironmentNames)
                            is not string uninstall || string.IsNullOrWhiteSpace(uninstall)) continue;

                        entry = new RegistryEntry(
                            RegistryFindingKind.InstalledProgram,
                            name.Trim(),
                            $@"{loc.HiveName}\{loc.Path}\{subName}",
                            "UninstallString",
                            uninstall,
                            loc.Is32);
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("Skipped uninstall key {Key}: {Error}", subName, ex.Message);
                    }

                    if (entry is not null) yield return entry;
                }
            }
        }
    }

    /// <summary>Only present, fixed local drives: never removable, optical or network.</summary>
    private static bool IsOnCheckableDrive(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal)) return false;
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root)) return false;
            var drive = new DriveInfo(root);
            return drive.IsReady && drive.DriveType == DriveType.Fixed;
        }
        catch
        {
            return false;
        }
    }
#endif
}
