using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Networking;

/// <summary>
/// HTTP tabanlı sürücü indirme yöneticisi — progress raporlama, hash doğrulama,
/// kesintide Range ile devam etme ve geri çekilmeli yeniden deneme.
/// </summary>
public sealed class HttpDownloadManager : IDownloadManager
{
    private const int MaxAttempts = 4;

    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;

    public HttpDownloadManager(HttpClient httpClient, ILoggerService logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DownloadedPackage> DownloadAsync(
        CandidateDriver driver,
        string destinationDir,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(driver.DownloadUrl))
            throw new InvalidOperationException("Driver download URL is empty");

        Directory.CreateDirectory(destinationDir);
        var fileName = GetFileNameFromUrl(driver.DownloadUrl);
        var filePath = Path.Combine(destinationDir, fileName);

        _logger.Info("Downloading {Title} from {Url}", driver.Title, driver.DownloadUrl);

        Exception? lastError = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await DownloadAttemptAsync(driver.DownloadUrl!, filePath, progress, cancellationToken);

                // Hash tamamlanmış dosyadan hesaplanır — devam ettirilen
                // indirmelerde artımlı hash mümkün olmadığı için.
                var size = new FileInfo(filePath).Length;
                var hash = await ComputeSha256Async(filePath, cancellationToken);

                // SHA256 doğrulaması
                if (!string.IsNullOrWhiteSpace(driver.ExpectedSha256))
                {
                    var expected = driver.ExpectedSha256!.Trim().ToLowerInvariant();
                    if (!string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(filePath);
                        _logger.Error("SHA256 mismatch for {Title}. Expected {Expected}, got {Actual}", null, driver.Title, expected, hash);
                        throw new InvalidDataException($"SHA256 verification failed for {driver.Title}");
                    }
                    _logger.Info("SHA256 verified for {Title}", driver.Title);
                }

                _logger.Info("Download completed: {Path} ({Size} bytes, SHA256: {Hash})",
                    filePath, size, hash);

                return new DownloadedPackage
                {
                    FilePath = filePath,
                    Hash = hash,
                    Size = size,
                    Driver = driver
                };
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException &&
                                       !cancellationToken.IsCancellationRequested)
            {
                // Ağ/TLS kesintileri geçicidir (ör. SEC_E_DECRYPT_FAILURE).
                // Kısmi dosya diskte kalır; sonraki deneme Range ile devam eder.
                lastError = ex;

                if (attempt == MaxAttempts)
                    break;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));   // 2s, 4s, 8s
                _logger.Warn("Download attempt {Attempt}/{Max} failed ({Error}). Retrying in {Delay}s",
                    attempt, MaxAttempts, ex.Message, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.Error("Download failed after {Attempts} attempts", lastError!, MaxAttempts);
        throw lastError!;
    }

    private async Task DownloadAttemptAsync(
        string url,
        string filePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        // Diskte kısmi dosya varsa kaldığı yerden devam etmeyi dene.
        var existingBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (existingBytes > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existingBytes, null);
            _logger.Debug("Resuming download at byte {Offset}", existingBytes);
        }

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // Sunucu Range'i yok saydıysa (206 yerine 200) baştan başla.
        var resuming = response.StatusCode == HttpStatusCode.PartialContent && existingBytes > 0;
        if (!resuming)
            existingBytes = 0;

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentRange?.Length
                         ?? (response.Content.Headers.ContentLength is { } len ? existingBytes + len : -1);

        var bytesRead = existingBytes;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            filePath,
            resuming ? FileMode.Append : FileMode.Create,
            FileAccess.Write, FileShare.None, 8192, useAsync: true);

        var buffer = new byte[8192];
        double lastReported = 0;
        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesRead += read;

            if (totalBytes > 0)
            {
                var fraction = (double)bytesRead / totalBytes;
                if (fraction - lastReported >= 0.01 || fraction >= 1.0)
                {
                    lastReported = fraction;
                    progress?.Report(fraction);
                }
            }
        }
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        var name = Path.GetFileName(uri.LocalPath);
        return string.IsNullOrWhiteSpace(name) ? $"driver_{Guid.NewGuid():N}.cab" : name;
    }
}