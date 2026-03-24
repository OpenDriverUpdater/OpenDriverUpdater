using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Donanım tarayıcı arayüzü.
/// </summary>
public interface IHardwareScanner
{
    Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken = default);
}
