using Godot;
using IndieFps.Shared.DTOs;
using IndieFps.Shared.Enums;
using IndieFps.Shared.Constants;
using IndieFps.Client.Networking;
using IndieFps.Client.Storage;

namespace IndieFps.Client.Subscription;

public partial class SubscriptionManager : Node
{
    private ApiClient _apiClient = null!;
    private AuthTokenManager _authManager = null!;
    private SecureStorage _secureStorage = null!;
    private LocalDb _localDb = null!;
    
    private SubscriptionStatusDto? _cachedStatus;
    private string? _currentUserId;
    private System.Timers.Timer? _heartbeatTimer;
    private bool _isChecking = false;
    
    public event Action<SubscriptionStatusDto> OnStatusChanged;
    public event Action<bool> OnEntitlementChanged; // hasProAccess
    
    public SubscriptionStatusDto? CachedStatus => _cachedStatus;
    public bool IsPro => _cachedStatus?.Tier == SubscriptionTier.Pro 
                      && _cachedStatus.State is SubscriptionState.Active or SubscriptionState.Trial;
    public bool HasActiveEntitlement => _cachedStatus?.HasActiveEntitlement ?? false;
    
    public override void _Ready()
    {
        _apiClient = GetNode<ApiClient>("/root/ApiClient");
        _authManager = GetNode<AuthTokenManager>("/root/AuthTokenManager");
        _secureStorage = GetNode<SecureStorage>("/root/SecureStorage");
        _localDb = GetNode<LocalDb>("/root/LocalDb");
        
        _authManager.OnTokensRefreshed += OnTokensRefreshed;
        _authManager.OnAuthExpired += OnAuthExpired;
        
        LoadCachedStatus();
        StartHeartbeat();
    }
    
    private void OnTokensRefreshed(string accessToken, string refreshToken, DateTime expiry)
    {
        if (!string.IsNullOrEmpty(_currentUserId))
        {
            _ = CheckStatusAsync(true);
        }
    }
    
    private void OnAuthExpired()
    {
        _cachedStatus = GetDemoStatus();
        _currentUserId = null;
        OnStatusChanged?.Invoke(_cachedStatus);
        OnEntitlementChanged?.Invoke(false);
    }
    
    public void SetUserId(string userId)
    {
        _currentUserId = userId;
        _ = CheckStatusAsync(true);
    }
    
    public async Task<SubscriptionStatusDto> CheckStatusAsync(bool forceRefresh = false)
    {
        if (_isChecking) return _cachedStatus ?? GetDemoStatus();
        if (string.IsNullOrEmpty(_currentUserId)) return GetDemoStatus();
        if (!forceRefresh && _cachedStatus != null && !IsCacheStale()) return _cachedStatus;
        
        _isChecking = true;
        
        try
        {
            var accessToken = await _secureStorage.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _cachedStatus = GetDemoStatus();
                return _cachedStatus;
            }
            
            var status = await _apiClient.GetSubscriptionStatusAsync(accessToken);
            
            if (status != null)
            {
                _cachedStatus = status;
                await _localDb.UpsertSubscriptionStatusAsync(status);
            }
            else
            {
                // Fallback to cached
                _cachedStatus ??= await _localDb.GetSubscriptionStatusAsync(_currentUserId);
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"Subscription check failed: {ex.Message}");
            _cachedStatus ??= await _localDb.GetSubscriptionStatusAsync(_currentUserId);
        }
        finally
        {
            _isChecking = false;
        }
        
        _cachedStatus ??= GetDemoStatus();
        OnStatusChanged?.Invoke(_cachedStatus);
        OnEntitlementChanged?.Invoke(IsPro);
        
        return _cachedStatus;
    }
    
    public bool CanAccess(string entitlement)
    {
        return _cachedStatus?.Entitlements.Contains(entitlement) ?? false;
    }
    
    public bool CanAccessLevel(string levelId)
    {
        if (IsPro) return true;
        
        // Free tier only gets tutorial
        return levelId == "tutorial" || levelId == "level_01";
    }
    
    public async Task<Stripe.Checkout.Session?> StartSubscriptionAsync(string priceId = "", string promoCode = "")
    {
        if (string.IsNullOrEmpty(_currentUserId)) return null;
        
        var accessToken = await _secureStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken)) return null;
        
        var request = new IndieFps.Shared.DTOs.CreateSubscriptionRequest(priceId, "", promoCode);
        return await _apiClient.CreateSubscriptionAsync(request, accessToken);
    }
    
    public async Task<string?> OpenCustomerPortalAsync()
    {
        var accessToken = await _secureStorage.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken)) return null;
        
        var response = await _apiClient.CreatePortalSessionAsync(accessToken);
        return response?.PortalUrl;
    }
    
    private void LoadCachedStatus()
    {
        if (string.IsNullOrEmpty(_currentUserId)) return;
        
        var cached = _localDb.GetSubscriptionStatusAsync(_currentUserId).Result;
        if (cached != null)
        {
            _cachedStatus = cached;
            OnStatusChanged?.Invoke(_cachedStatus);
            OnEntitlementChanged?.Invoke(IsPro);
        }
    }
    
    private void StartHeartbeat()
    {
        _heartbeatTimer = new System.Timers.Timer(SubscriptionConstants.HeartbeatInterval.TotalMilliseconds);
        _heartbeatTimer.Elapsed += async (sender, e) => 
        {
            if (!string.IsNullOrEmpty(_currentUserId) && _authManager.IsAuthenticated)
            {
                await CheckStatusAsync(true);
            }
        };
        _heartbeatTimer.AutoReset = true;
        _heartbeatTimer.Start();
        
        // Initial check
        if (!string.IsNullOrEmpty(_currentUserId))
        {
            _ = CheckStatusAsync(true);
        }
    }
    
    private bool IsCacheStale()
    {
        if (_cachedStatus == null) return true;
        
        var cacheAge = DateTime.UtcNow - DateTime.UtcNow; // We don't store cache timestamp yet
        return cacheAge > SubscriptionConstants.CacheExpiry;
    }
    
    private SubscriptionStatusDto GetDemoStatus()
    {
        return new SubscriptionStatusDto(
            UserId: _currentUserId ?? "anonymous",
            Tier: SubscriptionTier.Free,
            State: SubscriptionState.Unpaid,
            CurrentPeriodEnd: null,
            TrialEndsAt: null,
            HasActiveEntitlement: false,
            Entitlements: EntitlementConstants.FreeEntitlements
        );
    }
    
    public override void _ExitTree()
    {
        _heartbeatTimer?.Stop();
        _heartbeatTimer?.Dispose();
        _authManager.OnTokensRefreshed -= OnTokensRefreshed;
        _authManager.OnAuthExpired -= OnAuthExpired;
        base._ExitTree();
    }
}