namespace BlackoutClause.Shared.Constants;

/// <summary>
/// API endpoint paths and base URL constants.
/// </summary>
public static class ApiConstants
{
    /// <summary>
    /// Base path for all API v1 endpoints.
    /// </summary>
    public const string BasePath = "/api/v1";

    /// <summary>
    /// Specific endpoint paths grouped by feature area.
    /// </summary>
    public static class Endpoints
    {
        /// <summary>
        /// Base authentication endpoint path.
        /// </summary>
        public const string Auth = BasePath + "/auth";

        /// <summary>
        /// User registration endpoint.
        /// </summary>
        public const string Register = Auth + "/register";

        /// <summary>
        /// User login endpoint.
        /// </summary>
        public const string Login = Auth + "/login";

        /// <summary>
        /// Token refresh endpoint.
        /// </summary>
        public const string Refresh = Auth + "/refresh";

        /// <summary>
        /// User logout endpoint.
        /// </summary>
        public const string Logout = Auth + "/logout";

        /// <summary>
        /// Current user profile endpoint.
        /// </summary>
        public const string Me = Auth + "/me";

        /// <summary>
        /// Base subscription management endpoint path.
        /// </summary>
        public const string Subscription = BasePath + "/subscription";

        /// <summary>
        /// Get current subscription status.
        /// </summary>
        public const string SubscriptionStatus = Subscription + "/status";

        /// <summary>
        /// Create new subscription.
        /// </summary>
        public const string SubscriptionCreate = Subscription + "/create";

        /// <summary>
        /// Cancel subscription.
        /// </summary>
        public const string SubscriptionCancel = Subscription + "/cancel";

        /// <summary>
        /// Open Clerk customer portal for subscription management.
        /// </summary>
        public const string SubscriptionPortal = Subscription + "/portal";

        /// <summary>
        /// Check user entitlements.
        /// </summary>
        public const string SubscriptionEntitlements = Subscription + "/entitlements";

        /// <summary>
        /// Base webhooks endpoint path.
        /// </summary>
        public const string Webhooks = BasePath + "/webhooks";

        /// <summary>
        /// Clerk webhook endpoint.
        /// </summary>
        public const string ClerkWebhook = Webhooks + "/clerk";

        /// <summary>
        /// Basic health check endpoint.
        /// </summary>
        public const string Health = "/health";

        /// <summary>
        /// Readiness probe endpoint (checks dependencies).
        /// </summary>
        public const string HealthReady = "/health/ready";

        /// <summary>
        /// Liveness probe endpoint (process alive).
        /// </summary>
        public const string HealthLive = "/health/live";
    }
}

/// <summary>
/// JWT token configuration constants.
/// </summary>
public static class JwtConstants
{
    /// <summary>
    /// JWT token issuer identifier.
    /// </summary>
    public const string Issuer = "blackoutclause";

    /// <summary>
    /// JWT token audience identifier.
    /// </summary>
    public const string Audience = "blackoutclause-client";

    /// <summary>
    /// Access token lifetime in minutes (as string for configuration).
    /// </summary>
    public const string AccessTokenLifetimeMinutes = "15";

    /// <summary>
    /// Refresh token lifetime in days (as string for configuration).
    /// </summary>
    public const string RefreshTokenLifetimeDays = "30";

    /// <summary>
    /// Standard JWT claim names used in tokens.
    /// </summary>
    public static class Claims
    {
        /// <summary>
        /// Subject claim - user ID.
        /// </summary>
        public const string UserId = "sub";

        /// <summary>
        /// Email claim.
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// Username claim.
        /// </summary>
        public const string Username = "username";

        /// <summary>
        /// Subscription tier claim.
        /// </summary>
        public const string Tier = "tier";

        /// <summary>
        /// Subscription state claim.
        /// </summary>
        public const string SubscriptionState = "sub_state";

        /// <summary>
        /// User entitlements claim (JSON array).
        /// </summary>
        public const string Entitlements = "entitlements";

        /// <summary>
        /// Client platform claim.
        /// </summary>
        public const string Platform = "platform";

        /// <summary>
        /// Session ID claim.
        /// </summary>
        public const string SessionId = "sid";
    }
}

/// <summary>
/// Clerk-specific constants for products, prices, metadata, and webhooks.
/// </summary>
public static class ClerkConstants
{
    /// <summary>
    /// Clerk product identifiers.
    /// </summary>
    public static class Products
    {
        /// <summary>
        /// Pro subscription product ID.
        /// </summary>
        public const string Pro = "blackoutclause_pro";
    }

    /// <summary>
    /// Clerk price identifiers.
    /// </summary>
    public static class Prices
    {
        /// <summary>
        /// One-time activation price ID ($1.00).
        /// </summary>
        public const string ActivationOneTime = "blackoutclause_activation";

        /// <summary>
        /// Monthly Pro subscription price ID ($9.99/mo).
        /// </summary>
        public const string ProMonthly = "blackoutclause_pro_monthly";
    }

    /// <summary>
    /// Metadata keys used in Clerk objects.
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// Entitlement metadata key.
        /// </summary>
        public const string Entitlement = "entitlement";

        /// <summary>
        /// User ID metadata key.
        /// </summary>
        public const string UserId = "user_id";

        /// <summary>
        /// Platform metadata key.
        /// </summary>
        public const string Platform = "platform";
    }

    /// <summary>
    /// Clerk webhook event type constants.
    /// </summary>
    public static class WebhookEvents
    {
        /// <summary>
        /// User created event.
        /// </summary>
        public const string UserCreated = "user.created";

        /// <summary>
        /// User updated event.
        /// </summary>
        public const string UserUpdated = "user.updated";

        /// <summary>
        /// User deleted event.
        /// </summary>
        public const string UserDeleted = "user.deleted";

        /// <summary>
        /// Session created event.
        /// </summary>
        public const string SessionCreated = "session.created";

        /// <summary>
        /// Session ended event.
        /// </summary>
        public const string SessionEnded = "session.ended";

        /// <summary>
        /// Subscription created event.
        /// </summary>
        public const string SubscriptionCreated = "subscription.created";

        /// <summary>
        /// Subscription updated event.
        /// </summary>
        public const string SubscriptionUpdated = "subscription.updated";

        /// <summary>
        /// Subscription cancelled event.
        /// </summary>
        public const string SubscriptionCancelled = "subscription.cancelled";

        /// <summary>
        /// Subscription deleted event.
        /// </summary>
        public const string SubscriptionDeleted = "subscription.deleted";

        /// <summary>
        /// Organization created event.
        /// </summary>
        public const string OrganizationCreated = "organization.created";

        /// <summary>
        /// Organization updated event.
        /// </summary>
        public const string OrganizationUpdated = "organization.updated";

        /// <summary>
        /// Organization deleted event.
        /// </summary>
        public const string OrganizationDeleted = "organization.deleted";

        /// <summary>
        /// Organization membership created event.
        /// </summary>
        public const string OrganizationMembershipCreated = "organization_membership.created";

        /// <summary>
        /// Organization membership updated event.
        /// </summary>
        public const string OrganizationMembershipUpdated = "organization_membership.updated";

        /// <summary>
        /// Organization membership deleted event.
        /// </summary>
        public const string OrganizationMembershipDeleted = "organization_membership.deleted";
    }
}

/// <summary>
/// Game entitlement constants defining feature access per subscription tier.
/// </summary>
public static class EntitlementConstants
{
    /// <summary>
    /// Entitlements available to free tier users.
    /// </summary>
    public static readonly string[] FreeEntitlements =
    [
        "levels.tutorial"
    ];

    /// <summary>
    /// Entitlements available to Pro tier users.
    /// </summary>
    public static readonly string[] ProEntitlements =
    [
        "levels.all",
        "multiplayer",
        "cosmetics",
        "mods",
        "cloud_saves"
    ];

    /// <summary>
    /// Human-readable display names for entitlements.
    /// </summary>
    public static readonly Dictionary<string, string> EntitlementDisplayNames = new()
    {
        ["levels.tutorial"] = "Tutorial Level",
        ["levels.all"] = "All Levels",
        ["multiplayer"] = "Multiplayer",
        ["cosmetics"] = "Cosmetics",
        ["mods"] = "Mod Support",
        ["cloud_saves"] = "Cloud Saves"
    };
}

/// <summary>
/// Subscription-related constants for billing and caching.
/// </summary>
public static class SubscriptionConstants
{
    /// <summary>
    /// Free trial duration in days.
    /// </summary>
    public const int TrialDays = 7;

    /// <summary>
    /// Grace period after failed payment in days.
    /// </summary>
    public const int GracePeriodDays = 14;

    /// <summary>
    /// One-time activation fee in USD.
    /// </summary>
    public const decimal ActivationAmountUsd = 1.00m;

    /// <summary>
    /// Monthly subscription amount in USD.
    /// </summary>
    public const decimal MonthlyAmountUsd = 9.99m;

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    public const string Currency = "usd";

    /// <summary>
    /// Interval for subscription status heartbeat sync.
    /// </summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache expiry for subscription status.
    /// </summary>
    public static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    /// <summary>
    /// Grace period for offline entitlement validation.
    /// </summary>
    public static readonly TimeSpan OfflineGracePeriod = TimeSpan.FromDays(7);
}
