namespace IndieFps.Server.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IndieFps.Server.Configuration;
using IndieFps.Server.Domain.Entities;
using IndieFps.Shared.Constants;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public interface IJwtService
{
    string GenerateAccessToken(User user, UserSubscription? subscription, string sessionId, string platform);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    string HashToken(string token);
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;
    
    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
    }
    
    public string GenerateAccessToken(User user, UserSubscription? subscription, string sessionId, string platform)
    {
        var claims = new List<Claim>
        {
            new(JwtConstants.Claims.UserId, user.Id),
            new(JwtConstants.Claims.Email, user.Email),
            new(JwtConstants.Claims.Username, user.Username),
            new(JwtConstants.Claims.Tier, subscription?.Tier.ToString() ?? SubscriptionTier.Free.ToString()),
            new(JwtConstants.Claims.SubscriptionState, subscription?.State.ToString() ?? SubscriptionState.Unpaid.ToString()),
            new(JwtConstants.Claims.SessionId, sessionId),
            new(JwtConstants.Claims.Platform, platform),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        if (subscription?.Entitlements?.Length > 0)
        {
            claims.Add(new(JwtConstants.Claims.Entitlements, string.Join(',', subscription.Entitlements)));
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenLifetimeMinutes),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
    
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);
            
            return principal;
        }
        catch
        {
            return null;
        }
    }
    
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}