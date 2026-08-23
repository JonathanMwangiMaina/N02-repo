namespace BlackoutClause.Shared.DTOs;

using BlackoutClause.Shared.Enums;

/// <summary>
/// Request to register a new user (handled by Clerk on frontend).
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="Username">Desired username.</param>
public record RegisterRequest(
    string Email,
    string Password,
    string Username);

/// <summary>
/// Request to log in a user (handled by Clerk on frontend).
/// </summary>
/// <param name="Email">User's email address.</param>
/// <param name="Password">User's password.</param>
/// <param name="RememberMe">Whether to extend session duration.</param>
public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false);

/// <summary>
/// Authentication response containing tokens and user info.
/// </summary>
/// <param name="AccessToken">JWT access token.</param>
/// <param name="RefreshToken">Refresh token for token renewal.</param>
/// <param name="AccessTokenExpiresAt">Access token expiration timestamp.</param>
/// <param name="User">User profile information.</param>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserDto User);

/// <summary>
/// Request to refresh access token using refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token.</param>
public record RefreshTokenRequest(
    string RefreshToken);

/// <summary>
/// User profile data transfer object.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">User's email.</param>
/// <param name="Username">User's username.</param>
/// <param name="Tier">Subscription tier.</param>
/// <param name="SubscriptionState">Current subscription state.</param>
/// <param name="SubscriptionEndsAt">Subscription end date if applicable.</param>
/// <param name="CreatedAt">Account creation timestamp.</param>
public record UserDto(
    string Id,
    string Email,
    string Username,
    SubscriptionTier Tier,
    SubscriptionState SubscriptionState,
    DateTime? SubscriptionEndsAt,
    DateTime CreatedAt);

/// <summary>
/// Subscription status data transfer object.
/// </summary>
/// <param name="UserId">User identifier.</param>
/// <param name="Tier">Current subscription tier.</param>
/// <param name="State">Current subscription state.</param>
/// <param name="CurrentPeriodEnd">Current billing period end.</param>
/// <param name="TrialEndsAt">Trial end date if applicable.</param>
/// <param name="HasActiveEntitlement">Whether user has any active entitlement.</param>
/// <param name="Entitlements">Array of active entitlement identifiers.</param>
public record SubscriptionStatusDto(
    string UserId,
    SubscriptionTier Tier,
    SubscriptionState State,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    bool HasActiveEntitlement,
    string[] Entitlements);

/// <summary>
/// Request to create a new subscription.
/// </summary>
/// <param name="PriceId">Clerk price identifier.</param>
/// <param name="PaymentMethodId">Payment method identifier.</param>
/// <param name="PromoCode">Optional promotional code.</param>
public record CreateSubscriptionRequest(
    string PriceId,
    string PaymentMethodId,
    string? PromoCode = null);

/// <summary>
/// Response containing Clerk customer portal URL.
/// </summary>
/// <param name="PortalUrl">URL to Clerk-hosted billing portal.</param>
public record SubscriptionPortalResponse(
    string PortalUrl);

/// <summary>
/// Request to cancel subscription.
/// </summary>
/// <param name="CancelAtPeriodEnd">Whether to cancel at period end or immediately.</param>
public record CancelSubscriptionRequest(
    bool CancelAtPeriodEnd = true);

/// <summary>
/// Request to check user entitlements.
/// </summary>
/// <param name="RequiredEntitlements">Array of required entitlement identifiers.</param>
public record EntitlementCheckRequest(
    string[] RequiredEntitlements);

/// <summary>
/// Response from entitlement check.
/// </summary>
/// <param name="HasAccess">Whether user has all required entitlements.</param>
/// <param name="MissingEntitlements">Array of missing entitlement identifiers.</param>
public record EntitlementCheckResponse(
    bool HasAccess,
    string[] MissingEntitlements);

/// <summary>
/// Standard error response format.
/// </summary>
/// <param name="Code">Error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Details">Optional validation error details.</param>
public record ErrorResponse(
    string Code,
    string Message,
    Dictionary<string, string[]>? Details = null);

/// <summary>
/// Health check response.
/// </summary>
/// <param name="Status">Overall health status.</param>
/// <param name="Checks">Individual health check results.</param>
/// <param name="Timestamp">Check timestamp.</param>
public record HealthCheckResponse(
    string Status,
    Dictionary<string, object> Checks,
    DateTime Timestamp);

/// <summary>
/// Request to find a match with optional preferences.
/// </summary>
/// <param name="Mode">Game mode (casual, ranked, etc.).</param>
/// <param name="Region">Preferred region for matchmaking.</param>
/// <param name="PreferredMaps">Optional list of preferred maps.</param>
public record FindMatchRequest(
    string Mode = "casual",
    string? Region = null,
    string[]? PreferredMaps = null);

/// <summary>
/// Response when a match is found.
/// </summary>
/// <param name="MatchId">Unique match identifier.</param>
/// <param name="ServerAddress">Game server WebSocket address.</param>
/// <param name="ServerPort">Game server port.</param>
/// <param name="Ticket">Match join ticket.</param>
public record MatchFoundResponse(
    string MatchId,
    string ServerAddress,
    int ServerPort,
    string Ticket);

/// <summary>
/// Request to submit match results.
/// </summary>
/// <param name="MatchId">Match identifier.</param>
/// <param name="WinnerTeamId">Winning team identifier.</param>
/// <param name="PlayerStats">Per-player match statistics.</param>
/// <param name="Duration">Match duration.</param>
public record MatchResultRequest(
    string MatchId,
    string WinnerTeamId,
    Dictionary<string, PlayerMatchStats> PlayerStats,
    TimeSpan Duration);

/// <summary>
/// Individual player match statistics.
/// </summary>
/// <param name="Kills">Number of kills.</param>
/// <param name="Deaths">Number of deaths.</param>
/// <param name="Assists">Number of assists.</param>
/// <param name="Score">Total match score.</param>
/// <param name="PlayTime">Time spent in match.</param>
public record PlayerMatchStats(
    int Kills,
    int Deaths,
    int Assists,
    int Score,
    TimeSpan PlayTime);

/// <summary>
/// List of available game servers.
/// </summary>
/// <param name="Servers">Array of server information.</param>
public record ServerListResponse(ServerInfo[] Servers);

/// <summary>
/// Game server information.
/// </summary>
/// <param name="Id">Server unique identifier.</param>
/// <param name="Name">Server display name.</param>
/// <param name="Address">Server connection address.</param>
/// <param name="Port">Server connection port.</param>
/// <param name="CurrentPlayers">Current player count.</param>
/// <param name="MaxPlayers">Maximum player capacity.</param>
/// <param name="Map">Current map name.</param>
/// <param name="Mode">Current game mode.</param>
/// <param name="Region">Server region.</param>
/// <param name="Ping">Average ping in milliseconds.</param>
public record ServerInfo(
    string Id,
    string Name,
    string Address,
    int Port,
    int CurrentPlayers,
    int MaxPlayers,
    string Map,
    string Mode,
    string Region,
    int Ping);

/// <summary>
/// Leaderboard response with ranked entries.
/// </summary>
/// <param name="Entries">Array of leaderboard entries.</param>
public record LeaderboardResponse(LeaderboardEntry[] Entries);

/// <summary>
/// Single leaderboard entry.
/// </summary>
/// <param name="Rank">Player rank (1-based).</param>
/// <param name="UserId">Player user ID.</param>
/// <param name="Username">Player username.</param>
/// <param name="Score">Player score.</param>
/// <param name="Wins">Number of wins.</param>
/// <param name="Losses">Number of losses.</param>
/// <param name="KdRatio">Kill/Death ratio.</param>
public record LeaderboardEntry(
    int Rank,
    string UserId,
    string Username,
    int Score,
    int Wins,
    int Losses,
    double KdRatio);

/// <summary>
/// Player statistics response.
/// </summary>
/// <param name="UserId">Player user ID.</param>
/// <param name="Username">Player username.</param>
/// <param name="Kills">Total kills.</param>
/// <param name="Deaths">Total deaths.</param>
/// <param name="Wins">Total wins.</param>
/// <param name="Losses">Total losses.</param>
/// <param name="PlayTime">Total play time.</param>
public record PlayerStatsResponse(
    string UserId,
    string Username,
    int Kills,
    int Deaths,
    int Wins,
    int Losses,
    TimeSpan PlayTime);
