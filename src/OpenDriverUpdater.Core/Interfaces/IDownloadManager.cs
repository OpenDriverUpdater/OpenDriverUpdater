using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Sürücü paketi indirme yöneticisi.
/// </summary>
public interface IDownloadManager
{
    Task<DownloadedPackage> DownloadAsync(
        CandidateDriver driver,
        string destinationDir,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
