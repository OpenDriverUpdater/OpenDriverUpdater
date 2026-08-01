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

    // Sadece gerçek donanım aygıtları güncellenebilir — sanal/yazılım aygıtları
    // (ACPI, ROOT, SWD, STORAGE, BTHLE vb.) sürücü güncellemesi almaz ve
    // Catalog'un serbest metin araması bunlar için yanlış eşleşmeler döndürür.
    private static readonly string[] UpdatablePrefixes =
    [
        @"PCI\VEN_",
        @"USB\VID_",
        @"HDAUDIO\",
    ];

    private static bool IsUpdatableDevice(DeviceInfo device) =>
        device.HardwareIds.Any(id =>
            UpdatablePrefixes.Any(p => id.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
    // WMI bazı aygıtlar için sürüm bildirmez. Bu iki durumu ayırmak gerekir:
    //   (a) hiç sürücü yok  → aday sürücü önerilmeli
    //   (b) sürücü var ama sürümü bildirilmemiş → karşılaştırılamaz, önerilmemeli
    // Ayrım için sağlayıcı/tarih alanları kullanılır: sürücüsüz aygıtta
    // hiçbiri dolu değildir.
    private static bool HasNoDriverInstalled(DeviceInfo device) =>
        string.IsNullOrWhiteSpace(device.InstalledDriverVersion) &&
        string.IsNullOrWhiteSpace(device.InstalledProvider) &&
        device.InstalledDriverDate is null;

    public async Task<IReadOnlyList<UpdateRecommendation>> ResolveAsync(
        IReadOnlyList<DeviceInfo> devices,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<UpdateRecommendation>();

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsUpdatableDevice(device))
            {
                _logger.Debug("Skipping non-updatable device {Device}", device.Name);
                continue;
            }

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
        // Kabul edilebilir tüm adayları topla, güven puanına göre sırala:
        // en iyisi öneri olur, kalanlar indirme çözümlemesi başarısız olursa
        // denenecek yedeklerdir (Catalog'da aynı sürücünün indirilebilir ve
        // indirilemez kopyaları bir arada bulunabiliyor).
        var acceptable = new List<(CandidateDriver Candidate, TrustScore Trust, string? Delta)>();

        foreach (var candidate in candidates)
        {
            var noDriver = HasNoDriverInstalled(device);

            string? delta;
            if (noDriver)
            {
                delta = "No driver installed";
            }
            else
            {
                var versionResult = _versionComparer.Compare(device.InstalledDriverVersion, candidate.Version);
                if (!versionResult.IsNewer)
                    continue;

                delta = versionResult.Delta;
            }

            var trust = _trustEvaluator.Evaluate(candidate, device);
            if (trust.Level == TrustLevel.Blacklisted)
                continue;

            acceptable.Add((candidate, trust, delta));
        }

        if (acceptable.Count == 0)
            return null;

        var ordered = acceptable.OrderByDescending(a => a.Trust.Points).ToList();
        var best = ordered[0];

        return new UpdateRecommendation
        {
            DeviceId = device.Id,
            InstalledVersion = device.InstalledDriverVersion,
            CandidateVersion = best.Candidate.Version,
            RecommendationType = ClassifyRecommendation(best.Trust, best.Candidate),
            TrustScore = best.Trust,
            Reason = $"{best.Candidate.SourceType}: {best.Delta}",
            Candidate = best.Candidate,
            Alternates = ordered.Skip(1).Select(a => a.Candidate).ToList()
        };
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