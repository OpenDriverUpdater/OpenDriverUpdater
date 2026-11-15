using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// A registry value read for the health report (raw data, before checking).
/// </summary>
/// <param name="Kind">What the entry is.</param>
/// <param name="Name">Display name (startup value name, or the program's DisplayName).</param>
/// <param name="KeyPath">Full key path, e.g. HKEY_CURRENT_USER\Software\...\Run.</param>
/// <param name="ValueName">Value holding the command (Run value name, or "UninstallString").</param>
/// <param name="Command">The command line stored in the value, unexpanded.</param>
/// <param name="Is32BitView">Read from WOW6432Node: the command runs as a 32-bit program.</param>
/// <param name="IsDisabled">Startup entry turned off in Task Manager / Settings.</param>
public sealed record RegistryEntry(
    RegistryFindingKind Kind,
    string Name,
    string KeyPath,
    string ValueName,
    string Command,
    bool Is32BitView = false,
    bool IsDisabled = false);
