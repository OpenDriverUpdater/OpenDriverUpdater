using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Güncelleme önerisi — bir cihaz için bulunan en uygun sürücü karşılaştırma sonucu.
/// </summary>
public sealed class UpdateRecommendation
{
    public required string DeviceId { get; init; }
    public string? InstalledVersion { get; init; }
    public required string CandidateVersion { get; init; }
    public RecommendationType RecommendationType { get; init; }
    public TrustScore? TrustScore { get; init; }
    public string? Reason { get; init; }
    public CandidateDriver? Candidate { get; init; }
}
