using Godot;
using IndieFps.Shared.DTOs;
using IndieFps.Shared.Enums;
using IndieFps.Shared.Constants;
using System.Net.Http.Json;
using System.Text.Json;

namespace IndieFps.Client.Networking;

public partial class ApiClient : Node
{
    private HttpClient _httpClient = null!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public string BaseUrl { get; private set; } = "https://localhost:5001";
    
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
    
    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
    
    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
    
    // Auth
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        return await PostAsync<AuthResponse>(ApiConstants.Endpoints.Register, request);
    }
    
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        return await PostAsync<AuthResponse>(ApiConstants.Endpoints.Login, request);
    }
    
    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request)
    {
        return await PostAsync<AuthResponse>(ApiConstants.Endpoints.Refresh, request);
    }
    
    public async Task<UserDto?> GetMeAsync()
    {
        return await GetAsync<UserDto>(ApiConstants.Endpoints.Me);
    }
    
    // Subscription
    public async Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await GetAsync<SubscriptionStatusDto>(ApiConstants.Endpoints.SubscriptionStatus);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }
    
    public async Task<Stripe.Checkout.Session?> CreateSubscriptionAsync(CreateSubscriptionRequest request, string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<Stripe.Checkout.Session>(ApiConstants.Endpoints.SubscriptionCreate, request);
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }
    
    public async Task<SubscriptionPortalResponse?> CreatePortalSessionAsync(string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<SubscriptionPortalResponse>(ApiConstants.Endpoints.SubscriptionPortal, new { });
        }
        finally
        {
            _httpClient.DefaultRequestHeaders.Authorization = originalAuth;
        }
    }
    
    public async Task<EntitlementCheckResponse?> CheckEntitlementsAsync(EntitlementCheckRequest request, string accessToken)
    {
        var originalAuth = _httpClient.DefaultRequestHeaders.Authorization;
        try
        {
            SetAuthToken(accessToken);
            return await PostAsync<EntitlementCheckResponse>(ApiConstants.Endpoints.SubscriptionEntitlements, request);
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
    
    public override void _ExitTree()
    {
        _httpClient?.Dispose();
        base._ExitTree();
    }
}