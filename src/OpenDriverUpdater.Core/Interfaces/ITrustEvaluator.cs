using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Sürücü güven puanı değerlendirici.
/// </summary>
public interface ITrustEvaluator
{
    TrustScore Evaluate(CandidateDriver candidate, DeviceInfo device);
}
