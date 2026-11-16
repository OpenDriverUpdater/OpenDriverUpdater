using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Lists installed applications and updates them (implemented with winget).
/// </summary>
public interface ISoftwareUpdateService
{
    /// <summary>
    /// Lists installed applications, including any newer version available.
    /// </summary>
    /// <param name="refreshSources">Refresh the package sources first so "available" versions are current.</param>
    Task<IReadOnlyList<InstalledApp>> GetInstalledAppsAsync(
        bool refreshSources,
        CancellationToken cancellationToken = default);

    /// <summary>Updates one application to its latest available version.</summary>
    Task<SoftwareUpdateResult> UpdateAsync(
        InstalledApp app,
        CancellationToken cancellationToken = default);
}
