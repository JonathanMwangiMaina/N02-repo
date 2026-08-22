using Godot;
using System.Security.Cryptography;
using System.Text;

namespace IndieFps.Client.Storage;

public partial class SecureStorage : Node
{
    private const string AccessTokenKey = "indiefps_access_token";
    private const string RefreshTokenKey = "indiefps_refresh_token";
    private const string AccessTokenExpiryKey = "indiefps_access_token_expiry";
    private const string EncryptionKeyPrefix = "indiefps_key_";
    
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }
    
    public async Task SaveTokensAsync(string accessToken, string refreshToken, DateTime accessTokenExpiry)
    {
        await Task.Run(() =>
        {
            try
            {
                var encryptedAccess = Encrypt(accessToken);
                var encryptedRefresh = Encrypt(refreshToken);
                
                OS.SetEnvironmentVariable(AccessTokenKey, encryptedAccess);
                OS.SetEnvironmentVariable(RefreshTokenKey, encryptedRefresh);
                OS.SetEnvironmentVariable(AccessTokenExpiryKey, accessTokenExpiry.ToString("O"));
            }
            catch (Exception ex)
            {
                GD.PushError($"Failed to save tokens: {ex.Message}");
            }
        });
    }
    
    public async Task<string?> GetAccessTokenAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var encrypted = OS.GetEnvironmentVariable(AccessTokenKey);
                return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
            }
            catch
            {
                return null;
            }
        });
    }
    
    public async Task<string?> GetRefreshTokenAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var encrypted = OS.GetEnvironmentVariable(RefreshTokenKey);
                return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
            }
            catch
            {
                return null;
            }
        });
    }
    
    public async Task<string?> GetAccessTokenExpiryAsync()
    {
        return await Task.Run(() =>
        {
            return OS.GetEnvironmentVariable(AccessTokenExpiryKey);
        });
    }
    
    public async Task ClearTokensAsync()
    {
        await Task.Run(() =>
        {
            OS.SetEnvironmentVariable(AccessTokenKey, "");
            OS.SetEnvironmentVariable(RefreshTokenKey, "");
            OS.SetEnvironmentVariable(AccessTokenExpiryKey, "");
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
        var machineId = OS.GetUniqueId() + OS.GetName() + Environment.MachineName;
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
    }
}