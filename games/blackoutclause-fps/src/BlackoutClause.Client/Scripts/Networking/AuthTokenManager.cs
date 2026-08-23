using System.Text.Json;
using BlackoutClause.Client.Storage;
using BlackoutClause.Shared.DTOs;
using Godot;

namespace BlackoutClause.Client.Networking;

/// <summary>
/// Manages authentication tokens (access/refresh) with automatic refresh and secure storage.
/// </summary>
public partial class AuthTokenManager : Node
{
    private SecureStorage _secureStorage = null!;
    private ApiClient _apiClient = null!;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _accessTokenExpiry;
    private System.Timers.Timer? _refreshTimer;

    /// <summary>
    /// Fired when tokens are successfully refreshed.
    /// Event args: accessToken (string), refreshToken (string), expiry (DateTime).
    /// </summary>
    public event Action<string, string, DateTime>? OnTokensRefreshed;

    /// <summary>
    /// Fired when authentication expires and cannot be refreshed.
    /// </summary>
    public event Action? OnAuthExpired;

    /// <summary>
    /// Gets the current access token.
    /// </summary>
    public string? AccessToken => _accessToken;

    /// <summary>
    /// Gets whether the current access token is valid.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _accessTokenExpiry;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _secureStorage = GetNode<SecureStorage>("/root/SecureStorage");
        _apiClient = GetNode<ApiClient>("/root/ApiClient");

        _ = LoadTokensAsync();
    }

    /// <summary>
    /// Loads tokens from secure storage and initializes authentication state.
    /// </summary>
    public async Task LoadTokensAsync()
    {
        _accessToken = await _secureStorage.GetAccessTokenAsync();
        _refreshToken = await _secureStorage.GetRefreshTokenAsync();
        var expiryStr = await _secureStorage.GetAccessTokenExpiryAsync();

        if (DateTime.TryParse(expiryStr, out var expiry))
        {
            _accessTokenExpiry = expiry;
        }

        if (!string.IsNullOrEmpty(_accessToken))
        {
            _apiClient.SetAuthToken(_accessToken);

            if (IsAuthenticated)
            {
                ScheduleRefresh();
            }
            else if (!string.IsNullOrEmpty(_refreshToken))
            {
                await RefreshTokensAsync();
            }
            else
            {
                OnAuthExpired?.Invoke();
            }
        }
    }

    /// <summary>
    /// Sets new tokens from authentication response.
    /// </summary>
    /// <param name="response">Authentication response containing tokens.</param>
    public void SetTokens(AuthResponse response)
    {
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _accessTokenExpiry = response.AccessTokenExpiresAt;

        _apiClient.SetAuthToken(_accessToken);

        _ = _secureStorage.SaveTokensAsync(_accessToken, _refreshToken, _accessTokenExpiry);
        ScheduleRefresh();
    }

    /// <summary>
    /// Attempts to refresh tokens using the backend sync endpoint.
    /// </summary>
    /// <returns>True if refresh succeeded.</returns>
    public async Task<bool> RefreshTokensAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            OnAuthExpired?.Invoke();
            return false;
        }

        try
        {
            // Use the /auth/sync endpoint to sync user from Clerk
            if (!string.IsNullOrEmpty(_accessToken))
            {
                var response = await _apiClient.SyncUserAsync(_accessToken);

                if (response != null)
                {
                    // Update tokens from response if needed
                    OnTokensRefreshed?.Invoke(_accessToken!, _refreshToken!, _accessTokenExpiry);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"Token refresh failed: {ex.Message}");
        }

        OnAuthExpired?.Invoke();
        return false;
    }

    /// <summary>
    /// Clears all tokens and stops refresh timer.
    /// </summary>
    public async Task ClearTokensAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiry = default;

        _apiClient.ClearAuthToken();
        await _secureStorage.ClearTokensAsync();

        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    private void ScheduleRefresh()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();

        var timeUntilExpiry = _accessTokenExpiry - DateTime.UtcNow;
        var refreshIn = timeUntilExpiry - TimeSpan.FromMinutes(2); // Refresh 2 min before expiry

        if (refreshIn <= TimeSpan.Zero)
        {
            // Already expired or about to expire
            _ = RefreshTokensAsync();
            return;
        }

        _refreshTimer = new System.Timers.Timer(refreshIn.TotalMilliseconds);
        _refreshTimer.Elapsed += async (sender, e) =>
        {
            _refreshTimer?.Stop();
            await RefreshTokensAsync();
        };
        _refreshTimer.AutoReset = false;
        _refreshTimer.Start();
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        base._ExitTree();
    }
}
