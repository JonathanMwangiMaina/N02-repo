#pragma warning disable CS1591

namespace BlackoutClause.Server.Infrastructure.Redis;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Upstash Redis implementation using HTTP REST API directly (no SDK dependency).
/// Supports scale-to-zero, global replication, and per-request pricing.
/// </summary>
internal class UpstashRedisService : IRedisService
{
    private readonly HttpClient _http;
    private readonly ILogger<UpstashRedisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public UpstashRedisService(string url, string token, ILogger<UpstashRedisService> logger)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"get/{Uri.EscapeDataString(key)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<string>>(_jsonOptions, ct);
            return result?.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash GET failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var request = new SetRequest
            {
                Key = key,
                Value = value,
                Ex = expiry.HasValue ? (int)expiry.Value.TotalSeconds : null
            };
            var response = await _http.PostAsJsonAsync("set", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash SET failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var request = new SetRequest
            {
                Key = key,
                Value = value,
                Ex = expiry.HasValue ? (int)expiry.Value.TotalSeconds : null,
                Nx = true
            };
            var response = await _http.PostAsJsonAsync("set", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash SETNX failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync($"del/{Uri.EscapeDataString(key)}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash DEL failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"exists/{Uri.EscapeDataString(key)}", ct);
            if (!response.IsSuccessStatusCode) return false;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<int>>(_jsonOptions, ct);
            return result?.Result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash EXISTS failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var request = new IncrRequest { Key = key };
            var response = await _http.PostAsJsonAsync("incr", request, _jsonOptions, ct);
            if (!response.IsSuccessStatusCode) return 0;

            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<long>>(_jsonOptions, ct);
            var value = result?.Result ?? 0;

            if (expiry.HasValue)
            {
                await ExpireAsync(key, expiry.Value, ct);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash INCR failed for key: {Key}", key);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetMultipleAsync(string[] keys, CancellationToken ct = default)
    {
        try
        {
            var request = new MGetRequest { Keys = keys };
            var response = await _http.PostAsJsonAsync("mget", request, _jsonOptions, ct);
            if (!response.IsSuccessStatusCode) return new Dictionary<string, string>();

            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<string[]>>(_jsonOptions, ct);
            if (result?.Result == null) return new Dictionary<string, string>();

            var dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Length && i < result.Result.Length; i++)
            {
                if (result.Result[i] != null)
                {
                    dict[keys[i]] = result.Result[i];
                }
            }
            return dict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash MGET failed");
            return new Dictionary<string, string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var request = new MSetRequest { KeyValues = keyValues };
            if (expiry.HasValue)
            {
                request.Ex = (int)expiry.Value.TotalSeconds;
            }
            var response = await _http.PostAsJsonAsync("mset", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash MSET failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"ttl/{Uri.EscapeDataString(key)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<int>>(_jsonOptions, ct);
            return result?.Result > 0 ? TimeSpan.FromSeconds(result.Result) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash TTL failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken ct = default)
    {
        try
        {
            var request = new ZAddRequest
            {
                Key = key,
                Members = new[] { new ZAddMember { Member = member, Score = score } }
            };
            var response = await _http.PostAsJsonAsync("zadd", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZADD failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long?> SortedSetRankAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"zrank/{Uri.EscapeDataString(key)}/{Uri.EscapeDataString(member)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<long>>(_jsonOptions, ct);
            if (result?.Result == null) return null;

            // Upstash returns rank with lowest score first
            // For highest-first (leaderboards), we need count - rank - 1
            var count = await SortedSetCountAsync(key, ct);
            return count - result.Result - 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZRANK failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"zscore/{Uri.EscapeDataString(key)}/{Uri.EscapeDataString(member)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<double>>(_jsonOptions, ct);
            return result?.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZSCORE failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<(string Member, double Score)>> SortedSetTopAsync(string key, int count, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"zrange/{Uri.EscapeDataString(key)}/0/{count - 1}/rev/true/withscores/true", ct);
            if (!response.IsSuccessStatusCode) return new List<(string, double)>();

            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<ZRangeEntry[]>>(_jsonOptions, ct);
            return result?.Result?
                .Select(r => (r.Member, r.Score))
                .ToList() ?? new List<(string, double)>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZRANGE failed for key: {Key}", key);
            return new List<(string, double)>();
        }
    }

    /// <inheritdoc />
    public async Task<List<(string Member, double Score)>> SortedSetRangeByScoreAsync(string key, double min, double max, int? limit = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"zrangebyscore/{Uri.EscapeDataString(key)}/{min}/{max}/rev/true/withscores/true";
            if (limit.HasValue)
            {
                url += $"/limit/0/{limit.Value}";
            }

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return new List<(string, double)>();

            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<ZRangeEntry[]>>(_jsonOptions, ct);
            return result?.Result?
                .Select(r => (r.Member, r.Score))
                .ToList() ?? new List<(string, double)>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZRANGEBYSCORE failed for key: {Key}", key);
            return new List<(string, double)>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            var request = new ZRemRequest { Key = key, Members = new[] { member } };
            var response = await _http.PostAsJsonAsync("zrem", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZREM failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> SortedSetCountAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"zcard/{Uri.EscapeDataString(key)}", ct);
            if (!response.IsSuccessStatusCode) return 0;
            var result = await response.Content.ReadFromJsonAsync<UpstashResponse<long>>(_jsonOptions, ct);
            return result?.Result ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash ZCARD failed for key: {Key}", key);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("ping", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash PING failed");
            return false;
        }
    }

    private async Task<bool> ExpireAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            var request = new ExpireRequest { Key = key, Ex = (int)expiry.TotalSeconds };
            var response = await _http.PostAsJsonAsync("expire", request, _jsonOptions, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upstash EXPIRE failed for key: {Key}", key);
            return false;
        }
    }

    // Upstash REST API request/response models
    private class UpstashResponse<T>
    {
        public T? Result { get; set; }
        public bool Ok { get; set; }
        public string? Error { get; set; }
    }

    private class SetRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int? Ex { get; set; }
        public bool Nx { get; set; }
    }

    private class IncrRequest
    {
        public string Key { get; set; } = string.Empty;
    }

    private class MGetRequest
    {
        public string[] Keys { get; set; } = [];
    }

    private class MSetRequest
    {
        public Dictionary<string, string> KeyValues { get; set; } = new();
        public int? Ex { get; set; }
    }

    private class ExpireRequest
    {
        public string Key { get; set; } = string.Empty;
        public int Ex { get; set; }
    }

    private class ZAddRequest
    {
        public string Key { get; set; } = string.Empty;
        public ZAddMember[] Members { get; set; } = [];
    }

    private class ZAddMember
    {
        public string Member { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    private class ZRemRequest
    {
        public string Key { get; set; } = string.Empty;
        public string[] Members { get; set; } = [];
    }

    private class ZRangeEntry
    {
        public string Member { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
