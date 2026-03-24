using Microsoft.Data.Sqlite;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;
using System.Text.Json;

namespace OpenDriverUpdater.Infrastructure.Storage;

/// <summary>
/// SQLite tabanlı metadata deposu — Phase 3 implementasyonu.
/// </summary>
public sealed class SqliteMetadataStore : IMetadataStore, IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ILoggerService _logger;

    public SqliteMetadataStore(string dbPath, ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Blacklist (
                DriverId TEXT PRIMARY KEY
            );
            CREATE TABLE IF NOT EXISTS DriverCache (
                HardwareId TEXT PRIMARY KEY,
                DriverJson TEXT NOT NULL,
                CachedAt TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();
        _logger.Debug("SQLite schema initialized");
    }

    // ── Blacklist ──────────────────────────────────────────────────

    public Task<IReadOnlyList<string>> GetBlacklistedDriverIdsAsync(CancellationToken ct = default)
    {
        var list = new List<string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT DriverId FROM Blacklist";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(reader.GetString(0));
        return Task.FromResult<IReadOnlyList<string>>(list);
    }

    public Task AddToBlacklistAsync(string driverId, CancellationToken ct = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Blacklist (DriverId) VALUES ($id)";
        cmd.Parameters.AddWithValue("$id", driverId);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task RemoveFromBlacklistAsync(string driverId, CancellationToken ct = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Blacklist WHERE DriverId = $id";
        cmd.Parameters.AddWithValue("$id", driverId);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    // ── Cache ──────────────────────────────────────────────────────

    public Task<CandidateDriver?> GetCachedDriverAsync(string hardwareId, CancellationToken ct = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT DriverJson FROM DriverCache WHERE HardwareId = $id";
        cmd.Parameters.AddWithValue("$id", hardwareId);
        var json = cmd.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(json))
            return Task.FromResult<CandidateDriver?>(null);

        var driver = JsonSerializer.Deserialize<CandidateDriver>(json);
        return Task.FromResult(driver);
    }

    public Task CacheDriverAsync(string hardwareId, CandidateDriver driver, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(driver);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO DriverCache (HardwareId, DriverJson, CachedAt)
            VALUES ($id, $json, $at)
        """;
        cmd.Parameters.AddWithValue("$id", hardwareId);
        cmd.Parameters.AddWithValue("$json", json);
        cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task ClearCacheAsync(CancellationToken ct = default)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM DriverCache";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
