using HtmlAgilityPack;
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
        // Sorgular en özgülden en genele sıralı: tam donanım kimliği, sonra
        // &REV / &SUBSYS soyulmuş varyantlar. İlk sonuç veren sorguda dur —
        // daha özgül eşleşme her zaman daha iyi sürücüdür ve Catalog'a
        // gereksiz istek atmayı önler.
        foreach (var query in BuildSearchQueries(device.HardwareIds))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var searchUrl = $"{CatalogBaseUrl}?q={Uri.EscapeDataString(query)}";
                var response = await _httpClient.GetStringAsync(searchUrl, cancellationToken);

                var parsed = ParseCatalogResponse(response, query);
                if (parsed.Count > 0)
                {
                    _logger.Debug("Microsoft Catalog returned {Count} candidates for {Device} via {HwId}",
                        parsed.Count, device.Name, query);
                    return parsed;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error("Microsoft Catalog search failed for HW ID: {HwId}", ex, query);
            }
        }

        _logger.Debug("Microsoft Catalog returned 0 candidates for {Device}", device.Name);
        return Array.Empty<CandidateDriver>();
    }

    /// <summary>
    /// Donanım kimliklerinden arama sorguları üretir: tam kimlik, ardından
    /// giderek kısalan varyantlar (&REV, &SUBSYS soyularak, en son VEN&DEV).
    /// Catalog metin araması genelde kısa forma yayınlanan sürücüleri bulur.
    /// </summary>
    private static IReadOnlyList<string> BuildSearchQueries(IReadOnlyList<string> hardwareIds)
    {
        var queries = new List<string>();

        void Add(string q)
        {
            if (!string.IsNullOrWhiteSpace(q) &&
                !queries.Contains(q, StringComparer.OrdinalIgnoreCase))
            {
                queries.Add(q);
            }
        }

        foreach (var id in hardwareIds)
        {
            // CC_ (device class) varyantları çok genel — atla.
            if (id.Contains("&CC_", StringComparison.OrdinalIgnoreCase))
                continue;

            Add(id);

            var revIdx = id.IndexOf("&REV_", StringComparison.OrdinalIgnoreCase);
            if (revIdx > 0) Add(id[..revIdx]);

            var subsysIdx = id.IndexOf("&SUBSYS_", StringComparison.OrdinalIgnoreCase);
            if (subsysIdx > 0) Add(id[..subsysIdx]);
        }

        return queries;
    }

    /// <summary>
    /// Catalog update GUID'inden gerçek indirme URL'sini çözümler.
    /// DownloadDialog.aspx'e POST atar ve yanıttaki dosya URL'lerini ayıklar.
    /// </summary>
    public async Task<string?> ResolveDownloadUrlAsync(
        string updateId, CancellationToken cancellationToken = default)
    {
        const string dialogUrl = "https://www.catalog.update.microsoft.com/DownloadDialog.aspx";

        // The dialog endpoint expects a form field "updateIDs" containing a JSON array.
        var payload = $"[{{\"size\":0,\"languages\":\"\",\"uidInfo\":\"{updateId}\",\"updateID\":\"{updateId}\"}}]";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["updateIDs"] = payload,
        });

        try
        {
            using var response = await _httpClient.PostAsync(dialogUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // Response is JS containing lines like:
            //   downloadInformation[0].files[0].url = 'https://catalog.s.download.windowsupdate.com/...cab';
            var match = System.Text.RegularExpressions.Regex.Match(
                body, @"https?://[^'""\s]+\.(?:cab|msu|exe)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                _logger.Debug("Resolved download URL for update {UpdateId}", updateId);
                return match.Value;
            }

            _logger.Warn("No download URL found in Catalog dialog response for {UpdateId}", updateId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error("Download URL resolution failed for update {UpdateId}", ex, updateId);
            return null;
        }
    }

    private List<CandidateDriver> ParseCatalogResponse(string html, string searchQuery)
    {
        var drivers = new List<CandidateDriver>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // "We did not find any results" page has no results table.
        var table = doc.GetElementbyId("ctl00_catalogBody_updateMatches");
        if (table is null)
            return drivers;

        var rows = table.SelectNodes(".//tr[@id]");
        if (rows is null)
            return drivers;

        foreach (var row in rows)
        {
            if (row.Id == "headerRow")
                continue;

            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count < 7)
                continue;

            // Title cell contains <a id="{updateGuid}_link">Title</a>.
            var titleLink = cells[1].SelectSingleNode(".//a[contains(@id, '_link')]");
            if (titleLink is null)
                continue;

            var updateId = titleLink.Id.Replace("_link", "").Trim();
            var title = HtmlEntity.DeEntitize(titleLink.InnerText).Trim();
            var products = HtmlEntity.DeEntitize(cells[2].InnerText).Trim();
            var classification = HtmlEntity.DeEntitize(cells[3].InnerText).Trim();
            var lastUpdatedText = cells[4].InnerText.Trim();
            var version = cells[5].InnerText.Trim();

            // Catalog'un Version sütunu sürücüler için çoğunlukla "n/a" —
            // gerçek sürüm başlıkta yer alır ("Intel - System - 10.1.18836.8283").
            if (version.Equals("n/a", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(version))
            {
                var titleVersion = System.Text.RegularExpressions.Regex.Match(
                    title, @"\d+(?:\.\d+){1,3}\s*$");
                if (titleVersion.Success)
                    version = titleVersion.Value.Trim();
            }

            // Only driver updates — the Catalog can also return security
            // updates etc. for broad queries.
            if (!classification.Contains("Driver", StringComparison.OrdinalIgnoreCase))
                continue;

            // Size cell: first span is display text ("24.4 MB"), a hidden
            // second span holds the exact byte count.
            long sizeBytes = 0;
            var sizeSpans = cells[6].SelectNodes(".//span");
            if (sizeSpans is { Count: >= 2 })
                long.TryParse(sizeSpans[1].InnerText.Trim(), out sizeBytes);

            DateTime.TryParse(lastUpdatedText,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var lastUpdated);

            drivers.Add(new CandidateDriver
            {
                Title = title,
                Version = version,
                ReleaseDate = lastUpdated == default ? null : lastUpdated,
                Provider = products,
                SourceType = SourceType.MicrosoftCatalog,
                SourceReference = updateId,      // Catalog update GUID — download resolution needs this
                SizeBytes = sizeBytes > 0 ? sizeBytes : null,
                IsSigned = true,                 // Catalog only distributes WHQL-signed drivers
                SupportedHardwareIds = new[] { searchQuery },
            });
        }

        return drivers;
    }
}