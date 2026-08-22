namespace IndieFps.Server.Configuration;

public class JwtSettings
{
    public string Issuer { get; set; } = "indiefps";
    public string Audience { get; set; } = "indiefps-client";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string ProPriceId { get; set; } = string.Empty;
    public string ActivationPriceId { get; set; } = string.Empty;
}

public class AppSettings
{
    public string Environment { get; set; } = "Development";
    public string AllowedOrigins { get; set; } = "http://localhost:3000";
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}