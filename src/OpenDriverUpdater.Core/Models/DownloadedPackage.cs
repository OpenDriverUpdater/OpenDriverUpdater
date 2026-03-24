namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// İndirilen sürücü paketi bilgileri.
/// </summary>
public sealed class DownloadedPackage
{
    public required string FilePath { get; init; }
    public string? Hash { get; init; }
    public long Size { get; init; }
    public CandidateDriver? Driver { get; init; }
}
