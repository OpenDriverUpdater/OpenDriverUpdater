using System.Security.Cryptography;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Networking;

/// <summary>
/// HTTP tabanlı sürücü indirme yöneticisi — progress raporlama ve hash doğrulama.
/// </summary>
public sealed class HttpDownloadManager : IDownloadManager
{
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

        using var response = await _httpClient.GetAsync(
            driver.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var bytesRead = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        using var sha256 = SHA256.Create();

        var buffer = new byte[8192];
        int read;

        while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha256.TransformBlock(buffer, 0, read, null, 0);
            bytesRead += read;

            if (totalBytes > 0)
                progress?.Report((double)bytesRead / totalBytes);
        }

        sha256.TransformFinalBlock([], 0, 0);
        var hash = BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();

        _logger.Info("Download completed: {Path} ({Size} bytes, SHA256: {Hash})", filePath, bytesRead, hash);

        return new DownloadedPackage
        {
            FilePath = filePath,
            Hash = hash,
            Size = bytesRead,
            Driver = driver
        };
    }

    private static string GetFileNameFromUrl(string url)
    {
        var uri = new Uri(url);
        var name = Path.GetFileName(uri.LocalPath);
        return string.IsNullOrWhiteSpace(name) ? $"driver_{Guid.NewGuid():N}.cab" : name;
    }
}
