using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Services;

/// <summary>
/// Çok kriterli güven puanı değerlendiricisi.
/// </summary>
public sealed class TrustEvaluatorService : ITrustEvaluator
{
    private const int MaxPoints = 100;

    public TrustScore Evaluate(CandidateDriver candidate, DeviceInfo device)
    {
        var points = 0;
        var details = new List<string>();

        // 1. Kaynak resmi mi? (+30)
        if (IsOfficialSource(candidate.SourceType))
        {
            points += 30;
            details.Add("Official source (+30)");
        }

        // 2. Dijital imza (+25)
        if (candidate.IsSigned)
        {
            points += 25;
            details.Add("Digitally signed (+25)");
        }
        else
        {
            details.Add("Not signed (0)");
        }

        // 3. OEM uyumu (+20)
        if (IsOemMatch(candidate, device))
        {
            points += 20;
            details.Add("OEM match (+20)");
        }

        // 4. Stabil mi? (+15)
        if (!candidate.IsBeta)
        {
            points += 15;
            details.Add("Stable release (+15)");
        }
        else
        {
            details.Add("Beta release (0)");
        }

        // 5. Hardware ID doğrudan eşleşme (+10)
        if (HasDirectHardwareIdMatch(candidate, device))
        {
            points += 10;
            details.Add("Direct Hardware ID match (+10)");
        }

        var level = points switch
        {
            >= 80 => TrustLevel.Verified,
            >= 50 => TrustLevel.Trusted,
            >= 25 => TrustLevel.Unknown,
            _ => TrustLevel.Untrusted,
        };

        return new TrustScore
        {
            Level = level,
            Points = Math.Min(points, MaxPoints),
            Details = details
        };
    }

    private static bool IsOfficialSource(SourceType source) =>
        source is SourceType.MicrosoftCatalog
            or SourceType.Intel
            or SourceType.Amd
            or SourceType.Nvidia;

    private static bool IsOemMatch(CandidateDriver candidate, DeviceInfo device) =>
        !string.IsNullOrWhiteSpace(device.Manufacturer)
        && !string.IsNullOrWhiteSpace(candidate.Provider)
        && device.Manufacturer.Contains(candidate.Provider, StringComparison.OrdinalIgnoreCase);

    private static bool HasDirectHardwareIdMatch(CandidateDriver candidate, DeviceInfo device) =>
        candidate.SupportedHardwareIds
            .Any(cId => device.HardwareIds.Any(dId =>
                string.Equals(cId, dId, StringComparison.OrdinalIgnoreCase)));
}
