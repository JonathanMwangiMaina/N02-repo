namespace BlackoutClause.Server.Endpoints;

using System.Security.Claims;
using BlackoutClause.Server.Infrastructure.Clerk;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Shared.Constants;
using BlackoutClause.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Authentication endpoint definitions for user synchronization and profile retrieval.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints to the route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiConstants.Endpoints.Auth)
                       .WithTags("Authentication");

        group.MapPost("/sync", SyncUserAsync)
             .WithName("SyncUser")
             .RequireAuthorization()
             .Produces<UserDto>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetMeAsync)
             .WithName("GetCurrentUser")
             .RequireAuthorization()
             .Produces<UserDto>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Synchronizes user from Clerk claims to local database.
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <param name="userSync">User synchronization service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User DTO with current subscription status.</returns>
    private static async Task<IResult> SyncUserAsync(
        ClaimsPrincipal principal,
        IUserSyncService userSync,
        CancellationToken ct)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var user = await userSync.SyncUserFromClaimsAsync(principal);
        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));

        var response = new UserDto(
            user.Id, user.Email, user.Username, user.Tier, user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt);

        return Results.Ok(response);
    }

    /// <summary>
    /// Retrieves the current authenticated user's profile.
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <param name="db">Application database context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User DTO with current subscription status.</returns>
    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken ct)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var user = await db.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));

        var response = new UserDto(
            user.Id, user.Email, user.Username, user.Tier, user.SubscriptionState, user.SubscriptionEndsAt, user.CreatedAt);

        return Results.Ok(response);
    }
}
