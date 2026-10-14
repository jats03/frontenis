using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Autenticación del administrador: inicio de sesión y cambio de contraseña.</summary>
public interface IAuthService
{
    /// <summary>Inicia sesión y devuelve el token. Limita los intentos fallidos por usuario y por IP.</summary>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">Usuario o contraseña incorrectos.</exception>
    /// <exception cref="Frontenis.Application.Exceptions.TooManyAttemptsException">Demasiados intentos fallidos: cuenta o IP bloqueadas temporalmente.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, string clientIp, CancellationToken ct = default);
    /// <summary>Cambia la contraseña del usuario autenticado, validando la actual y la política de contraseñas.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Contraseña actual incorrecta, nueva igual a la actual o que incumple la política.</exception>
    /// <exception cref="Frontenis.Application.Exceptions.UnauthorizedAppException">La sesión ya no es válida.</exception>
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, string clientIp, CancellationToken ct = default);
}
