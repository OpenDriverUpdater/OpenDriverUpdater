using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Güncelleme önerisi — bir cihaz için bulunan en uygun sürücü karşılaştırma sonucu.
/// </summary>
public sealed class UpdateRecommendation
{
    public string DeviceId { get; set; } = string.Empty;
    public string? InstalledVersion { get; set; }
    public string CandidateVersion { get; set; } = string.Empty;
    public RecommendationType RecommendationType { get; set; }
    public TrustScore? TrustScore { get; set; }
    public string? Reason { get; set; }
    public CandidateDriver? Candidate { get; set; }

    /// <summary>Birincil aday çözümlenemezse denenecek yedek adaylar (güven puanına göre sıralı).</summary>
    public IReadOnlyList<CandidateDriver> Alternates { get; set; } = [];
}
