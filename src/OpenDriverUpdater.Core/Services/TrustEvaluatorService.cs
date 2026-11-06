using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Services;

public sealed class TrustEvaluatorService : ITrustEvaluator
{
    public TrustScore Evaluate(CandidateDriver candidate, DeviceInfo device, VerificationResult? verification = null)
    {
        var points = 0;
        var details = new List<string>();

        var candidateIds = candidate.SupportedHardwareIds ?? Array.Empty<string>();
        var deviceIds = device.HardwareIds ?? Array.Empty<string>();

        var hasExactMatch = candidateIds.Any(cId =>
            !string.IsNullOrWhiteSpace(cId) &&
            deviceIds.Any(dId => !string.IsNullOrWhiteSpace(dId) && string.Equals(cId, dId, StringComparison.OrdinalIgnoreCase)));

        if (!hasExactMatch)
        {
            return new TrustScore { Points = 0, Level = TrustLevel.Untrusted, Details = new[] { "No hardware ID match - Untrusted" } };
        }
        details.Add("Hardware ID match - gate passed");

        if (IsOfficialSource(candidate.SourceType))
        {
            points += 30;
            details.Add("Official source (+30)");
        }

        details.Add("Source reports signed claim - 0 points");

        if (IsOemMatchNormalized(candidate, device))
        {
            details.Add("OEM match - detail only");
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
        var verified = verification?.HashVerified == true && verification?.SignatureValidated == true;
        var basis = candidate.MatchBasis;

        if (basis == HardwareIdMatchBasis.QueryOnly)
        {
            details.Add("Hardware ID match is based on the search query only");
            level = TrustLevel.Unknown;
        }
        else if (basis == HardwareIdMatchBasis.CatalogMetadata)
        {
            level = points >= 50 ? (candidate.IsBeta ? TrustLevel.Unknown : TrustLevel.Trusted) : TrustLevel.Unknown;
        }
        else // InfFile
        {
            if (verified)
            {
                level = TrustLevel.Verified;
                details.Add("Hash verified + signature validated => Verified");
            }
            else
            {
                level = points >= 50 ? (candidate.IsBeta ? TrustLevel.Unknown : TrustLevel.Trusted) : TrustLevel.Unknown;
            }
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
