namespace IndieFps.Server.Endpoints;

using IndieFps.Server.Infrastructure.Auth;
using IndieFps.Server.Infrastructure.Data;
using IndieFps.Server.Infrastructure.Payments;
using IndieFps.Server.Domain.Entities;
using IndieFps.Shared.DTOs;
using IndieFps.Shared.Enums;
using IndieFps.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiConstants.Endpoints.Subscription)
                       .WithTags("Subscription")
                       .RequireAuthorization();
        
        group.MapGet("/status", GetStatusAsync)
             .WithName("GetSubscriptionStatus")
             .Produces<SubscriptionStatusDto>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapPost("/create", CreateSubscriptionAsync)
             .WithName("CreateSubscription")
             .Produces<Session>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapPost("/cancel", CancelSubscriptionAsync)
             .WithName("CancelSubscription")
             .Produces(StatusCodes.Status204NoContent)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapPost("/portal", CreatePortalSessionAsync)
             .WithName("CreatePortalSession")
             .Produces<SubscriptionPortalResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapPost("/entitlements", CheckEntitlementsAsync)
             .WithName("CheckEntitlements")
             .Produces<EntitlementCheckResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }
    
    private static async Task<IResult> GetStatusAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken ct)
    {
        var userId = principal.FindFirst(JwtConstants.Claims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));
        
        var subscription = user.Subscription;
        var now = DateTime.UtcNow;
        
        // Determine entitlements
        var entitlements = subscription?.Entitlements ?? EntitlementConstants.FreeEntitlements;
        var hasActiveEntitlement = subscription != null && 
            (subscription.State == SubscriptionState.Active || subscription.State == SubscriptionState.Trial) &&
            subscription.CurrentPeriodEnd > now;
        
        var response = new SubscriptionStatusDto(
            user.Id,
            user.Tier,
            user.SubscriptionState,
            subscription?.CurrentPeriodEnd,
            subscription?.TrialEndsAt,
            hasActiveEntitlement,
            entitlements
        );
        
        return Results.Ok(response);
    }
    
    private static async Task<IResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IStripeService stripeService,
        IConfiguration config,
        CancellationToken ct)
    {
        var userId = principal.FindFirst(JwtConstants.Claims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));
        
        if (user.Subscription?.State == SubscriptionState.Active || 
            user.Subscription?.State == SubscriptionState.Trial)
        {
            return Results.BadRequest(new ErrorResponse("ALREADY_SUBSCRIBED", "User already has an active subscription"));
        }
        
        // Determine price ID
        var priceId = request.PriceId;
        if (string.IsNullOrEmpty(priceId))
        {
            // Default to Pro monthly
            priceId = config["Stripe:ProPriceId"] ?? StripeConstants.Prices.ProMonthly;
        }
        
        var successUrl = config["App:SuccessUrl"] ?? "https://indiefps.game/subscription/success";
        var cancelUrl = config["App:CancelUrl"] ?? "https://indiefps.game/subscription/cancel";
        
        var session = await stripeService.CreateCheckoutSessionAsync(
            user.StripeCustomerId!, 
            priceId, 
            successUrl, 
            cancelUrl,
            request.PromoCode);
        
        return Results.Ok(session);
    }
    
    private static async Task<IResult> CancelSubscriptionAsync(
        CancelSubscriptionRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        IStripeService stripeService,
        CancellationToken ct)
    {
        var userId = principal.FindFirst(JwtConstants.Claims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null || user.Subscription == null)
            return Results.NotFound(new ErrorResponse("NO_SUBSCRIPTION", "No active subscription found"));
        
        if (user.Subscription.State == SubscriptionState.Cancelled || 
            user.Subscription.State == SubscriptionState.Expired)
        {
            return Results.BadRequest(new ErrorResponse("ALREADY_CANCELLED", "Subscription already cancelled"));
        }
        
        await stripeService.CancelSubscriptionAsync(user.Subscription.StripeSubscriptionId, request.CancelAtPeriodEnd);
        
        // Update local state immediately for at_period_end = false
        if (!request.CancelAtPeriodEnd)
        {
            user.Subscription.State = SubscriptionState.Cancelled;
            user.Subscription.CancelledAt = DateTime.UtcNow;
            user.Tier = SubscriptionTier.Free;
            user.SubscriptionState = SubscriptionState.Cancelled;
            await db.SaveChangesAsync(ct);
        }
        
        return Results.NoContent();
    }
    
    private static async Task<IResult> CreatePortalSessionAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        IStripeService stripeService,
        IConfiguration config,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = principal.FindFirst(JwtConstants.Claims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || string.IsNullOrEmpty(user.StripeCustomerId))
            return Results.NotFound(new ErrorResponse("NO_CUSTOMER", "No Stripe customer found"));
        
        var returnUrl = config["App:PortalReturnUrl"] ?? "https://indiefps.game/account";
        var session = await stripeService.CreatePortalSessionAsync(user.StripeCustomerId, returnUrl);
        
        return Results.Ok(new SubscriptionPortalResponse(session.Url));
    }
    
    private static async Task<IResult> CheckEntitlementsAsync(
        EntitlementCheckRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken ct)
    {
        var userId = principal.FindFirst(JwtConstants.Claims.UserId)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        
        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));
        
        var subscription = user.Subscription;
        var now = DateTime.UtcNow;
        var hasActiveSub = subscription != null && 
            (subscription.State == SubscriptionState.Active || subscription.State == SubscriptionState.Trial) &&
            subscription.CurrentPeriodEnd > now;
        
        var userEntitlements = hasActiveSub ? subscription.Entitlements : EntitlementConstants.FreeEntitlements;
        var missing = request.RequiredEntitlements.Where(e => !userEntitlements.Contains(e)).ToArray();
        
        var response = new EntitlementCheckResponse(
            missing.Length == 0,
            missing
        );
        
        return Results.Ok(response);
    }
}