using System.Security.Cryptography;
using System.Text.Json;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Networking;

public sealed class GitHubUpdateService : IGitHubUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;
    private const string RepoApiUrl = "https://api.github.com/repos/OpenDriverUpdater/OpenDriverUpdater/releases/latest";

    public GitHubUpdateService(HttpClient httpClient, ILoggerService logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // GitHub API requires a User-Agent
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenDriverUpdater-App");
        }
    }

    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.Info("Checking for updates from GitHub...");
            var response = await _httpClient.GetAsync(RepoApiUrl, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            var version = root.GetProperty("tag_name").GetString() ?? "unknown";
            var notes = root.GetProperty("body").GetString() ?? "";
            
            var assets = new List<ReleaseAsset>();
            foreach (var assetNode in root.GetProperty("assets").EnumerateArray())
            {
                var name = assetNode.GetProperty("name").GetString() ?? "";
                var url = assetNode.GetProperty("browser_download_url").GetString() ?? "";
                var size = assetNode.GetProperty("size").GetInt64();
                
                // MD5 usually placed in body or separate .md5 file. 
                // For this implementation, we look for "MD5: [hash]" in the release notes if not found elsewhere.
                var expectedMd5 = ExtractMd5FromNotes(notes, name);

                assets.Add(new ReleaseAsset(name, url, size, expectedMd5));
            }

            return new ReleaseInfo(version, notes, assets);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to check for updates", ex);
            return null;
        }
    }

    public async Task<bool> DownloadAndVerifyAsync(ReleaseAsset asset, string destinationPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            _logger.Info("Downloading update: {Name} ({Size} bytes)", asset.Name, asset.Size);
            
            using var response = await _httpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
            using var source = await response.Content.ReadAsStreamAsync(ct);
            using var destination = File.Create(destinationPath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;
                progress?.Report((double)totalRead / totalBytes);
            }

            destination.Close();

            // Verify MD5
            if (!string.IsNullOrEmpty(asset.ExpectedMd5))
            {
                _logger.Info("Verifying MD5 integrity...");
                var actualMd5 = await CalculateMd5Async(destinationPath, ct);
                if (!string.Equals(actualMd5, asset.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Error("MD5 mismatch! Expected: {Expected}, Actual: {Actual}", asset.ExpectedMd5, actualMd5);
                    File.Delete(destinationPath);
                    return false;
                }
                _logger.Info("MD5 verification successful.");
            }
            else
            {
                _logger.Warning("No MD5 hash provided for verification. Proceeding with caution.");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Update download failed", ex);
            return false;
        }
    }

    private static string? ExtractMd5FromNotes(string notes, string assetName)
    {
        // Simple regex or string search for MD5 in release notes
        // Format example: OpenDriverUpdater.exe MD5: 123456...
        var lines = notes.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains(assetName, StringComparison.OrdinalIgnoreCase) && line.Contains("MD5:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split("MD5:", StringSplitOptions.TrimEntries);
                if (parts.Length > 1) return parts[1].Split(' ')[0].Trim();
            }
        }
        return null;
    }

    private static async Task<string> CalculateMd5Async(string filePath, CancellationToken ct)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
