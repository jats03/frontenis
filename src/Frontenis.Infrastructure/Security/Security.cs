using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Frontenis.Application.Abstractions;
using Frontenis.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Frontenis.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "frontenis";
    public string Audience { get; set; } = "frontenis-clients";
    public string Key { get; set; } = string.Empty;   // mínimo 32 caracteres
    public int ExpiresMinutes { get; set; } = 240;
}

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

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options) { _options = options.Value; }

    public (string Token, DateTime ExpiresAtUtc) Create(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
