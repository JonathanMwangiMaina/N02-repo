namespace BlackoutClause.Server.Configuration;

/// <summary>
/// Configuration settings for Clerk authentication and billing integration.
/// </summary>
public class ClerkSettings
{
    /// <summary>
    /// The Clerk domain (e.g., "clerk.yourdomain.com" or "your-instance.clerk.accounts.dev").
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// The Clerk publishable key (pk_test_... or pk_live_...).
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// The Clerk secret key (sk_test_... or sk_live_...).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The Clerk webhook secret for verifying webhook signatures (whsec_...).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// The Clerk API base URL.
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.clerk.com/v1";
}

/// <summary>
/// Redis configuration settings.
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis provider type: "StackExchange" for local/managed Redis, "Upstash" for serverless HTTP/REST.
    /// </summary>
    public string Provider { get; set; } = "StackExchange";

    /// <summary>
    /// StackExchange.Redis connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Upstash Redis REST API URL.
    /// </summary>
    public string UpstashUrl { get; set; } = string.Empty;

    /// <summary>
    /// Upstash Redis REST API token.
    /// </summary>
    public string UpstashToken { get; set; } = string.Empty;
}

/// <summary>
/// General application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The current environment name (Development, Staging, Production).
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Comma-separated list of allowed CORS origins.
    /// </summary>
    public string AllowedOrigins { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Maximum number of failed login attempts before lockout.
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes after max login attempts exceeded.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;
}
