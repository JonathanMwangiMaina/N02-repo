namespace BlackoutClause.Server.Infrastructure.Data;

using BlackoutClause.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Application database context for BlackoutClause server.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Gets or sets the users table.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the user subscriptions table.
    /// </summary>
    public DbSet<UserSubscription> Subscriptions => Set<UserSubscription>();

    /// <summary>
    /// Gets or sets the processed Clerk webhook events table (for idempotency).
    /// </summary>
    public DbSet<ProcessedClerkWebhookEvent> ProcessedClerkWebhookEvents => Set<ProcessedClerkWebhookEvent>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(36);
        });

        // UserSubscription
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.ClerkSubscriptionId).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.Entitlements).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                  .WithOne(u => u.Subscription)
                  .HasForeignKey<UserSubscription>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProcessedClerkWebhookEvent
        modelBuilder.Entity<ProcessedClerkWebhookEvent>(entity =>
        {
            entity.HasIndex(e => e.ClerkEventId).IsUnique();
            entity.HasIndex(e => e.ProcessedAt);
            entity.Property(e => e.Id).HasMaxLength(36);
            entity.Property(e => e.ClerkEventId).HasMaxLength(256);
            entity.Property(e => e.EventType).HasMaxLength(100);
        });
    }
}
