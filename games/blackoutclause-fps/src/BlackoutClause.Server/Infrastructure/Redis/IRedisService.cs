namespace BlackoutClause.Server.Infrastructure.Redis;

/// <summary>
/// Abstraction for Redis operations supporting both StackExchange.Redis and Upstash Redis.
/// </summary>
internal interface IRedisService
{
    /// <summary>
    /// Gets a value by key.
    /// </summary>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The value if found, otherwise null.</returns>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Sets a key-value pair with optional expiration.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the value was set.</returns>
    Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Sets a key-value pair only if key doesn't exist (atomic).
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiry">Optional expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the value was set.</returns>
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a key.
    /// </summary>
    /// <param name="key">The key to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the key was deleted.</returns>
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Checks if a key exists.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the key exists.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Increments a numeric value (for rate limiting).
    /// </summary>
    /// <param name="key">The key to increment.</param>
    /// <param name="expiry">Optional expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new value.</returns>
    Task<long> IncrementAsync(string key, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple keys at once.
    /// </summary>
    /// <param name="keys">The keys to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary of key-value pairs that were found.</returns>
    Task<Dictionary<string, string>> GetMultipleAsync(string[] keys, CancellationToken ct = default);

    /// <summary>
    /// Sets multiple key-value pairs at once.
    /// </summary>
    /// <param name="keyValues">The key-value pairs to set.</param>
    /// <param name="expiry">Optional expiration time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the values were set.</returns>
    Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues, TimeSpan? expiry = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the TTL (time to live) for a key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The TTL if the key exists and has an expiry, otherwise null.</returns>
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Adds a member to a sorted set with score (for leaderboards).
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="member">The member to add.</param>
    /// <param name="score">The score for sorting.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the member was added.</returns>
    Task<bool> SortedSetAddAsync(string key, string member, double score, CancellationToken ct = default);

    /// <summary>
    /// Gets rank of member in sorted set (0-based, highest score first).
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="member">The member to rank.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rank if the member exists, otherwise null.</returns>
    Task<long?> SortedSetRankAsync(string key, string member, CancellationToken ct = default);

    /// <summary>
    /// Gets score of member in sorted set.
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="member">The member to score.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The score if the member exists, otherwise null.</returns>
    Task<double?> SortedSetScoreAsync(string key, string member, CancellationToken ct = default);

    /// <summary>
    /// Gets top N members from sorted set (highest score first).
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="count">Number of top entries to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of member-score pairs.</returns>
    Task<List<(string Member, double Score)>> SortedSetTopAsync(string key, int count, CancellationToken ct = default);

    /// <summary>
    /// Gets members in score range from sorted set.
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="min">Minimum score (inclusive).</param>
    /// <param name="max">Maximum score (inclusive).</param>
    /// <param name="limit">Optional limit on results.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of member-score pairs.</returns>
    Task<List<(string Member, double Score)>> SortedSetRangeByScoreAsync(string key, double min, double max, int? limit = null, CancellationToken ct = default);

    /// <summary>
    /// Removes member from sorted set.
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="member">The member to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the member was removed.</returns>
    Task<bool> SortedSetRemoveAsync(string key, string member, CancellationToken ct = default);

    /// <summary>
    /// Gets sorted set cardinality (count).
    /// </summary>
    /// <param name="key">The sorted set key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of members in the sorted set.</returns>
    Task<long> SortedSetCountAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Health check - pings the Redis server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the server responds.</returns>
    Task<bool> PingAsync(CancellationToken ct = default);
}
