namespace OpenDriverUpdater.Core.Dtos;

using OpenDriverUpdater.Core.Models;

/// <summary>
/// Güncelleme kontrolü sonucu DTO'su.
/// </summary>
public sealed class UpdateCheckResultDto
{
    public IReadOnlyList<UpdateRecommendation> Recommendations { get; init; } = [];
    public int AvailableUpdates => Recommendations.Count(r => r.RecommendationType != Enums.RecommendationType.Skip);
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}
