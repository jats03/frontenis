using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Frontenis.Application.Interfaces.Security;
using Frontenis.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Frontenis.Infrastructure.Security;

/// <summary>Genera tokens JWT firmados con HMAC-SHA256.</summary>
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
