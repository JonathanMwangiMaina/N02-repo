namespace BlackoutClause.Server.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlackoutClause.Shared.Enums;

/// <summary>
/// Represents a user account synchronized from Clerk.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique user identifier (Clerk user ID).
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Clerk user ID (redundant with Id for clarity).
    /// </summary>
    public string? ClerkUserId { get; set; }

    /// <summary>
    /// Gets or sets the user's subscription tier.
    /// </summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

    /// <summary>
    /// Gets or sets the user's subscription state.
    /// </summary>
    public SubscriptionState SubscriptionState { get; set; } = SubscriptionState.Unpaid;

    /// <summary>
    /// Gets or sets the subscription end date (if cancelled at period end).
    /// </summary>
    public DateTime? SubscriptionEndsAt { get; set; }

    /// <summary>
    /// Gets or sets the account creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last login timestamp (UTC).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets whether the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the email is verified.
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// Gets or sets the user's subscription details.
    /// </summary>
    public UserSubscription? Subscription { get; set; }
}

/// <summary>
/// Represents a user's subscription details synchronized from Clerk.
/// </summary>
public class UserSubscription
{
    /// <summary>
    /// Gets or sets the unique subscription record identifier.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the user identifier (foreign key).
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Clerk subscription identifier.
    /// </summary>
    [Required]
    public string ClerkSubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Clerk price identifier.
    /// </summary>
    public string? ClerkPriceId { get; set; }

    /// <summary>
    /// Gets or sets the subscription tier.
    /// </summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

    /// <summary>
    /// Gets or sets the subscription state.
    /// </summary>
    public SubscriptionState State { get; set; } = SubscriptionState.Unpaid;

    /// <summary>
    /// Gets or sets the current billing period start.
    /// </summary>
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the current billing period end.
    /// </summary>
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the trial end date (if applicable).
    /// </summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>
    /// Gets or sets the cancellation timestamp.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Gets or sets whether the subscription cancels at period end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Gets or sets the array of entitlement identifiers.
    /// </summary>
    public string[] Entitlements { get; set; } = [];

    /// <summary>
    /// Gets or sets the record creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the record last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the associated user (navigation property).
    /// </summary>
    public User User { get; set; } = null!;
}

/// <summary>
/// Represents a processed Clerk webhook event for idempotency.
/// </summary>
public class ProcessedClerkWebhookEvent
{
    /// <summary>
    /// Gets or sets the unique record identifier.
    /// </summary>
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the Clerk event identifier (for deduplication).
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ClerkEventId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook event type.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing timestamp (UTC).
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the event was processed successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
