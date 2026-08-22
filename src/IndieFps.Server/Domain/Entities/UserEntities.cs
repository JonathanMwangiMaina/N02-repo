namespace IndieFps.Server.Domain.Entities;

using IndieFps.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string? StripeCustomerId { get; set; }
    
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    
    public SubscriptionState SubscriptionState { get; set; } = SubscriptionState.Unpaid;
    
    public DateTime? SubscriptionEndsAt { get; set; }
    
    public DateTime? TrialEndsAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool EmailVerified { get; set; } = false;
    
    // Navigation
    public UserSubscription? Subscription { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

public class UserSubscription
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string StripeSubscriptionId { get; set; } = string.Empty;
    
    public string? StripePriceId { get; set; }
    
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    
    public SubscriptionState State { get; set; } = SubscriptionState.Unpaid;
    
    public DateTime CurrentPeriodStart { get; set; }
    
    public DateTime CurrentPeriodEnd { get; set; }
    
    public DateTime? TrialEndsAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public bool CancelAtPeriodEnd { get; set; }
    
    public string[] Entitlements { get; set; } = [];
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}

public class RefreshToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(512)]
    public string TokenHash { get; set; } = string.Empty;
    
    public DateTime ExpiresAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? RevokedAt { get; set; }
    
    public string? RevokedByIp { get; set; }
    
    public string? ReplacedByTokenHash { get; set; }
    
    public string? DeviceInfo { get; set; }
    
    public string? IpAddress { get; set; }
    
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}

public class UserSession
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string SessionToken { get; set; } = string.Empty;
    
    public string? DeviceInfo { get; set; }
    
    public string? IpAddress { get; set; }
    
    public string? Platform { get; set; }
    
    public string? GameVersion { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}

public class ProcessedWebhookEvent
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(256)]
    public string StripeEventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    public bool Success { get; set; } = true;
    
    public string? ErrorMessage { get; set; }
}