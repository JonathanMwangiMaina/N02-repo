using Godot;
using IndieFps.Shared.DTOs;
using System.Text.Json;

namespace IndieFps.Client.Networking;

public partial class AuthTokenManager : Node
{
    private SecureStorage _secureStorage = null!;
    private ApiClient _apiClient = null!;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _accessTokenExpiry;
    private System.Timers.Timer? _refreshTimer;
    
    public event Action<string, string, DateTime> OnTokensRefreshed;
    public event Action OnAuthExpired;
    
    public string? AccessToken => _accessToken;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _accessTokenExpiry;
    
    public override void _Ready()
    {
        _secureStorage = GetNode<SecureStorage>("/root/SecureStorage");
        _apiClient = GetNode<ApiClient>("/root/ApiClient");
        
        LoadTokensAsync();
    }
    
    public async void LoadTokensAsync()
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
    
    public void SetTokens(AuthResponse response)
    {
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _accessTokenExpiry = response.AccessTokenExpiresAt;
        
        _apiClient.SetAuthToken(_accessToken);
        
        _secureStorage.SaveTokensAsync(_accessToken, _refreshToken, _accessTokenExpiry);
        ScheduleRefresh();
    }
    
    public async Task<bool> RefreshTokensAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            OnAuthExpired?.Invoke();
            return false;
        }
        
        try
        {
            var request = new RefreshTokenRequest(_refreshToken);
            var response = await _apiClient.RefreshAsync(request);
            
            if (response != null)
            {
                SetTokens(response);
                OnTokensRefreshed?.Invoke(response.AccessToken, response.RefreshToken, response.AccessTokenExpiresAt);
                return true;
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"Token refresh failed: {ex.Message}");
        }
        
        OnAuthExpired?.Invoke();
        return false;
    }
    
    public void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiry = default;
        
        _apiClient.ClearAuthToken();
        _secureStorage.ClearTokensAsync();
        
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
    
    public override void _ExitTree()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        base._ExitTree();
    }
}