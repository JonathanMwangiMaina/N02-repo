namespace IndieFps.Server.Infrastructure.Auth;

using IndieFps.Server.Domain.Entities;
using IndieFps.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public interface IRefreshTokenStore
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<RefreshToken> CreateAsync(string userId, string token, string? deviceInfo, string? ipAddress);
    Task RevokeAsync(string tokenHash, string? replacedByTokenHash = null, string? revokedByIp = null);
    Task RevokeAllUserTokensAsync(string userId);
    Task CleanupExpiredAsync();
}

public class RefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwtSettings;
    
    public RefreshTokenStore(AppDbContext db, IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings.Value;
    }
    
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.IsActive);
    }
    
    public async Task<RefreshToken> CreateAsync(string userId, string token, string? deviceInfo, string? ipAddress)
    {
        var tokenHash = HashToken(token);
        
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
        
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
        
        return refreshToken;
    }
    
    public async Task RevokeAsync(string tokenHash, string? replacedByTokenHash = null, string? revokedByIp = null)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
            token.ReplacedByTokenHash = replacedByTokenHash;
            await _db.SaveChangesAsync();
        }
    }
    
    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();
        
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
        
        await _db.SaveChangesAsync();
    }
    
    public async Task CleanupExpiredAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var expired = await _db.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoff || rt.RevokedAt != null)
            .ToListAsync();
        
        _db.RefreshTokens.RemoveRange(expired);
        await _db.SaveChangesAsync();
    }
    
    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}