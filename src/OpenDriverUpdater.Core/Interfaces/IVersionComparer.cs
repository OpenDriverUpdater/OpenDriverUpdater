using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Sürüm karşılaştırma arayüzü.
/// </summary>
public interface IVersionComparer
{
    VersionComparisonResult Compare(string? installedVersion, string? candidateVersion);
}
