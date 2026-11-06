using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

public interface ITrustEvaluator
{
    TrustScore Evaluate(CandidateDriver candidate, DeviceInfo device, VerificationResult? verification = null);
}
