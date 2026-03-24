using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Sources;

/// <summary>
/// Microsoft Update Catalog tabanlı sürücü kaynağı.
/// Phase 1 MVP: HTTP/scraping ile katalog sorgusu.
/// </summary>
public sealed class MicrosoftCatalogSource : IDriverSource
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;
    private const string CatalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";

    public SourceType SourceType => SourceType.MicrosoftCatalog;

    public MicrosoftCatalogSource(HttpClient httpClient, ILoggerService logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<CandidateDriver>> SearchAsync(
        DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var candidates = new List<CandidateDriver>();

        foreach (var hardwareId in device.HardwareIds)
        {
            try
            {
                var searchUrl = $"{CatalogBaseUrl}?q={Uri.EscapeDataString(hardwareId)}";
                var response = await _httpClient.GetStringAsync(searchUrl, cancellationToken);

                var parsed = ParseCatalogResponse(response, hardwareId);
                candidates.AddRange(parsed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error("Microsoft Catalog search failed for HW ID: {HwId}", ex, hardwareId);
            }
        }

        _logger.Debug("Microsoft Catalog returned {Count} candidates for {Device}",
            candidates.Count, device.Name);

        return candidates;
    }

    private List<CandidateDriver> ParseCatalogResponse(string html, string hardwareId)
    {
        var drivers = new List<CandidateDriver>();

        // TODO: Phase 1 — Implement HTML parsing for Microsoft Update Catalog.
        // Catalog sayfası tablo formatında sonuç döner.
        // Her satır: Title, Products, Classification, Last Updated, Version, Size
        // HtmlAgilityPack veya regex ile parse edilecek.

        _logger.Debug("Catalog response parsing is a stub — returning empty results");

        return drivers;
    }
}
