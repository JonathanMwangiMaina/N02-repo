namespace BlackoutClause.Server.Infrastructure.Redis;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check for Redis using the abstract IRedisService.
/// </summary>
internal class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisService _redis;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
    /// </summary>
    /// <param name="redis">Redis service instance.</param>
    public RedisHealthCheck(IRedisService redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _redis.PingAsync(cancellationToken);
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Redis is responsive");
            }
            return HealthCheckResult.Unhealthy("Redis ping failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed", ex);
        }
    }
}
