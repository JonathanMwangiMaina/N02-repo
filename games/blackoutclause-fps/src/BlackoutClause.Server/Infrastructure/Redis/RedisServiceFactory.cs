#pragma warning disable CS1591

namespace BlackoutClause.Server.Infrastructure.Redis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

/// <summary>
/// Factory for creating the appropriate Redis service based on configuration.
/// </summary>
internal class RedisServiceFactory
{
    /// <summary>
    /// Creates the appropriate Redis service implementation based on configuration.
    /// </summary>
    /// <param name="settings">Redis configuration settings.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    /// <returns>An <see cref="IRedisService"/> implementation.</returns>
    public static IRedisService Create(IOptions<BlackoutClause.Server.Configuration.RedisSettings> settings, ILoggerFactory loggerFactory)
    {
        var config = settings.Value;
        var logger = loggerFactory.CreateLogger<RedisServiceFactory>();

        return config.Provider switch
        {
            "Upstash" => CreateUpstash(config, loggerFactory),
            _ => CreateStackExchange(config, loggerFactory)
        };
    }

    private static IRedisService CreateStackExchange(BlackoutClause.Server.Configuration.RedisSettings config, ILoggerFactory loggerFactory)
    {
        try
        {
            var options = ConfigurationOptions.Parse(config.ConnectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;

            var redis = ConnectionMultiplexer.Connect(options);
            var logger = loggerFactory.CreateLogger<StackExchangeRedisService>();
            return new StackExchangeRedisService(redis, logger);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<RedisServiceFactory>();
            logger.LogError(ex, "Failed to connect to StackExchange.Redis at {ConnectionString}", config.ConnectionString);
            throw;
        }
    }

    private static IRedisService CreateUpstash(BlackoutClause.Server.Configuration.RedisSettings config, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(config.UpstashUrl) || string.IsNullOrEmpty(config.UpstashToken))
        {
            throw new InvalidOperationException("Upstash URL and Token must be configured for Upstash provider");
        }

        var logger = loggerFactory.CreateLogger<UpstashRedisService>();
        return new UpstashRedisService(config.UpstashUrl, config.UpstashToken, logger);
    }
}
