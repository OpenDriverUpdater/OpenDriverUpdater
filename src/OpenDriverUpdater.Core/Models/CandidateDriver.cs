using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Bulunan aday sürücü bilgileri.
/// </summary>
public sealed class CandidateDriver
{
    public required string Title { get; init; }
    public required string Version { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public string? Provider { get; init; }
    public string? DownloadUrl { get; init; }
    public SourceType SourceType { get; init; } = SourceType.Unknown;
    public bool IsSigned { get; init; }
    public bool IsBeta { get; init; }
    public IReadOnlyList<string> SupportedHardwareIds { get; init; } = [];

    public override string ToString() => $"{Title} v{Version} ({SourceType})";
}
