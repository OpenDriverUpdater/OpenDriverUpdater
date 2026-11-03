using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Sources;

/// <summary>
/// Intel resmi sürücü kaynağı — Phase 2+ implementasyonu.
/// </summary>
public sealed class IntelDriverSource : IDriverSource
{
    private readonly ILoggerService _logger;

    public SourceType SourceType => SourceType.Intel;

    public IntelDriverSource(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<CandidateDriver>> SearchAsync(
        DeviceInfo device, CancellationToken cancellationToken = default)
    {
        // TODO: Implement Intel Driver & Support Assistant API integration
        _logger.Debug("Intel source is a stub — returning empty results for {Device}", device.Name);
        return Task.FromResult<IReadOnlyList<CandidateDriver>>([]);
    }

    public Task<string?> ResolveDownloadUrlAsync(string sourceReference, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
