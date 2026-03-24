using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Bulunan aday sürücü bilgileri.
/// </summary>
public sealed class CandidateDriver
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public string? Provider { get; set; }
    public string? DownloadUrl { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Unknown;
    public bool IsSigned { get; set; }
    public bool IsBeta { get; set; }
    public IReadOnlyList<string> SupportedHardwareIds { get; set; } = [];

    public override string ToString() => $"{Title} v{Version} ({SourceType})";
}
