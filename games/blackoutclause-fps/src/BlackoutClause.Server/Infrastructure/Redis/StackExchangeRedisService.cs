#pragma warning disable CS1591

namespace BlackoutClause.Server.Infrastructure.Redis;

using Microsoft.Extensions.Logging;
using StackExchange.Redis;

/// <summary>
/// StackExchange.Redis implementation for local/managed Redis instances.
/// </summary>
internal class StackExchangeRedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<StackExchangeRedisService> _logger;

    /// <inheritdoc />
    public StackExchangeRedisService(IConnectionMultiplexer redis, ILogger<StackExchangeRedisService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis GET failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SET failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            return await _db.StringSetAsync(key, value, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SETNX failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis DEL failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis EXISTS failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringIncrementAsync(key);
            if (expiry.HasValue)
            {
                await _db.KeyExpireAsync(key, expiry);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis INCR failed for key: {Key}", key);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetMultipleAsync(string[] keys, CancellationToken ct = default)
    {
        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var values = await _db.StringGetAsync(redisKeys);
            var result = new Dictionary<string, string>();
            for (int i = 0; i < keys.Length; i++)
            {
                if (values[i].HasValue)
                {
                    result[keys[i]] = values[i].ToString();
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis MGET failed");
            return new Dictionary<string, string>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var entries = keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray();
            await _db.StringSetAsync(entries);
            if (expiry.HasValue)
            {
                foreach (var key in keyValues.Keys)
                {
                    await _db.KeyExpireAsync(key, expiry);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis MSET failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var ttl = await _db.KeyTimeToLiveAsync(key);
            return ttl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis TTL failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken ct = default)
    {
        try
        {
            return await _db.SortedSetAddAsync(key, member, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZADD failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long?> SortedSetRankAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            // Redis returns rank with lowest score first, we want highest first
            var rank = await _db.SortedSetRankAsync(key, member, Order.Descending);
            return rank;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZRANK failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            var score = await _db.SortedSetScoreAsync(key, member);
            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZSCORE failed for key: {Key}", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<(string Member, double Score)>> SortedSetTopAsync(string key, int count, CancellationToken ct = default)
    {
        try
        {
            var results = await _db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending);
            return results.Select(r => (r.Element.ToString(), (double)r.Score)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZRANGE failed for key: {Key}", key);
            return new List<(string, double)>();
        }
    }

    /// <inheritdoc />
    public async Task<List<(string Member, double Score)>> SortedSetRangeByScoreAsync(string key, double min, double max, int? limit = null, CancellationToken ct = default)
    {
        try
        {
            var results = await _db.SortedSetRangeByScoreWithScoresAsync(key, min, max, Exclude.None, Order.Descending, skip: 0, take: limit ?? -1);
            return results.Select(r => (r.Element.ToString(), (double)r.Score)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZRANGEBYSCORE failed for key: {Key}", key);
            return new List<(string, double)>();
        }
    }

    /// <inheritdoc />
    public async Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken ct = default)
    {
        try
        {
            return await _db.SortedSetRemoveAsync(key, member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZREM failed for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> SortedSetCountAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.SortedSetLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis ZCARD failed for key: {Key}", key);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _db.PingAsync();
            return result.TotalMilliseconds >= 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis PING failed");
            return false;
        }
    }
}
