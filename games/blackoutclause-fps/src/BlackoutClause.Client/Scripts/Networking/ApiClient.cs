using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using BlackoutClause.Shared.Constants;
using BlackoutClause.Shared.DTOs;
using BlackoutClause.Shared.Enums;
using Godot;
using HttpClient = System.Net.Http.HttpClient;

namespace BlackoutClause.Client.Networking;

/// <summary>
/// HTTP API client for communicating with the BlackoutClause backend.
/// </summary>
public partial class ApiClient : Node
{
    private HttpClient _httpClient = null!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Base URL for the API server.
    /// </summary>
    public string BaseUrl { get; private set; } = "https://localhost:5001";

    /// <inheritdoc/>
    public override void _Ready()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // Dev only!
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("X-Platform", GetPlatformString());
    }

    /// <summary>
    /// Sets the authorization header for authenticated requests.
    /// </summary>
    /// <param name="token">JWT access token.</param>
    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Synchronizes user from Clerk claims via the backend.
    /// </summary>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>User DTO or null on failure.</returns>
    public async Task<UserDto?> SyncUserAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<UserDto>(ApiConstants.Endpoints.Auth + "/sync", new { });
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>User DTO or null on failure.</returns>
    public async Task<UserDto?> GetMeAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await GetAsync<UserDto>(ApiConstants.Endpoints.Auth + "/me");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Gets the current subscription status.
    /// </summary>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>Subscription status or null on failure.</returns>
    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await GetAsync<SubscriptionStatusDto>(ApiConstants.Endpoints.Subscription + "/status");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Creates a Clerk customer portal session for subscription management.
    /// </summary>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>Portal response with URL or null on failure.</returns>
    public async Task<SubscriptionPortalResponse?> CreatePortalSessionAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<SubscriptionPortalResponse>(ApiConstants.Endpoints.Subscription + "/portal", new { });
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Checks if user has required entitlements.
    /// </summary>
    /// <param name="request">Entitlement check request.</param>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>Entitlement check response or null on failure.</returns>
    public async Task<EntitlementCheckResponse?> CheckEntitlementsAsync(EntitlementCheckRequest request, string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<EntitlementCheckResponse>(ApiConstants.Endpoints.Subscription + "/entitlements", request);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Gets list of available game servers.
    /// </summary>
    /// <returns>Server list response or null on failure.</returns>
    public async Task<ServerListResponse?> GetServersAsync()
    {
        return await GetAsync<ServerListResponse>(ApiConstants.BasePath + "/game/servers");
    }

    /// <summary>
    /// Finds a match for the current user.
    /// </summary>
    /// <param name="request">Match finding request.</param>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>Match found response or null on failure.</returns>
    public async Task<MatchFoundResponse?> FindMatchAsync(FindMatchRequest request, string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<MatchFoundResponse>(ApiConstants.BasePath + "/game/match/find", request);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    /// <summary>
    /// Gets the global leaderboard.
    /// </summary>
    /// <param name="limit">Maximum number of entries.</param>
    /// <returns>Leaderboard response or null on failure.</returns>
    public async Task<LeaderboardResponse?> GetLeaderboardAsync(int limit = 100)
    {
        return await GetAsync<LeaderboardResponse>($"{ApiConstants.BasePath}/game/leaderboard?limit={limit}");
    }

    /// <summary>
    /// Gets statistics for a specific player.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="accessToken">Clerk access token.</param>
    /// <returns>Player stats response or null on failure.</returns>
    public async Task<PlayerStatsResponse?> GetPlayerStatsAsync(string userId, string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await GetAsync<PlayerStatsResponse>($"{ApiConstants.BasePath}/game/stats/{userId}");
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }

    // Generic HTTP methods
    private async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            await HandleErrorResponse(response);
            return default;
        }
        catch (Exception ex)
        {
            GD.PushError($"API GET {endpoint} failed: {ex.Message}");
            return default;
        }
    }

    private async Task<T?> PostAsync<T>(string endpoint, object payload)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            await HandleErrorResponse(response);
            return default;
        }
        catch (Exception ex)
        {
            GD.PushError($"API POST {endpoint} failed: {ex.Message}");
            return default;
        }
    }

    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        GD.PushError($"API Error: {(int)response.StatusCode} - {content}");
    }

    private static string GetPlatformString()
    {
        if (OS.GetName() == "Windows") return "windows";
        if (OS.GetName() == "macOS") return "macos";
        if (OS.GetName() == "Linux") return "linux";
        return "unknown";
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        _httpClient?.Dispose();
        base._ExitTree();
    }
}
