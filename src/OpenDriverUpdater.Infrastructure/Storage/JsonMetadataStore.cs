using System.Text.Json;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Storage;

/// <summary>
/// JSON dosya tabanlı metadata deposu.
/// </summary>
public sealed class JsonMetadataStore : IMetadataStore
{
    private readonly string _basePath;
    private readonly ILoggerService _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string BlacklistPath => Path.Combine(_basePath, "blacklist.json");
    private string CachePath => Path.Combine(_basePath, "cache.json");

    public JsonMetadataStore(string basePath, ILoggerService logger)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Directory.CreateDirectory(_basePath);
    }

    // ── Blacklist ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetBlacklistedDriverIdsAsync(CancellationToken ct = default)
    {
        return await LoadListAsync(BlacklistPath, ct);
    }

    public async Task AddToBlacklistAsync(string driverId, CancellationToken ct = default)
    {
        var list = (await LoadListAsync(BlacklistPath, ct)).ToList();
        if (!list.Contains(driverId, StringComparer.OrdinalIgnoreCase))
        {
            list.Add(driverId);
            await SaveListAsync(BlacklistPath, list, ct);
            _logger.Info("Added {DriverId} to blacklist", driverId);
        }
    }

    public async Task RemoveFromBlacklistAsync(string driverId, CancellationToken ct = default)
    {
        var list = (await LoadListAsync(BlacklistPath, ct)).ToList();
        if (list.RemoveAll(id => string.Equals(id, driverId, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            await SaveListAsync(BlacklistPath, list, ct);
            _logger.Info("Removed {DriverId} from blacklist", driverId);
        }
    }

    // ── Cache ──────────────────────────────────────────────────────

    public async Task<CandidateDriver?> GetCachedDriverAsync(string hardwareId, CancellationToken ct = default)
    {
        var cache = await LoadCacheAsync(ct);
        return cache.GetValueOrDefault(hardwareId);
    }

    public async Task CacheDriverAsync(string hardwareId, CandidateDriver driver, CancellationToken ct = default)
    {
        var cache = await LoadCacheAsync(ct);
        cache[hardwareId] = driver;
        var json = JsonSerializer.Serialize(cache, _jsonOptions);
        await File.WriteAllTextAsync(CachePath, json, ct);
    }

    public Task ClearCacheAsync(CancellationToken ct = default)
    {
        if (File.Exists(CachePath)) File.Delete(CachePath);
        _logger.Info("Cache cleared");
        return Task.CompletedTask;
    }

    // ── Helpers ────────────────────────────────────────────────────

    private async Task<List<string>> LoadListAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? [];
    }

    private async Task SaveListAsync(string path, List<string> list, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(list, _jsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    private async Task<Dictionary<string, CandidateDriver>> LoadCacheAsync(CancellationToken ct)
    {
        if (!File.Exists(CachePath)) return new();
        var json = await File.ReadAllTextAsync(CachePath, ct);
        return JsonSerializer.Deserialize<Dictionary<string, CandidateDriver>>(json, _jsonOptions) ?? new();
    }
}
