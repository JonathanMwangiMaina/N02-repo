namespace IndieFps.Server.Infrastructure.Auth;

using Argon2;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class Argon2PasswordHasher : IPasswordHasher
{
    private readonly Argon2Config _config;
    
    public Argon2PasswordHasher()
    {
        _config = new Argon2Config
        {
            Type = Argon2Type.DataIndependentAddressing, // Argon2id
            Version = Argon2Version.Nineteen,
            TimeCost = 3,
            MemoryCost = 65536, // 64 MB
            Lanes = 4,
            Threads = Environment.ProcessorCount,
            HashLength = 32
        };
    }
    
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        _config.Salt = salt;
        _config.Password = System.Text.Encoding.UTF8.GetBytes(password);
        
        var argon2 = new Argon2(_config);
        var hash = argon2.Hash();
        
        // Format: $argon2id$v=19$m=65536,t=3,p=4$salt$hash
        return $"$argon2id$v=19$m={_config.MemoryCost},t={_config.TimeCost},p={_config.Lanes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            // Parse the hash format
            var parts = hash.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;
            
            var paramsPart = parts[3];
            var saltB64 = parts[4];
            var hashB64 = parts[5];
            
            var paramsDict = paramsPart.Split(',')
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => int.Parse(p[1]));
            
            var salt = Convert.FromBase64String(saltB64);
            var expectedHash = Convert.FromBase64String(hashB64);
            
            var config = new Argon2Config
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,
                TimeCost = paramsDict["t"],
                MemoryCost = paramsDict["m"],
                Lanes = paramsDict["p"],
                Threads = Environment.ProcessorCount,
                HashLength = expectedHash.Length,
                Salt = salt,
                Password = System.Text.Encoding.UTF8.GetBytes(password)
            };
            
            var argon2 = new Argon2(config);
            var computedHash = argon2.Hash();
            
            return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}