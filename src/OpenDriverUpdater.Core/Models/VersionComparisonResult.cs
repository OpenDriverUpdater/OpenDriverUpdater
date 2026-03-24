namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Sürüm karşılaştırma sonucu.
/// </summary>
public sealed class VersionComparisonResult
{
    public bool IsNewer { get; init; }
    public string? InstalledVersion { get; init; }
    public string? CandidateVersion { get; init; }
    public string? Delta { get; init; }
}
