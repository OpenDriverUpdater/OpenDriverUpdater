using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Reads (never writes) startup and installed-program registry entries and
/// reports those whose command points to a missing file.
/// </summary>
public interface IRegistryHealthScanner
{
    Task<RegistryScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
