using System.Text.Json;
using BlackoutClause.Shared.DTOs;
using BlackoutClause.Shared.Enums;
using Godot;
using Microsoft.Data.Sqlite;

namespace BlackoutClause.Client.Storage;

/// <summary>
/// Local SQLite database for offline caching of subscription status, settings, and game progress.
/// </summary>
public partial class LocalDb : Node
{
    private const string DbFileName = "blackoutclause_cache.db";
    private string _dbPath = string.Empty;
    private SqliteConnection? _connection;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc/>
    public override void _Ready()
    {
        _dbPath = OS.GetUserDataDir().PathJoin(DbFileName);
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        var connectionString = $"Data Source={_dbPath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS subscription_status (
                user_id TEXT PRIMARY KEY,
                tier INTEGER NOT NULL,
                state INTEGER NOT NULL,
                current_period_end TEXT,
                trial_ends_at TEXT,
                has_active_entitlement INTEGER NOT NULL,
                entitlements TEXT NOT NULL,
                cached_at TEXT NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS user_settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS game_progress (
                user_id TEXT NOT NULL,
                level_id TEXT NOT NULL,
                data TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                PRIMARY KEY (user_id, level_id)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Upserts subscription status to local cache.
    /// </summary>
    /// <param name="status">Subscription status to cache.</param>
    public async Task UpsertSubscriptionStatusAsync(SubscriptionStatusDto status)
    {
        await Task.Run(() =>
        {
            if (_connection == null) return;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO subscription_status 
                (user_id, tier, state, current_period_end, trial_ends_at, has_active_entitlement, entitlements, cached_at)
                VALUES ($userId, $tier, $state, $currentPeriodEnd, $trialEndsAt, $hasActiveEntitlement, $entitlements, $cachedAt);
            ";

            cmd.Parameters.AddWithValue("$userId", status.UserId);
            cmd.Parameters.AddWithValue("$tier", (int)status.Tier);
            cmd.Parameters.AddWithValue("$state", (int)status.State);
            cmd.Parameters.AddWithValue("$currentPeriodEnd", status.CurrentPeriodEnd?.ToString("O") ?? "");
            cmd.Parameters.AddWithValue("$trialEndsAt", status.TrialEndsAt?.ToString("O") ?? "");
            cmd.Parameters.AddWithValue("$hasActiveEntitlement", status.HasActiveEntitlement ? 1 : 0);
            cmd.Parameters.AddWithValue("$entitlements", JsonSerializer.Serialize(status.Entitlements, _jsonOptions));
            cmd.Parameters.AddWithValue("$cachedAt", DateTime.UtcNow.ToString("O"));

            cmd.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Retrieves cached subscription status for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>Cached subscription status or null if not found.</returns>
    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(string userId)
    {
        return await Task.Run(() =>
        {
            if (_connection == null) return null;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM subscription_status WHERE user_id = $userId;";
            cmd.Parameters.AddWithValue("$userId", userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new SubscriptionStatusDto(
                    reader.GetString(0),
                    (SubscriptionTier)reader.GetInt32(1),
                    (SubscriptionState)reader.GetInt32(2),
                    string.IsNullOrEmpty(reader.GetString(3)) ? null : DateTime.Parse(reader.GetString(3)),
                    string.IsNullOrEmpty(reader.GetString(4)) ? null : DateTime.Parse(reader.GetString(4)),
                    reader.GetInt32(5) == 1,
                    JsonSerializer.Deserialize<string[]>(reader.GetString(6), _jsonOptions) ?? []
                );
            }

            return null;
        });
    }

    /// <summary>
    /// Sets a user setting in local cache.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public async Task SetSettingAsync(string key, string value)
    {
        await Task.Run(() =>
        {
            if (_connection == null) return;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO user_settings (key, value) VALUES ($key, $value);";
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value", value);
            cmd.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Gets a user setting from local cache.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <returns>Setting value or null if not found.</returns>
    public async Task<string?> GetSettingAsync(string key)
    {
        return await Task.Run(() =>
        {
            if (_connection == null) return null;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT value FROM user_settings WHERE key = $key;";
            cmd.Parameters.AddWithValue("$key", key);

            var result = cmd.ExecuteScalar();
            return result?.ToString();
        });
    }

    /// <summary>
    /// Saves game progress for a user and level.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="levelId">Level identifier.</param>
    /// <param name="progressData">Progress data object to serialize.</param>
    public async Task SaveGameProgressAsync(string userId, string levelId, object progressData)
    {
        await Task.Run(() =>
        {
            if (_connection == null) return;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO game_progress (user_id, level_id, data, updated_at)
                VALUES ($userId, $levelId, $data, $updatedAt);
            ";

            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$levelId", levelId);
            cmd.Parameters.AddWithValue("$data", JsonSerializer.Serialize(progressData, _jsonOptions));
            cmd.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));

            cmd.ExecuteNonQuery();
        });
    }

    /// <summary>
    /// Retrieves game progress for a user and level.
    /// </summary>
    /// <typeparam name="T">Type to deserialize progress data to.</typeparam>
    /// <param name="userId">User identifier.</param>
    /// <param name="levelId">Level identifier.</param>
    /// <returns>Deserialized progress data or default if not found.</returns>
    public async Task<T?> GetGameProgressAsync<T>(string userId, string levelId)
    {
        return await Task.Run(() =>
        {
            if (_connection == null) return default;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT data FROM game_progress WHERE user_id = $userId AND level_id = $levelId;";
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$levelId", levelId);

            var result = cmd.ExecuteScalar();
            if (result != null)
            {
                return JsonSerializer.Deserialize<T>(result.ToString()!, _jsonOptions);
            }

            return default;
        });
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        _connection?.Close();
        _connection?.Dispose();
        base._ExitTree();
    }
}
