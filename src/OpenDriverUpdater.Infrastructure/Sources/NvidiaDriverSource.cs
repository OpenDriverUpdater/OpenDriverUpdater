using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Sources;

/// <summary>
/// NVIDIA resmi sürücü kaynağı — Phase 2+ implementasyonu.
/// </summary>
public sealed class NvidiaDriverSource : IDriverSource
{
    private readonly ILoggerService _logger;

    public SourceType SourceType => SourceType.Nvidia;

    public NvidiaDriverSource(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<CandidateDriver>> SearchAsync(
        DeviceInfo device, CancellationToken cancellationToken = default)
    {
        // TODO: Implement NVIDIA driver API integration
        _logger.Debug("NVIDIA source is a stub — returning empty results for {Device}", device.Name);
        return Task.FromResult<IReadOnlyList<CandidateDriver>>([]);
    }
}
