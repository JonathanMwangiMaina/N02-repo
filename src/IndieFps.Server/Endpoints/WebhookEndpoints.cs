namespace IndieFps.Server.Endpoints;

using IndieFps.Server.Infrastructure.Data;
using IndieFps.Server.Domain.Entities;
using IndieFps.Server.Infrastructure.Payments;
using IndieFps.Shared.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiConstants.Endpoints.StripeWebhook, HandleStripeWebhookAsync)
           .WithName("StripeWebhook")
           .DisableAntiforgery()
           .AllowAnonymous()
           .Produces(StatusCodes.Status200OK)
           .Produces(StatusCodes.Status400BadRequest)
           .Produces(StatusCodes.Status500InternalServerError);
    }
    
    private static async Task<IResult> HandleStripeWebhookAsync(
        HttpRequest request,
        IStripeService stripeService,
        AppDbContext db,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync(ct);
        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Missing Stripe-Signature header");
            return Results.BadRequest();
        }
        
        Event stripeEvent;
        try
        {
            stripeEvent = stripeService.ConstructEvent(json, signature);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Invalid Stripe signature");
            return Results.BadRequest();
        }
        
        // Idempotency check
        var exists = await db.ProcessedWebhookEvents
            .AnyAsync(e => e.StripeEventId == stripeEvent.Id, ct);
        
        if (exists)
        {
            logger.LogInformation("Webhook {EventId} already processed", stripeEvent.Id);
            return Results.Ok();
        }
        
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await HandleEventAsync(stripeEvent, db, logger);
            
            db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                Success = true
            });
            
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process webhook {EventId} ({EventType})", stripeEvent.Id, stripeEvent.Type);
            
            db.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
            {
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                Success = false,
                ErrorMessage = ex.Message
            });
            
            await db.SaveChangesAsync(ct);
            await transaction.RollbackAsync(ct);
            
            return Results.StatusCode(500);
        }
        
        return Results.Ok();
    }
    
    private static async Task HandleEventAsync(Event stripeEvent, AppDbContext db, ILogger logger)
    {
        switch (stripeEvent.Type)
        {
            case StripeConstants.WebhookEvents.SubscriptionCreated:
            case StripeConstants.WebhookEvents.SubscriptionUpdated:
                await SyncSubscriptionAsync(stripeEvent.Data.Object as Subscription, db, logger);
                break;
                
            case StripeConstants.WebhookEvents.SubscriptionDeleted:
                await CancelSubscriptionAsync(stripeEvent.Data.Object as Subscription, db, logger);
                break;
                
            case StripeConstants.WebhookEvents.InvoicePaymentSucceeded:
                await HandlePaymentSuccessAsync(stripeEvent.Data.Object as Invoice, db, logger);
                break;
                
            case StripeConstants.WebhookEvents.InvoicePaymentFailed:
                await HandlePaymentFailedAsync(stripeEvent.Data.Object as Invoice, db, logger);
                break;
                
            case StripeConstants.WebhookEvents.PaymentIntentSucceeded:
                await HandleActivationPaymentAsync(stripeEvent.Data.Object as PaymentIntent, db, logger);
                break;
                
            default:
                logger.LogInformation("Unhandled webhook type: {Type}", stripeEvent.Type);
                break;
        }
    }
    
    private static async Task SyncSubscriptionAsync(Subscription? stripeSub, AppDbContext db, ILogger logger)
    {
        if (stripeSub == null) return;
        
        var customerId = stripeSub.CustomerId;
        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        
        if (user == null)
        {
            logger.LogWarning("No user found for Stripe customer {CustomerId}", customerId);
            return;
        }
        
        // Extract entitlements from subscription items
        var entitlements = stripeSub.Items.Data
            .Select(i => i.Price.Metadata.GetValueOrDefault(StripeConstants.MetadataKeys.Entitlement))
            .Where(e => !string.IsNullOrEmpty(e))
            .ToArray();
        
        if (entitlements.Length == 0)
        {
            // Default based on price
            var priceId = stripeSub.Items.Data.FirstOrDefault()?.Price.Id;
            if (priceId == StripeConstants.Prices.ProMonthly)
            {
                entitlements = EntitlementConstants.ProEntitlements;
            }
        }
        
        var state = stripeSub.Status switch
        {
            "trialing" => SubscriptionState.Trial,
            "active" => SubscriptionState.Active,
            "past_due" => SubscriptionState.PastDue,
            "canceled" => SubscriptionState.Cancelled,
            "incomplete" => SubscriptionState.Unpaid,
            "incomplete_expired" => SubscriptionState.Expired,
            _ => SubscriptionState.Unpaid
        };
        
        var tier = entitlements.Contains("pro") || entitlements.Contains("levels.all") 
            ? SubscriptionTier.Pro 
            : SubscriptionTier.Free;
        
        // Update or create subscription
        if (user.Subscription == null)
        {
            user.Subscription = new UserSubscription
            {
                UserId = user.Id,
                StripeSubscriptionId = stripeSub.Id,
                StripePriceId = stripeSub.Items.Data.FirstOrDefault()?.Price.Id
            };
            db.Subscriptions.Add(user.Subscription);
        }
        
        user.Subscription.StripeSubscriptionId = stripeSub.Id;
        user.Subscription.StripePriceId = stripeSub.Items.Data.FirstOrDefault()?.Price.Id;
        user.Subscription.Tier = tier;
        user.Subscription.State = state;
        user.Subscription.CurrentPeriodStart = DateTimeOffset.FromUnixTimeSeconds(stripeSub.CurrentPeriodStart).UtcDateTime;
        user.Subscription.CurrentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(stripeSub.CurrentPeriodEnd).UtcDateTime;
        user.Subscription.TrialEndsAt = stripeSub.TrialEnd.HasValue 
            ? DateTimeOffset.FromUnixTimeSeconds(stripeSub.TrialEnd.Value).UtcDateTime 
            : null;
        user.Subscription.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        user.Subscription.CancelledAt = stripeSub.CanceledAt.HasValue 
            ? DateTimeOffset.FromUnixTimeSeconds(stripeSub.CanceledAt.Value).UtcDateTime 
            : null;
        user.Subscription.Entitlements = entitlements;
        user.Subscription.UpdatedAt = DateTime.UtcNow;
        
        // Update user denormalized fields
        user.Tier = tier;
        user.SubscriptionState = state;
        user.SubscriptionEndsAt = stripeSub.CancelAtPeriodEnd 
            ? DateTimeOffset.FromUnixTimeSeconds(stripeSub.CurrentPeriodEnd).UtcDateTime 
            : null;
        user.UpdatedAt = DateTime.UtcNow;
        
        logger.LogInformation("Synced subscription for user {UserId}: {Tier} {State}", user.Id, tier, state);
    }
    
    private static async Task CancelSubscriptionAsync(Subscription? stripeSub, AppDbContext db, ILogger logger)
    {
        if (stripeSub == null) return;
        
        var customerId = stripeSub.CustomerId;
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        
        if (user == null || user.Subscription == null) return;
        
        user.Subscription.State = SubscriptionState.Cancelled;
        user.Subscription.CancelledAt = DateTime.UtcNow;
        user.Subscription.UpdatedAt = DateTime.UtcNow;
        
        user.Tier = SubscriptionTier.Free;
        user.SubscriptionState = SubscriptionState.Cancelled;
        user.SubscriptionEndsAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        logger.LogInformation("Cancelled subscription for user {UserId}", user.Id);
    }
    
    private static async Task HandlePaymentSuccessAsync(Invoice? invoice, AppDbContext db, ILogger logger)
    {
        if (invoice == null) return;
        
        var customerId = invoice.CustomerId;
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        
        if (user == null) return;
        
        // If this was the activation payment ($1), upgrade to trial
        if (invoice.AmountPaid == 100 && invoice.Currency == "usd") // $1.00 in cents
        {
            if (user.Subscription == null)
            {
                user.Subscription = new UserSubscription
                {
                    UserId = user.Id,
                    Tier = SubscriptionTier.Pro,
                    State = SubscriptionState.Trial,
                    CurrentPeriodStart = DateTime.UtcNow,
                    CurrentPeriodEnd = DateTime.UtcNow.AddDays(SubscriptionConstants.TrialDays),
                    TrialEndsAt = DateTime.UtcNow.AddDays(SubscriptionConstants.TrialDays),
                    Entitlements = EntitlementConstants.ProEntitlements
                };
                db.Subscriptions.Add(user.Subscription);
            }
            else
            {
                user.Subscription.State = SubscriptionState.Trial;
                user.Subscription.TrialEndsAt = DateTime.UtcNow.AddDays(SubscriptionConstants.TrialDays);
                user.Subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(SubscriptionConstants.TrialDays);
            }
            
            user.Tier = SubscriptionTier.Pro;
            user.SubscriptionState = SubscriptionState.Trial;
            user.SubscriptionEndsAt = DateTime.UtcNow.AddDays(SubscriptionConstants.TrialDays);
            user.UpdatedAt = DateTime.UtcNow;
            
            logger.LogInformation("Activation payment succeeded for user {UserId}, trial started", user.Id);
        }
    }
    
    private static async Task HandlePaymentFailedAsync(Invoice? invoice, AppDbContext db, ILogger logger)
    {
        if (invoice == null) return;
        
        var customerId = invoice.CustomerId;
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        
        if (user == null || user.Subscription == null) return;
        
        // Move to past_due
        user.Subscription.State = SubscriptionState.PastDue;
        user.Subscription.UpdatedAt = DateTime.UtcNow;
        
        user.SubscriptionState = SubscriptionState.PastDue;
        user.UpdatedAt = DateTime.UtcNow;
        
        logger.LogWarning("Payment failed for user {UserId}, invoice {InvoiceId}", user.Id, invoice.Id);
    }
    
    private static async Task HandleActivationPaymentAsync(PaymentIntent? paymentIntent, AppDbContext db, ILogger logger)
    {
        if (paymentIntent == null) return;
        
        var userId = paymentIntent.Metadata.GetValueOrDefault(StripeConstants.MetadataKeys.UserId);
        if (string.IsNullOrEmpty(userId)) return;
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null) return;
        
        // This handles the $1 activation charge
        if (paymentIntent.Amount == 100 && paymentIntent.Currency == "usd")
        {
            logger.LogInformation("Activation payment intent succeeded for user {UserId}", userId);
            // The invoice.payment_succeeded will handle the actual subscription creation
        }
    }
}