using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Cli.Handlers;

/// <summary>
/// Güncelleme kontrolü orkestratörü.
/// </summary>
public sealed class UpdateHandler
{
    private readonly IHardwareScanner _scanner;
    private readonly IDriverResolver _resolver;
    private readonly ILoggerService _logger;

    public UpdateHandler(IHardwareScanner scanner, IDriverResolver resolver, ILoggerService logger)
    {
        _scanner = scanner;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UpdateRecommendation>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.Info("Starting update check...");
        var devices = await _scanner.ScanAsync(ct);
        var recommendations = await _resolver.ResolveAsync(devices, ct);
        _logger.Info("Update check completed: {Count} updates found", recommendations.Count);
        return recommendations;
    }
}
