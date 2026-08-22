namespace IndieFps.Server.Infrastructure.Data;

using IndieFps.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSubscription> Subscriptions => Set<UserSubscription>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
        });
        
        // UserSubscription
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.StripeSubscriptionId).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.Entitlements).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                  .WithOne(u => u.Subscription)
                  .HasForeignKey<UserSubscription>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.TokenHash).HasMaxLength(512);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // UserSession
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // ProcessedWebhookEvent
        modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
        {
            entity.HasIndex(e => e.StripeEventId).IsUnique();
            entity.HasIndex(e => e.ProcessedAt);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.StripeEventId).HasMaxLength(256);
            entity.Property(e => e.EventType).HasMaxLength(100);
        });
    }
}