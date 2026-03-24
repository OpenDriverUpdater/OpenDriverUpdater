namespace OpenDriverUpdater.Core.Enums;

/// <summary>
/// Kurulum durumu.
/// </summary>
public enum InstallStatus
{
    Success = 0,
    Failed,
    Skipped,
    RequiresRestart,
    RolledBack
}
