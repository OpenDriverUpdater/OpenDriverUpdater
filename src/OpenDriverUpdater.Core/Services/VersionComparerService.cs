using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Services;

/// <summary>
/// Semantik ve tarih tabanlı sürüm karşılaştırma servisi.
/// </summary>
public sealed class VersionComparerService : IVersionComparer
{
    public VersionComparisonResult Compare(string? installedVersion, string? candidateVersion)
    {
        if (string.IsNullOrWhiteSpace(candidateVersion))
        {
            return new VersionComparisonResult
            {
                IsNewer = false,
                InstalledVersion = installedVersion,
                CandidateVersion = candidateVersion,
                Delta = "Candidate version is empty"
            };
        }

        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            return new VersionComparisonResult
            {
                IsNewer = true,
                InstalledVersion = installedVersion,
                CandidateVersion = candidateVersion,
                Delta = "No installed version found"
            };
        }

        var installed = NormalizeVersion(installedVersion);
        var candidate = NormalizeVersion(candidateVersion);

        if (installed is null || candidate is null)
        {
            var cmp = string.Compare(installedVersion, candidateVersion, StringComparison.OrdinalIgnoreCase);
            return new VersionComparisonResult
            {
                IsNewer = cmp < 0,
                InstalledVersion = installedVersion,
                CandidateVersion = candidateVersion,
                Delta = cmp < 0 ? "Newer (string comparison)" : "Same or older"
            };
        }

        var comparison = candidate.CompareTo(installed);
        return new VersionComparisonResult
        {
            IsNewer = comparison > 0,
            InstalledVersion = installedVersion,
            CandidateVersion = candidateVersion,
            Delta = comparison > 0
                ? $"{installed} → {candidate}"
                : comparison == 0 ? "Same version" : "Older"
        };
    }

    private static Version? NormalizeVersion(string version)
    {
        var clean = version.TrimStart('v', 'V').Trim();
        return Version.TryParse(clean, out var result) ? result : null;
    }
}
