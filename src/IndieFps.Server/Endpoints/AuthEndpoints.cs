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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiConstants.Endpoints.Auth)
                       .WithTags("Authentication")
                       .RequireRateLimiting("auth");
        
        group.MapPost("/register", RegisterAsync)
             .WithName("Register")
             .Produces<AuthResponse>(StatusCodes.Status201Created)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status409Conflict);
        
        group.MapPost("/login", LoginAsync)
             .WithName("Login")
             .Produces<AuthResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
             .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests);
        
        group.MapPost("/refresh", RefreshAsync)
             .WithName("RefreshToken")
             .Produces<AuthResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapPost("/logout", LogoutAsync)
             .WithName("Logout")
             .RequireAuthorization()
             .Produces(StatusCodes.Status204NoContent)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
        
        group.MapGet("/me", GetMeAsync)
             .WithName("GetCurrentUser")
             .RequireAuthorization()
             .Produces<UserDto>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }
    
    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext db,
        IPasswordHasher hasher,
        IJwtService jwtService,
        IRefreshTokenStore tokenStore,
        IStripeService stripeService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // Validate
        if (await db.Users.AnyAsync(u => u.Email == request.Email, ct))
            return Results.Conflict(new ErrorResponse("EMAIL_EXISTS", "Email already registered"));
        
        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return Results.Conflict(new ErrorResponse("USERNAME_EXISTS", "Username already taken"));
        
        // Create user
        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = hasher.HashPassword(request.Password),
            Tier = SubscriptionTier.Free,
            SubscriptionState = SubscriptionState.Unpaid
        };
        
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        
        // Create Stripe customer
        var customerId = await stripeService.CreateCustomerAsync(user.Email, user.Username, user.Id);
        user.StripeCustomerId = customerId;
        await db.SaveChangesAsync(ct);
        
        // Create session & tokens
        var sessionId = Guid.NewGuid().ToString();
        var platform = httpContext.Request.Headers["X-Platform"].FirstOrDefault() ?? "unknown";
        
        var accessToken = jwtService.GenerateAccessToken(user, null, sessionId, platform);
        var refreshToken = jwtService.GenerateRefreshToken();
        
        await tokenStore.CreateAsync(user.Id, refreshToken, 
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.Connection.RemoteIpAddress?.ToString());
        
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = sessionId,
            DeviceInfo = httpContext.Request.Headers.UserAgent.ToString(),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            Platform = platform,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        
        var response = new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.Email, user.Username, user.Tier, user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt)
        );
        
        return Results.Created($"/api/v1/auth/me", response);
    }
    
    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        IPasswordHasher hasher,
        IJwtService jwtService,
        IRefreshTokenStore tokenStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        
        if (user == null || !hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }
        
        if (!user.IsActive)
        {
            return Results.Problem("Account is deactivated", statusCode: 403);
        }
        
        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        
        // Create session & tokens
        var sessionId = Guid.NewGuid().ToString();
        var platform = httpContext.Request.Headers["X-Platform"].FirstOrDefault() ?? "unknown";
        
        var accessToken = jwtService.GenerateAccessToken(user, user.Subscription, sessionId, platform);
        var refreshToken = jwtService.GenerateRefreshToken();
        
        await tokenStore.CreateAsync(user.Id, refreshToken,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.Connection.RemoteIpAddress?.ToString());
        
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = sessionId,
            DeviceInfo = httpContext.Request.Headers.UserAgent.ToString(),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            Platform = platform,
            ExpiresAt = DateTime.UtcNow.AddDays(request.RememberMe ? 90 : 30)
        };
        
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        
        var response = new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.Email, user.Username, user.Tier, user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt)
        );
        
        return Results.Ok(response);
    }
    
    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        AppDbContext db,
        IJwtService jwtService,
        IRefreshTokenStore tokenStore,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tokenHash = jwtService.HashToken(request.RefreshToken);
        var storedToken = await tokenStore.GetByTokenHashAsync(tokenHash);
        
        if (storedToken == null)
        {
            return Results.Unauthorized();
        }
        
        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, ct);
        
        if (user == null || !user.IsActive)
        {
            await tokenStore.RevokeAsync(tokenHash);
            return Results.Unauthorized();
        }
        
        // Rotate refresh token
        var newRefreshToken = jwtService.GenerateRefreshToken();
        await tokenStore.RevokeAsync(tokenHash, jwtService.HashToken(newRefreshToken), 
            httpContext.Connection.RemoteIpAddress?.ToString());
        await tokenStore.CreateAsync(user.Id, newRefreshToken,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.Connection.RemoteIpAddress?.ToString());
        
        // Generate new access token
        var sessionId = Guid.NewGuid().ToString();
        var platform = httpContext.Request.Headers["X-Platform"].FirstOrDefault() ?? "unknown";
        var accessToken = jwtService.GenerateAccessToken(user, user.Subscription, sessionId, platform);
        
        var response = new AuthResponse(
            accessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.Email, user.Username, user.Tier, user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt)
        );
        
        return Results.Ok(response);
    }
    
    private static async Task<IResult> LogoutAsync(
        ClaimsPrincipal user,
        IRefreshTokenStore tokenStore,
        IJwtService jwtService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = user.FindFirst(JwtConstants.Claims.UserId)?.Value;
        var sessionId = user.FindFirst(JwtConstants.Claims.SessionId)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Revoke all refresh tokens for this session
            // In production, you'd track session->refresh token mapping
            await tokenStore.RevokeAllUserTokensAsync(userId);
        }
        
        return Results.NoContent();
    }
    
    private static async Task<IResult> GetMeAsync(
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
        
        var response = new UserDto(
            user.Id, user.Email, user.Username, user.Tier, 
            user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt
        );
        
        return Results.Ok(response);
    }
}