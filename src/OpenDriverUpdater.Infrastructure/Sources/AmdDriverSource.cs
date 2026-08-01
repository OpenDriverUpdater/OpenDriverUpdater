using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Sources;

/// <summary>
/// AMD resmi sürücü kaynağı — Phase 2+ implementasyonu.
/// </summary>
public sealed class AmdDriverSource : IDriverSource
{
    private readonly ILoggerService _logger;

    public SourceType SourceType => SourceType.Amd;

    public AmdDriverSource(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<CandidateDriver>> SearchAsync(
        DeviceInfo device, CancellationToken cancellationToken = default)
    {
        // TODO: Implement AMD driver download page integration
        _logger.Debug("AMD source is a stub — returning empty results for {Device}", device.Name);
        return Task.FromResult<IReadOnlyList<CandidateDriver>>([]);
    }
    public Task<string?> ResolveDownloadUrlAsync(string sourceReference, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
