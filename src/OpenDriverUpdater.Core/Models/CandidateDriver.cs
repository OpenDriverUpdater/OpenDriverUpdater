using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

public sealed class CandidateDriver
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public string? Provider { get; set; }
    public string? DownloadUrl { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Unknown;
    public bool SourceReportsSigned { get; set; }
    public bool IsBeta { get; set; }
    public IReadOnlyList<string> SupportedHardwareIds { get; set; } = [];
    public HardwareIdMatchBasis MatchBasis { get; set; } = HardwareIdMatchBasis.QueryOnly;

    public override string ToString() => $"{Title} v{Version} ({SourceType})";

    public string? SourceReference { get; set; }
    public long? SizeBytes { get; set; }
    public string? ExpectedSha256 { get; set; }
}
