namespace BlackoutClause.Server.Infrastructure.Clerk;

using System.Security.Claims;
using BlackoutClause.Server.Domain.Entities;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Shared.Constants;
using BlackoutClause.Shared.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service for synchronizing user data from Clerk claims to local database.
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Synchronizes user from JWT claims, creating or updating local user record.
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The synchronized user entity or null if not found.</returns>
    Task<User?> SyncUserFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}

/// <summary>
/// Implementation of user synchronization from Clerk to local database.
/// </summary>
public class UserSyncService : IUserSyncService
{
    private readonly AppDbContext _db;
    private readonly IClerkClient _clerkClient;
    private readonly ILogger<UserSyncService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSyncService"/> class.
    /// </summary>
    /// <param name="db">Application database context.</param>
    /// <param name="clerkClient">Clerk API client.</param>
    /// <param name="logger">Logger instance.</param>
    public UserSyncService(
        AppDbContext db,
        IClerkClient clerkClient,
        ILogger<UserSyncService> logger)
    {
        _db = db;
        _clerkClient = clerkClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<User?> SyncUserFromClaimsAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await _db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        // If user doesn't exist locally, fetch from Clerk and create
        if (user == null)
        {
            var clerkUser = await _clerkClient.GetUserAsync(userId, ct);
            if (clerkUser == null)
            {
                _logger.LogWarning("User {UserId} not found in Clerk", userId);
                return null;
            }

            user = new User
            {
                Id = clerkUser.Id,
                Email = clerkUser.EmailAddress,
                Username = clerkUser.Username ?? clerkUser.EmailAddress.Split('@')[0],
                Tier = SubscriptionTier.Free,
                SubscriptionState = SubscriptionState.Unpaid,
                EmailVerified = clerkUser.EmailVerified,
                CreatedAt = clerkUser.CreatedAt,
                UpdatedAt = clerkUser.UpdatedAt,
                LastLoginAt = clerkUser.LastSignInAt
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Created new user {UserId} from Clerk claims", userId);
        }
        else
        {
            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        // Sync subscription from Clerk if user has one
        if (user.Subscription == null || user.Subscription.State == SubscriptionState.Unpaid)
        {
            await SyncSubscriptionFromClerkAsync(user, ct);
        }

        return user;
    }

    private async Task SyncSubscriptionFromClerkAsync(User user, CancellationToken ct)
    {
        var subscriptions = await _clerkClient.GetSubscriptionsForUserAsync(user.Id, ct);
        if (subscriptions.Length == 0) return;

        // Get the active subscription (first non-canceled)
        var activeSub = subscriptions.FirstOrDefault(s => s.Status != "canceled");
        if (activeSub == null) return;

        var tier = activeSub.PlanId.Contains("pro") ? SubscriptionTier.Pro : SubscriptionTier.Free;
        var state = activeSub.Status switch
        {
            "active" => SubscriptionState.Active,
            "trialing" => SubscriptionState.Trial,
            "past_due" => SubscriptionState.PastDue,
            "canceled" => SubscriptionState.Cancelled,
            _ => SubscriptionState.Unpaid
        };

        var entitlements = tier == SubscriptionTier.Pro
            ? EntitlementConstants.ProEntitlements
            : EntitlementConstants.FreeEntitlements;

        if (user.Subscription == null)
        {
            user.Subscription = new UserSubscription
            {
                UserId = user.Id,
                ClerkSubscriptionId = activeSub.Id,
                ClerkPriceId = activeSub.PlanId
            };
            _db.Subscriptions.Add(user.Subscription);
        }

        user.Subscription.ClerkSubscriptionId = activeSub.Id;
        user.Subscription.ClerkPriceId = activeSub.PlanId;
        user.Subscription.Tier = tier;
        user.Subscription.State = state;
        user.Subscription.CurrentPeriodStart = activeSub.CurrentPeriodStart;
        user.Subscription.CurrentPeriodEnd = activeSub.CurrentPeriodEnd;
        user.Subscription.TrialEndsAt = activeSub.TrialEnd;
        user.Subscription.CancelAtPeriodEnd = activeSub.CancelAtPeriodEnd;
        user.Subscription.CancelledAt = activeSub.CanceledAt;
        user.Subscription.Entitlements = entitlements;
        user.Subscription.UpdatedAt = DateTime.UtcNow;

        user.Tier = tier;
        user.SubscriptionState = state;
        user.SubscriptionEndsAt = activeSub.CancelAtPeriodEnd ? activeSub.CurrentPeriodEnd : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Synced subscription for user {UserId} from Clerk: {Tier} {State}", user.Id, tier, state);
    }
}
