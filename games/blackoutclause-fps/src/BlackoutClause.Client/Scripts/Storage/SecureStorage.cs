using System.Security.Cryptography;
using System.Text;
using Godot;
using SysEnv = System.Environment;

namespace BlackoutClause.Client.Storage;

/// <summary>
/// Secure storage for authentication tokens using machine-specific encryption.
/// Stores encrypted tokens in environment variables for cross-session persistence.
/// </summary>
public partial class SecureStorage : Node
{
    private const string AccessTokenKey = "blackoutclause_access_token";
    private const string RefreshTokenKey = "blackoutclause_refresh_token";
    private const string AccessTokenExpiryKey = "blackoutclause_access_token_expiry";
    private const string EncryptionKeyPrefix = "blackoutclause_key_";

    /// <inheritdoc/>
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Saves access and refresh tokens securely with expiration.
    /// </summary>
    /// <param name="accessToken">JWT access token.</param>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="accessTokenExpiry">Access token expiration time (UTC).</param>
    public async Task SaveTokensAsync(string accessToken, string refreshToken, DateTime accessTokenExpiry)
    {
        await Task.Run(() =>
        {
            try
            {
                var encryptedAccess = Encrypt(accessToken);
                var encryptedRefresh = Encrypt(refreshToken);

                SysEnv.SetEnvironmentVariable(AccessTokenKey, encryptedAccess);
                SysEnv.SetEnvironmentVariable(RefreshTokenKey, encryptedRefresh);
                SysEnv.SetEnvironmentVariable(AccessTokenExpiryKey, accessTokenExpiry.ToString("O"));
            }
            catch (Exception ex)
            {
                GD.PushError($"Failed to save tokens: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Retrieves the decrypted access token.
    /// </summary>
    /// <returns>Access token or null if not found/decryption fails.</returns>
    public async Task<string?> GetAccessTokenAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var encrypted = SysEnv.GetEnvironmentVariable(AccessTokenKey);
                return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Retrieves the decrypted refresh token.
    /// </summary>
    /// <returns>Refresh token or null if not found/decryption fails.</returns>
    public async Task<string?> GetRefreshTokenAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var encrypted = SysEnv.GetEnvironmentVariable(RefreshTokenKey);
                return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Retrieves the access token expiration timestamp.
    /// </summary>
    /// <returns>Expiration timestamp string or null.</returns>
    public async Task<string?> GetAccessTokenExpiryAsync()
    {
        return await Task.Run(() =>
        {
            return SysEnv.GetEnvironmentVariable(AccessTokenExpiryKey);
        });
    }

    /// <summary>
    /// Clears all stored tokens.
    /// </summary>
    public async Task ClearTokensAsync()
    {
        await Task.Run(() =>
        {
            SysEnv.SetEnvironmentVariable(AccessTokenKey, "");
            SysEnv.SetEnvironmentVariable(RefreshTokenKey, "");
            SysEnv.SetEnvironmentVariable(AccessTokenExpiryKey, "");
        });
    }

    // Simple encryption using machine-specific key
    private string Encrypt(string plainText)
    {
        var key = GetMachineKey();
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string Decrypt(string cipherText)
    {
        var key = GetMachineKey();
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[aes.IV.Length];
        var cipher = new byte[cipherBytes.Length - iv.Length];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private byte[] GetMachineKey()
    {
        // Generate a machine-specific key from hardware info
        var machineId = OS.GetUniqueId() + OS.GetName() + SysEnv.MachineName;
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
    }
}
