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

        // Yüklü sürüm bilinmiyorsa güncelleme ÖNERME: "bilinmeyen" durumda
        // rastgele sürücü yüklemek, hiç dokunmamaktan daha riskli.
        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            return new VersionComparisonResult
            {
                IsNewer = false,
                InstalledVersion = installedVersion,
                CandidateVersion = candidateVersion,
                Delta = "Installed version unknown — not recommending"
            };
        }

        var installed = NormalizeVersion(installedVersion);
        var candidate = NormalizeVersion(candidateVersion);

        // Herhangi biri sürüm olarak çözümlenemiyorsa karşılaştırma yapılamaz.
        // Alfabetik karşılaştırma anlamsız sonuç verir ("n/a" > "10.0..." gibi) —
        // güvenli varsayılan: önerme.
        if (installed is null || candidate is null)
        {
            return new VersionComparisonResult
            {
                IsNewer = false,
                InstalledVersion = installedVersion,
                CandidateVersion = candidateVersion,
                Delta = "Version not comparable"
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