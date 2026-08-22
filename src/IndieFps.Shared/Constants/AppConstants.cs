namespace IndieFps.Shared.Constants;

public static class ApiConstants
{
    public const string BasePath = "/api/v1";
    
    public static class Endpoints
    {
        public const string Auth = BasePath + "/auth";
        public const string Register = Auth + "/register";
        public const string Login = Auth + "/login";
        public const string Refresh = Auth + "/refresh";
        public const string Logout = Auth + "/logout";
        public const string Me = Auth + "/me";
        
        public const string Subscription = BasePath + "/subscription";
        public const string SubscriptionStatus = Subscription + "/status";
        public const string SubscriptionCreate = Subscription + "/create";
        public const string SubscriptionCancel = Subscription + "/cancel";
        public const string SubscriptionPortal = Subscription + "/portal";
        public const string SubscriptionEntitlements = Subscription + "/entitlements";
        
        public const string Webhooks = BasePath + "/webhooks";
        public const string StripeWebhook = Webhooks + "/stripe";
        
        public const string Health = "/health";
        public const string HealthReady = "/health/ready";
        public const string HealthLive = "/health/live";
    }
}

public static class JwtConstants
{
    public const string Issuer = "indiefps";
    public const string Audience = "indiefps-client";
    public const string AccessTokenLifetimeMinutes = "15";
    public const string RefreshTokenLifetimeDays = "30";
    
    public static class Claims
    {
        public const string UserId = "sub";
        public const string Email = "email";
        public const string Username = "username";
        public const string Tier = "tier";
        public const string SubscriptionState = "sub_state";
        public const string Entitlements = "entitlements";
        public const string Platform = "platform";
        public const string SessionId = "sid";
    }
}

public static class StripeConstants
{
    public static class Products
    {
        public const string Pro = "indiefps_pro";
    }
    
    public static class Prices
    {
        public const string ActivationOneTime = "indiefps_activation"; // $1.00
        public const string ProMonthly = "indiefps_pro_monthly";       // $9.99/mo
    }
    
    public static class MetadataKeys
    {
        public const string Entitlement = "entitlement";
        public const string UserId = "user_id";
        public const string Platform = "platform";
    }
    
    public static class WebhookEvents
    {
        public const string SubscriptionCreated = "customer.subscription.created";
        public const string SubscriptionUpdated = "customer.subscription.updated";
        public const string SubscriptionDeleted = "customer.subscription.deleted";
        public const string InvoicePaymentSucceeded = "invoice.payment_succeeded";
        public const string InvoicePaymentFailed = "invoice.payment_failed";
        public const string PaymentIntentSucceeded = "payment_intent.succeeded";
        public const string PaymentIntentFailed = "payment_intent.failed";
        public const string CustomerCreated = "customer.created";
    }
}

public static class EntitlementConstants
{
    public static readonly string[] FreeEntitlements = 
    [
        "levels.tutorial"
    ];
    
    public static readonly string[] ProEntitlements = 
    [
        "levels.all",
        "multiplayer",
        "cosmetics",
        "mods",
        "cloud_saves"
    ];
    
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

public static class SubscriptionConstants
{
    public const int TrialDays = 7;
    public const int GracePeriodDays = 14;
    public const decimal ActivationAmountUsd = 1.00m;
    public const decimal MonthlyAmountUsd = 9.99m;
    public const string Currency = "usd";
    
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);
    public static readonly TimeSpan OfflineGracePeriod = TimeSpan.FromDays(7);
}