using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Security;

/// <summary>Emisión de tokens de sesión.</summary>
public interface ITokenService
{
    /// <summary>Genera el token JWT firmado del usuario y su fecha de caducidad (UTC).</summary>
    (string Token, DateTime ExpiresAtUtc) Create(User user);
}
