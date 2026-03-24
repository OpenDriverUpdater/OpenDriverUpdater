using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Metadata deposu — cache, blacklist, whitelist yönetimi.
/// </summary>
public interface IMetadataStore
{
    Task<IReadOnlyList<string>> GetBlacklistedDriverIdsAsync(CancellationToken cancellationToken = default);
    Task AddToBlacklistAsync(string driverId, CancellationToken cancellationToken = default);
    Task RemoveFromBlacklistAsync(string driverId, CancellationToken cancellationToken = default);

    Task<CandidateDriver?> GetCachedDriverAsync(string hardwareId, CancellationToken cancellationToken = default);
    Task CacheDriverAsync(string hardwareId, CandidateDriver driver, CancellationToken cancellationToken = default);
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
}
