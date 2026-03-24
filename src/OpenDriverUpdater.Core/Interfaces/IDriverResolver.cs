using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Birden fazla kaynaktan sürücü toplar ve en iyi adayları belirler.
/// </summary>
public interface IDriverResolver
{
    Task<IReadOnlyList<UpdateRecommendation>> ResolveAsync(
        IReadOnlyList<DeviceInfo> devices,
        CancellationToken cancellationToken = default);
}
