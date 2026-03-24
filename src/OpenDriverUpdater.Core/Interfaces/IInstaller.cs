using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Sürücü kurulum arayüzü.
/// </summary>
public interface IInstaller
{
    Task<InstallResult> InstallAsync(
        DownloadedPackage package,
        CancellationToken cancellationToken = default);
}
