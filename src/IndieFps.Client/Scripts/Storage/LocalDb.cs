using Godot;
using IndieFps.Shared.DTOs;
using IndieFps.Shared.Enums;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace IndieFps.Client.Storage;

public partial class LocalDb : Node
{
    private const string DbFileName = "indiefps_cache.db";
    private string _dbPath = string.Empty;
    private SqliteConnection? _connection;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    
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
    
    public override void _ExitTree()
    {
        _connection?.Close();
        _connection?.Dispose();
        base._ExitTree();
    }
}