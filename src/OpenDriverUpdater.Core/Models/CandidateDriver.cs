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

    /// <summary>Kaynağa özgü referans (ör. Microsoft Catalog update GUID) — indirme URL çözümlemesi için.</summary>
    public string? SourceReference { get; set; }

    /// <summary>İndirme boyutu (bayt), biliniyorsa.</summary>
    public long? SizeBytes { get; set; }

    /// <summary>Beklenen SHA256 hash (varsa). İndirme sonrası doğrulama için kullanılır.</summary>
    public string? ExpectedSha256 { get; set; }
}
