using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Services;

/// <summary>
/// Birden fazla kaynaktan sürücü toplar, karşılaştırır ve en iyi adayı belirler.
/// </summary>
public sealed class DriverResolverService : IDriverResolver
{
    private readonly IEnumerable<IDriverSource> _sources;
    private readonly IVersionComparer _versionComparer;
    private readonly ITrustEvaluator _trustEvaluator;
    private readonly ILoggerService _logger;

    public DriverResolverService(
        IEnumerable<IDriverSource> sources,
        IVersionComparer versionComparer,
        ITrustEvaluator trustEvaluator,
        ILoggerService logger)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _versionComparer = versionComparer ?? throw new ArgumentNullException(nameof(versionComparer));
        _trustEvaluator = trustEvaluator ?? throw new ArgumentNullException(nameof(trustEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<UpdateRecommendation>> ResolveAsync(
        IReadOnlyList<DeviceInfo> devices,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<UpdateRecommendation>();

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.Info("Resolving drivers for {DeviceName} ({DeviceId})", device.Name, device.Id);

            var allCandidates = new List<CandidateDriver>();

            foreach (var source in _sources)
            {
                try
                {
                    var candidates = await source.SearchAsync(device, cancellationToken);
                    allCandidates.AddRange(candidates);
                    _logger.Debug("Source {Source} returned {Count} candidates for {Device}",
                        source.SourceType, candidates.Count, device.Name);
                }
                catch (Exception ex)
                {
                    _logger.Error("Source {Source} failed for {Device}", ex, source.SourceType, device.Name);
                }
            }

            if (allCandidates.Count == 0)
            {
                _logger.Debug("No candidates found for {Device}", device.Name);
                continue;
            }

            var best = SelectBestCandidate(device, allCandidates);
            if (best is not null)
            {
                recommendations.Add(best);
            }
        }

        return recommendations;
    }

    private UpdateRecommendation? SelectBestCandidate(DeviceInfo device, List<CandidateDriver> candidates)
    {
        UpdateRecommendation? bestRecommendation = null;

        foreach (var candidate in candidates)
        {
            var versionResult = _versionComparer.Compare(device.InstalledDriverVersion, candidate.Version);

            if (!versionResult.IsNewer)
                continue;

            var trust = _trustEvaluator.Evaluate(candidate, device);

            if (trust.Level == TrustLevel.Blacklisted)
                continue;

            var recommendation = new UpdateRecommendation
            {
                DeviceId = device.Id,
                InstalledVersion = device.InstalledDriverVersion,
                CandidateVersion = candidate.Version,
                RecommendationType = ClassifyRecommendation(trust, candidate),
                TrustScore = trust,
                Reason = $"Newer driver from {candidate.SourceType}: {versionResult.Delta}",
                Candidate = candidate
            };

            if (bestRecommendation is null || trust.Points > (bestRecommendation.TrustScore?.Points ?? 0))
            {
                bestRecommendation = recommendation;
            }
        }

        return bestRecommendation;
    }

    private static RecommendationType ClassifyRecommendation(TrustScore trust, CandidateDriver candidate)
    {
        if (trust.Level >= TrustLevel.Verified && !candidate.IsBeta)
            return RecommendationType.Recommended;

        if (trust.Level >= TrustLevel.Trusted)
            return RecommendationType.Optional;

        return RecommendationType.Optional;
    }
}
