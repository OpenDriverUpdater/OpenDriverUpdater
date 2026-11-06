using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Services;

public sealed class TrustEvaluatorService : ITrustEvaluator
{
    public TrustScore Evaluate(CandidateDriver candidate, DeviceInfo device)
    {
        var points = 0;
        var details = new List<string>();

        // Gate: exact hardware ID match required
        var hasExactMatch = candidate.SupportedHardwareIds?.Any(cId => device.HardwareIds.Any(dId => string.Equals(cId, dId, StringComparison.OrdinalIgnoreCase))) ?? false;
        if (!hasExactMatch)
        {
            return new TrustScore
            {
                Points = 0,
                Level = TrustLevel.Untrusted,
                Details = new[] { "No hardware ID match - Untrusted" }
            };
        }
        details.Add("Hardware ID match - gate passed");

        if (IsOfficialSource(candidate.SourceType))
        {
            points += 30;
            details.Add("Official source (+30)");
        }

        // SourceReportsSigned is a claim, 0 points
        details.Add("Source reports signed claim - 0 points");

        if (IsOemMatchNormalized(candidate, device))
        {
            points += 20;
            details.Add("OEM match (+20)");
        }

        if (!candidate.IsBeta)
        {
            points += 15;
            details.Add("Stable release (+15)");
        }
        else
        {
            details.Add("Beta release - level lowered");
        }

        points += 10;
        details.Add("Hardware ID match (+10)");

        TrustLevel level;
        if (candidate.HashVerified && candidate.SignatureValidated)
        {
            level = TrustLevel.Verified;
            details.Add("Hash verified + signature validated => Verified");
        }
        else if (points >= 50)
        {
            level = candidate.IsBeta ? TrustLevel.Unknown : TrustLevel.Trusted;
        }
        else if (points >= 25)
        {
            level = TrustLevel.Unknown;
        }
        else
        {
            level = TrustLevel.Untrusted;
        }

        return new TrustScore { Points = points, Level = level, Details = details };
    }

    private static bool IsOfficialSource(SourceType source) => source switch
    {
        SourceType.MicrosoftCatalog => true,
        SourceType.Intel => true,
        SourceType.Amd => true,
        SourceType.Nvidia => true,
        _ => false
    };

    private static bool IsOemMatchNormalized(CandidateDriver candidate, DeviceInfo device)
    {
        var provider = (candidate.Provider ?? "").Trim().ToLowerInvariant();
        var manufacturer = (device.Manufacturer ?? "").Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(provider) && manufacturer.Contains(provider);
    }
}
