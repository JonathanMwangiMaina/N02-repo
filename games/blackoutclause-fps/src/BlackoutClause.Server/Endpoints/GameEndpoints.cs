namespace BlackoutClause.Server.Endpoints;

using System.Security.Claims;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Shared.Constants;
using BlackoutClause.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Game-related endpoints for matchmaking, leaderboards, and player statistics.
/// </summary>
public static class GameEndpoints
{
    /// <summary>
    /// Maps game endpoints to the route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiConstants.BasePath + "/game")
                       .WithTags("Game")
                       .RequireAuthorization();

        group.MapGet("/servers", GetServersAsync)
             .WithName("GetGameServers")
             .Produces<ServerListResponse>(StatusCodes.Status200OK);

        group.MapPost("/match/find", FindMatchAsync)
             .WithName("FindMatch")
             .Produces<MatchFoundResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/match/{matchId}/result", SubmitMatchResultAsync)
             .WithName("SubmitMatchResult")
             .Produces(StatusCodes.Status204NoContent)
             .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/leaderboard", GetLeaderboardAsync)
             .WithName("GetLeaderboard")
             .Produces<LeaderboardResponse>(StatusCodes.Status200OK);

        group.MapGet("/stats/{userId}", GetPlayerStatsAsync)
             .WithName("GetPlayerStats")
             .Produces<PlayerStatsResponse>(StatusCodes.Status200OK)
             .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Gets list of available game servers.
    /// </summary>
    /// <param name="db">Application database context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of game servers.</returns>
    private static async Task<IResult> GetServersAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        // TODO: Implement actual server listing from dedicated server registry
        await Task.CompletedTask;
        var response = new ServerListResponse([]);
        return Results.Ok(response);
    }

    /// <summary>
    /// Finds a match for the current user.
    /// </summary>
    /// <param name="request">Match finding request parameters.</param>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Match found response with connection details.</returns>
    private static async Task<IResult> FindMatchAsync(
        FindMatchRequest request,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        // TODO: Implement matchmaking logic
        await Task.CompletedTask;
        var response = new MatchFoundResponse(
            MatchId: Guid.NewGuid().ToString(),
            ServerAddress: "wss://game.blackoutclause.dev",
            ServerPort: 443,
            Ticket: "match_ticket_" + Guid.NewGuid().ToString("N")[..16]);

        return Results.Ok(response);
    }

    /// <summary>
    /// Submits match result for the specified match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="request">Match result request data.</param>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    private static async Task<IResult> SubmitMatchResultAsync(
        string matchId,
        MatchResultRequest request,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        // TODO: Validate and store match result
        await Task.CompletedTask;
        return Results.NoContent();
    }

    /// <summary>
    /// Gets the global leaderboard.
    /// </summary>
    /// <param name="db">Application database context.</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Leaderboard entries.</returns>
    private static async Task<IResult> GetLeaderboardAsync(
        AppDbContext db,
        int limit = 100,
        CancellationToken ct = default)
    {
        // TODO: Implement leaderboard query
        await Task.CompletedTask;
        var response = new LeaderboardResponse([]);
        return Results.Ok(response);
    }

    /// <summary>
    /// Gets statistics for a specific player.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="db">Application database context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Player statistics.</returns>
    private static async Task<IResult> GetPlayerStatsAsync(
        string userId,
        AppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return Results.NotFound(new ErrorResponse("USER_NOT_FOUND", "User not found"));

        // TODO: Implement stats query
        var response = new PlayerStatsResponse(
            UserId: user.Id,
            Username: user.Username,
            Kills: 0,
            Deaths: 0,
            Wins: 0,
            Losses: 0,
            PlayTime: TimeSpan.Zero);

        return Results.Ok(response);
    }
}

/// <summary>
/// Request to find a match with optional preferences.
/// </summary>
/// <param name="Mode">Game mode (casual, ranked, etc.).</param>
/// <param name="Region">Preferred region for matchmaking.</param>
/// <param name="PreferredMaps">Optional list of preferred maps.</param>
public record FindMatchRequest(
    string Mode = "casual",
    string? Region = null,
    string[]? PreferredMaps = null);

/// <summary>
/// Response when a match is found.
/// </summary>
/// <param name="MatchId">Unique match identifier.</param>
/// <param name="ServerAddress">Game server WebSocket address.</param>
/// <param name="ServerPort">Game server port.</param>
/// <param name="Ticket">Match join ticket.</param>
public record MatchFoundResponse(
    string MatchId,
    string ServerAddress,
    int ServerPort,
    string Ticket);

/// <summary>
/// Request to submit match results.
/// </summary>
/// <param name="MatchId">Match identifier.</param>
/// <param name="WinnerTeamId">Winning team identifier.</param>
/// <param name="PlayerStats">Per-player match statistics.</param>
/// <param name="Duration">Match duration.</param>
public record MatchResultRequest(
    string MatchId,
    string WinnerTeamId,
    Dictionary<string, PlayerMatchStats> PlayerStats,
    TimeSpan Duration);

/// <summary>
/// Individual player match statistics.
/// </summary>
/// <param name="Kills">Number of kills.</param>
/// <param name="Deaths">Number of deaths.</param>
/// <param name="Assists">Number of assists.</param>
/// <param name="Score">Total match score.</param>
/// <param name="PlayTime">Time spent in match.</param>
public record PlayerMatchStats(
    int Kills,
    int Deaths,
    int Assists,
    int Score,
    TimeSpan PlayTime);

/// <summary>
/// List of available game servers.
/// </summary>
/// <param name="Servers">Array of server information.</param>
public record ServerListResponse(ServerInfo[] Servers);

/// <summary>
/// Game server information.
/// </summary>
/// <param name="Id">Server unique identifier.</param>
/// <param name="Name">Server display name.</param>
/// <param name="Address">Server connection address.</param>
/// <param name="Port">Server connection port.</param>
/// <param name="CurrentPlayers">Current player count.</param>
/// <param name="MaxPlayers">Maximum player capacity.</param>
/// <param name="Map">Current map name.</param>
/// <param name="Mode">Current game mode.</param>
/// <param name="Region">Server region.</param>
/// <param name="Ping">Average ping in milliseconds.</param>
public record ServerInfo(
    string Id,
    string Name,
    string Address,
    int Port,
    int CurrentPlayers,
    int MaxPlayers,
    string Map,
    string Mode,
    string Region,
    int Ping);

/// <summary>
/// Leaderboard response with ranked entries.
/// </summary>
/// <param name="Entries">Array of leaderboard entries.</param>
public record LeaderboardResponse(LeaderboardEntry[] Entries);

/// <summary>
/// Single leaderboard entry.
/// </summary>
/// <param name="Rank">Player rank (1-based).</param>
/// <param name="UserId">Player user ID.</param>
/// <param name="Username">Player username.</param>
/// <param name="Score">Player score.</param>
/// <param name="Wins">Number of wins.</param>
/// <param name="Losses">Number of losses.</param>
/// <param name="KdRatio">Kill/Death ratio.</param>
public record LeaderboardEntry(
    int Rank,
    string UserId,
    string Username,
    int Score,
    int Wins,
    int Losses,
    double KdRatio);

/// <summary>
/// Player statistics response.
/// </summary>
/// <param name="UserId">Player user ID.</param>
/// <param name="Username">Player username.</param>
/// <param name="Kills">Total kills.</param>
/// <param name="Deaths">Total deaths.</param>
/// <param name="Wins">Total wins.</param>
/// <param name="Losses">Total losses.</param>
/// <param name="PlayTime">Total play time.</param>
public record PlayerStatsResponse(
    string UserId,
    string Username,
    int Kills,
    int Deaths,
    int Wins,
    int Losses,
    TimeSpan PlayTime);
