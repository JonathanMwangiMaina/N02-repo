namespace BlackoutClause.Shared.Enums;

/// <summary>
/// Subscription tier levels.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free tier - tutorial access only.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Pro tier - full game access including multiplayer.
    /// </summary>
    Pro = 1
}

/// <summary>
/// Subscription payment/access state.
/// </summary>
public enum SubscriptionState
{
    /// <summary>
    /// No active subscription or payment failed.
    /// </summary>
    Unpaid = 0,

    /// <summary>
    /// Free trial period active.
    /// </summary>
    Trial = 1,

    /// <summary>
    /// Active paid subscription.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Payment past due, grace period active.
    /// </summary>
    PastDue = 3,

    /// <summary>
    /// Subscription cancelled, access until period end.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Subscription fully expired.
    /// </summary>
    Expired = 5
}

/// <summary>
/// Game feature entitlements.
/// </summary>
public enum Entitlement
{
    /// <summary>
    /// Access to tutorial level only.
    /// </summary>
    LevelsTutorial = 0,

    /// <summary>
    /// Access to all game levels/maps.
    /// </summary>
    LevelsAll = 1,

    /// <summary>
    /// Multiplayer matchmaking access.
    /// </summary>
    Multiplayer = 2,

    /// <summary>
    /// Cosmetic items access.
    /// </summary>
    Cosmetics = 3,

    /// <summary>
    /// Mod installation/support access.
    /// </summary>
    ModSupport = 4,

    /// <summary>
    /// Cloud save synchronization access.
    /// </summary>
    CloudSaves = 5
}

/// <summary>
/// Authentication provider types.
/// </summary>
public enum AuthProvider
{
    /// <summary>
    /// Email/password authentication.
    /// </summary>
    Email = 0,

    /// <summary>
    /// Steam OpenID authentication.
    /// </summary>
    Steam = 1,

    /// <summary>
    /// Google OAuth authentication.
    /// </summary>
    Google = 2,

    /// <summary>
    /// Apple Sign In authentication.
    /// </summary>
    Apple = 3
}

/// <summary>
/// Client platform identifiers.
/// </summary>
public enum Platform
{
    /// <summary>
    /// Windows platform.
    /// </summary>
    Windows = 0,

    /// <summary>
    /// macOS platform.
    /// </summary>
    MacOS = 1,

    /// <summary>
    /// Linux platform.
    /// </summary>
    Linux = 2
}
