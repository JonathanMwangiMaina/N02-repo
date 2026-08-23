namespace BlackoutClause.Server.Infrastructure.Clerk;

using System.Net.Http.Json;
using System.Text.Json;
using BlackoutClause.Server.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Client for interacting with the Clerk REST API.
/// </summary>
public interface IClerkClient
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Clerk user or null if not found.</returns>
    Task<ClerkUser?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of Clerk users.</returns>
    Task<ClerkUser[]> GetUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an organization by ID.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Clerk organization or null if not found.</returns>
    Task<ClerkOrganization?> GetOrganizationAsync(string orgId, CancellationToken ct = default);

    /// <summary>
    /// Gets organization memberships.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of organization memberships.</returns>
    Task<ClerkOrganizationMembership[]> GetOrganizationMembershipsAsync(string orgId, CancellationToken ct = default);

    /// <summary>
    /// Gets a subscription by ID.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Clerk subscription or null if not found.</returns>
    Task<ClerkSubscription?> GetSubscriptionAsync(string subscriptionId, CancellationToken ct = default);

    /// <summary>
    /// Gets subscriptions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of Clerk subscriptions.</returns>
    Task<ClerkSubscription[]> GetSubscriptionsForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Verifies a Clerk webhook signature using Svix HMAC-SHA256.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="signature">The Svix signature header value.</param>
    /// <returns>True if signature is valid.</returns>
    bool VerifyWebhookSignature(string payload, string signature);
}

/// <summary>
/// HTTP client implementation for Clerk REST API.
/// </summary>
public class ClerkClient : IClerkClient
{
    private readonly HttpClient _http;
    private readonly ClerkSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClerkClient"/> class.
    /// </summary>
    /// <param name="http">HTTP client for API requests.</param>
    /// <param name="settings">Clerk configuration settings.</param>
    public ClerkClient(HttpClient http, IOptions<ClerkSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        _http.BaseAddress = new Uri(_settings.ApiUrl);
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.SecretKey);
    }

    /// <inheritdoc/>
    public async Task<ClerkUser?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"users/{userId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ClerkUser>(_jsonOptions, ct);
    }

    /// <inheritdoc/>
    public async Task<ClerkUser[]> GetUsersAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("users", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<ClerkUser[]>(_jsonOptions, ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<ClerkOrganization?> GetOrganizationAsync(string orgId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"organizations/{orgId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ClerkOrganization>(_jsonOptions, ct);
    }

    /// <inheritdoc/>
    public async Task<ClerkOrganizationMembership[]> GetOrganizationMembershipsAsync(string orgId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"organizations/{orgId}/memberships", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<ClerkOrganizationMembership[]>(_jsonOptions, ct) ?? [];
    }

    /// <inheritdoc/>
    public async Task<ClerkSubscription?> GetSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"subscriptions/{subscriptionId}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ClerkSubscription>(_jsonOptions, ct);
    }

    /// <inheritdoc/>
    public async Task<ClerkSubscription[]> GetSubscriptionsForUserAsync(string userId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"users/{userId}/subscriptions", ct);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<ClerkSubscription[]>(_jsonOptions, ct) ?? [];
    }

    /// <inheritdoc/>
    public bool VerifyWebhookSignature(string payload, string signature)
    {
        // Clerk uses Svix for webhook signatures
        // Verify using HMAC-SHA256 with the webhook secret
        try
        {
            // Svix signature format: "v1,<timestamp> <signature>"
            var parts = signature.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            var versionAndTimestamp = parts[0];
            var sig = parts[1];

            // Extract timestamp (format: "v1,<timestamp>")
            var timestampParts = versionAndTimestamp.Split(',');
            if (timestampParts.Length < 2) return false;
            var timestamp = timestampParts[1];

            // Verify: HMAC-SHA256(secret, timestamp + "." + payload)
            var key = System.Text.Encoding.UTF8.GetBytes(_settings.WebhookSecret);
            var message = System.Text.Encoding.UTF8.GetBytes($"{timestamp}.{payload}");

            using var hmac = new System.Security.Cryptography.HMACSHA256(key);
            var computedHash = hmac.ComputeHash(message);
            var computedSig = Convert.ToBase64String(computedHash);

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(sig),
                System.Text.Encoding.UTF8.GetBytes(computedSig));
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Clerk user model from API response.
/// </summary>
public class ClerkUser
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// User's username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's profile image URL.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether email is verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Whether phone is verified.
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last sign-in timestamp.
    /// </summary>
    public DateTime? LastSignInAt { get; set; }

    /// <summary>
    /// Public metadata (readable by frontend).
    /// </summary>
    public Dictionary<string, object> PublicMetadata { get; set; } = new();

    /// <summary>
    /// Private metadata (backend only).
    /// </summary>
    public Dictionary<string, object> PrivateMetadata { get; set; } = new();

    /// <summary>
    /// Unsafe metadata (writable by frontend).
    /// </summary>
    public Dictionary<string, object> UnsafeMetadata { get; set; } = new();
}

/// <summary>
/// Clerk organization model from API response.
/// </summary>
public class ClerkOrganization
{
    /// <summary>
    /// Unique organization identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organization name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization slug (URL-friendly).
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Organization image URL.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Public metadata.
    /// </summary>
    public Dictionary<string, object> PublicMetadata { get; set; } = new();

    /// <summary>
    /// Private metadata.
    /// </summary>
    public Dictionary<string, object> PrivateMetadata { get; set; } = new();
}

/// <summary>
/// Clerk organization membership model from API response.
/// </summary>
public class ClerkOrganizationMembership
{
    /// <summary>
    /// Unique membership identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organization identifier.
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// User identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Membership role (admin, member, moderator).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Public metadata.
    /// </summary>
    public Dictionary<string, object> PublicMetadata { get; set; } = new();

    /// <summary>
    /// Private metadata.
    /// </summary>
    public Dictionary<string, object> PrivateMetadata { get; set; } = new();
}

/// <summary>
/// Clerk subscription model from API response.
/// </summary>
public class ClerkSubscription
{
    /// <summary>
    /// Unique subscription identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Subscription status (active, canceled, past_due, trialing).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Plan identifier.
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Current period start timestamp.
    /// </summary>
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>
    /// Current period end timestamp.
    /// </summary>
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Trial start timestamp.
    /// </summary>
    public DateTime? TrialStart { get; set; }

    /// <summary>
    /// Trial end timestamp.
    /// </summary>
    public DateTime? TrialEnd { get; set; }

    /// <summary>
    /// Cancellation timestamp.
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// Whether subscription cancels at period end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; set; }

    /// <summary>
    /// Subscription metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
