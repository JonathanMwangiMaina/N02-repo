namespace IndieFps.Shared.DTOs;

using IndieFps.Shared.Enums;

public record RegisterRequest(
    string Email,
    string Password,
    string Username
);

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserDto User
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record UserDto(
    string Id,
    string Email,
    string Username,
    SubscriptionTier Tier,
    SubscriptionState SubscriptionState,
    DateTime? SubscriptionEndsAt,
    DateTime CreatedAt
);

public record SubscriptionStatusDto(
    string UserId,
    SubscriptionTier Tier,
    SubscriptionState State,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    bool HasActiveEntitlement,
    string[] Entitlements
);

public record CreateSubscriptionRequest(
    string PriceId,
    string PaymentMethodId,
    string? PromoCode = null
);

public record SubscriptionPortalResponse(
    string PortalUrl
);

public record CancelSubscriptionRequest(
    bool CancelAtPeriodEnd = true
);

public record EntitlementCheckRequest(
    string[] RequiredEntitlements
);

public record EntitlementCheckResponse(
    bool HasAccess,
    string[] MissingEntitlements
);

public record ErrorResponse(
    string Code,
    string Message,
    Dictionary<string, string[]>? Details = null
);

public record HealthCheckResponse(
    string Status,
    Dictionary<string, object> Checks,
    DateTime Timestamp
);