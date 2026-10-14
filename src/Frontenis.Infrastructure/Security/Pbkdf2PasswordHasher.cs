using System.Security.Cryptography;
using Frontenis.Application.Interfaces.Security;

namespace Frontenis.Infrastructure.Security;

/// <summary>PBKDF2-SHA256 con sal aleatoria. Formato: iteraciones.sal.hash (base64).</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    // OWASP: 600.000 iteraciones como mínimo para PBKDF2-HMAC-SHA256. Los hashes antiguos se regeneran al iniciar sesión.
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations < 1 || iterations > 5_000_000) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    public bool NeedsRehash(string hash)
    {
        var parts = hash.Split('.');
        return parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations < Iterations;
    }
}
