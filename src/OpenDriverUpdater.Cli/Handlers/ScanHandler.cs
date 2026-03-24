using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Cli.Handlers;

/// <summary>
/// Tarama iş akışı orkestratörü.
/// </summary>
public sealed class ScanHandler
{
    private readonly IHardwareScanner _scanner;
    private readonly ILoggerService _logger;

    public ScanHandler(IHardwareScanner scanner, ILoggerService logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceInfo>> ExecuteAsync(CancellationToken ct = default)
    {
        _logger.Info("Starting hardware scan...");
        var devices = await _scanner.ScanAsync(ct);
        _logger.Info("Scan completed: {Count} devices", devices.Count);
        return devices;
    }
}
