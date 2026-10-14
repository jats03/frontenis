using System.Text;
using Frontenis.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Frontenis.Api.Extensions;

/// <summary>Configura la autenticación JWT y la política de administrador.</summary>
public static class AuthenticationExtensions
{
    private const int MinKeyBytes = 32;
    private const string SampleKeyPrefix = "CAMBIA-ESTA-CLAVE";

    /// <summary>Autenticación por JWT (HS256 fijado) y autorización por roles.</summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
        EnsureKeyIsSafe(jwt, environment);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, // evita ataques de confusión de algoritmo ("none", etc.)
                ClockSkew = TimeSpan.FromSeconds(30)
            });
        services.AddAuthorization();
        return services;
    }

    private static void EnsureKeyIsSafe(JwtOptions jwt, IHostEnvironment environment)
    {
        if (Encoding.UTF8.GetByteCount(jwt.Key) < MinKeyBytes)
            throw new InvalidOperationException($"Jwt:Key debe tener al menos {MinKeyBytes} caracteres.");
        // La clave de ejemplo del repositorio es pública: con ella cualquiera podría fabricar un token de administrador.
        if (!environment.IsDevelopment() && jwt.Key.StartsWith(SampleKeyPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Jwt:Key es la clave de ejemplo. Define una clave secreta propia (variable de entorno Jwt__Key, p. ej. 64 caracteres aleatorios).");
    }
}
