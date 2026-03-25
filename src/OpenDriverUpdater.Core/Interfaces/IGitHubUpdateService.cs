// IGitHubUpdateService.cs
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Interfaces;

public interface IGitHubUpdateService
{
    Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default);
    Task<bool> DownloadAndVerifyAsync(ReleaseAsset asset, string destinationPath, IProgress<double>? progress = null, CancellationToken ct = default);
}

public record ReleaseInfo(string Version, string Notes, IReadOnlyList<ReleaseAsset> Assets);
public record ReleaseAsset(string Name, string DownloadUrl, long Size, string? ExpectedMd5 = null);
