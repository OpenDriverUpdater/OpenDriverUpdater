using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Sürücü kurulum sonucu.
/// </summary>
public sealed class InstallResult
{
    public bool Success { get; init; }
    public int ExitCode { get; init; }
    public string? Message { get; init; }
    public bool RequiresRestart { get; init; }
    public InstallStatus Status { get; init; }
}
