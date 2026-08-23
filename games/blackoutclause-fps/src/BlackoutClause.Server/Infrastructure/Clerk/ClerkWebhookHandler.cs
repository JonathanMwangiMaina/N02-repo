namespace BlackoutClause.Server.Infrastructure.Clerk;

using System.Text.Json;
using BlackoutClause.Server.Configuration;
using BlackoutClause.Server.Domain.Entities;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Shared.Constants;
using BlackoutClause.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Handles incoming Clerk webhook events with signature verification.
/// </summary>
public interface IClerkWebhookHandler
{
    /// <summary>
    /// Processes a Clerk webhook event after verifying its signature.
    /// </summary>
    /// <param name="payload">The raw webhook payload.</param>
    /// <param name="signature">The Svix signature from the clerk-signature header.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(string payload, string signature, CancellationToken ct = default);
}

/// <summary>
/// Implementation of Clerk webhook handling with event routing and data synchronization.
/// </summary>
public class ClerkWebhookHandler : IClerkWebhookHandler
{
    private readonly IClerkClient _clerkClient;
    private readonly AppDbContext _db;
    private readonly ILogger<ClerkWebhookHandler> _logger;
    private readonly ClerkSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClerkWebhookHandler"/> class.
    /// </summary>
    /// <param name="clerkClient">Clerk API client.</param>
    /// <param name="db">Application database context.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="settings">Clerk configuration settings.</param>
    public ClerkWebhookHandler(
        IClerkClient clerkClient,
        AppDbContext db,
        ILogger<ClerkWebhookHandler> logger,
        IOptions<ClerkSettings> settings)
    {
        _clerkClient = clerkClient;
        _db = db;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(string payload, string signature, CancellationToken ct = default)
    {
        var isValid = _clerkClient.VerifyWebhookSignature(payload, signature);
        if (!isValid)
        {
            _logger.LogWarning("Invalid Clerk webhook signature");
            throw new InvalidOperationException("Invalid webhook signature");
        }

        var webhookEvent = JsonSerializer.Deserialize<ClerkWebhookEvent>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (webhookEvent == null)
        {
            _logger.LogWarning("Failed to deserialize Clerk webhook");
            return;
        }

        _logger.LogInformation("Processing Clerk webhook: {Type}", webhookEvent.Type);

        await ProcessEventAsync(webhookEvent, ct);
    }

    private async Task ProcessEventAsync(ClerkWebhookEvent webhookEvent, CancellationToken ct)
    {
        switch (webhookEvent.Type)
        {
            case ClerkConstants.WebhookEvents.UserCreated:
            case ClerkConstants.WebhookEvents.UserUpdated:
                await SyncUserAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.UserDeleted:
                await DeleteUserAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.SessionCreated:
            case ClerkConstants.WebhookEvents.SessionEnded:
                break;

            case ClerkConstants.WebhookEvents.SubscriptionCreated:
            case ClerkConstants.WebhookEvents.SubscriptionUpdated:
                await SyncSubscriptionAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.SubscriptionCancelled:
            case ClerkConstants.WebhookEvents.SubscriptionDeleted:
                await CancelSubscriptionAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.OrganizationCreated:
            case ClerkConstants.WebhookEvents.OrganizationUpdated:
                await SyncOrganizationAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.OrganizationDeleted:
                await DeleteOrganizationAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.OrganizationMembershipCreated:
            case ClerkConstants.WebhookEvents.OrganizationMembershipUpdated:
                await SyncOrganizationMembershipAsync(webhookEvent.Data, ct);
                break;

            case ClerkConstants.WebhookEvents.OrganizationMembershipDeleted:
                await DeleteOrganizationMembershipAsync(webhookEvent.Data, ct);
                break;

            default:
                _logger.LogInformation("Unhandled Clerk webhook type: {Type}", webhookEvent.Type);
                break;
        }
    }

    private async Task SyncUserAsync(JsonElement data, CancellationToken ct)
    {
        var clerkUser = JsonSerializer.Deserialize<ClerkUser>(data.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (clerkUser == null) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == clerkUser.Id, ct);

        if (user == null)
        {
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
        }
        else
        {
            user.Email = clerkUser.EmailAddress;
            user.Username = clerkUser.Username ?? user.Username;
            user.EmailVerified = clerkUser.EmailVerified;
            user.UpdatedAt = clerkUser.UpdatedAt;
            user.LastLoginAt = clerkUser.LastSignInAt ?? user.LastLoginAt;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Synced user {UserId} from Clerk", clerkUser.Id);
    }

    private async Task DeleteUserAsync(JsonElement data, CancellationToken ct)
    {
        var clerkUser = JsonSerializer.Deserialize<ClerkUser>(data.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (clerkUser == null) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == clerkUser.Id, ct);
        if (user != null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Deactivated user {UserId}", clerkUser.Id);
        }
    }

    private async Task SyncSubscriptionAsync(JsonElement data, CancellationToken ct)
    {
        var clerkSub = JsonSerializer.Deserialize<ClerkSubscription>(data.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (clerkSub == null) return;

        var user = await _db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == clerkSub.UserId, ct);

        if (user == null)
        {
            _logger.LogWarning("No user found for Clerk subscription {SubscriptionId}", clerkSub.Id);
            return;
        }

        var tier = clerkSub.PlanId.Contains("pro") ? SubscriptionTier.Pro : SubscriptionTier.Free;
        var state = clerkSub.Status switch
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
                ClerkSubscriptionId = clerkSub.Id,
                ClerkPriceId = clerkSub.PlanId
            };
            _db.Subscriptions.Add(user.Subscription);
        }

        user.Subscription.ClerkSubscriptionId = clerkSub.Id;
        user.Subscription.ClerkPriceId = clerkSub.PlanId;
        user.Subscription.Tier = tier;
        user.Subscription.State = state;
        user.Subscription.CurrentPeriodStart = clerkSub.CurrentPeriodStart;
        user.Subscription.CurrentPeriodEnd = clerkSub.CurrentPeriodEnd;
        user.Subscription.TrialEndsAt = clerkSub.TrialEnd;
        user.Subscription.CancelAtPeriodEnd = clerkSub.CancelAtPeriodEnd;
        user.Subscription.CancelledAt = clerkSub.CanceledAt;
        user.Subscription.Entitlements = entitlements;
        user.Subscription.UpdatedAt = DateTime.UtcNow;

        user.Tier = tier;
        user.SubscriptionState = state;
        user.SubscriptionEndsAt = clerkSub.CancelAtPeriodEnd ? clerkSub.CurrentPeriodEnd : null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Synced subscription for user {UserId}: {Tier} {State}", user.Id, tier, state);
    }

    private async Task CancelSubscriptionAsync(JsonElement data, CancellationToken ct)
    {
        var clerkSub = JsonSerializer.Deserialize<ClerkSubscription>(data.GetRawText(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (clerkSub == null) return;

        var user = await _db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == clerkSub.UserId, ct);

        if (user?.Subscription == null) return;

        user.Subscription.State = SubscriptionState.Cancelled;
        user.Subscription.CancelledAt = DateTime.UtcNow;
        user.Subscription.UpdatedAt = DateTime.UtcNow;

        user.Tier = SubscriptionTier.Free;
        user.SubscriptionState = SubscriptionState.Cancelled;
        user.SubscriptionEndsAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cancelled subscription for user {UserId}", user.Id);
    }

    private async Task SyncOrganizationAsync(JsonElement data, CancellationToken ct)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Organization webhook received: {Type}", data.GetProperty("type").GetString());
    }

    private async Task DeleteOrganizationAsync(JsonElement data, CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    private async Task SyncOrganizationMembershipAsync(JsonElement data, CancellationToken ct)
    {
        await Task.CompletedTask;
    }

    private async Task DeleteOrganizationMembershipAsync(JsonElement data, CancellationToken ct)
    {
        await Task.CompletedTask;
    }
}

/// <summary>
/// Represents a parsed Clerk webhook event.
/// </summary>
public class ClerkWebhookEvent
{
    /// <summary>
    /// The webhook event type (e.g., "user.created", "subscription.updated").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The event data payload as a JSON element.
    /// </summary>
    public JsonElement Data { get; set; }
}
