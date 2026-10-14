using System.Security.Claims;
using Frontenis.Api.Authorization;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Exceptions;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IAuthService auth, ILogger<AuthController> logger) { _auth = auth; _logger = logger; }

    private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    /// <summary>Inicio de sesión del administrador. Devuelve un token JWT; limitado por intentos.</summary>
    [HttpPost("login"), EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        try { return Ok(await _auth.LoginAsync(request, ClientIp, ct)); }
        catch (Exception ex) when (ex is UnauthorizedAppException or TooManyAttemptsException)
        {
            // Se registra el intento (sin la contraseña) para poder detectar ataques en los logs de Render.
            _logger.LogWarning("Inicio de sesión rechazado ({Reason}) desde {Ip}.", ex.GetType().Name, ClientIp);
            throw;
        }
    }

    /// <summary>Cambio de contraseña del administrador con sesión iniciada.</summary>
    [HttpPost("change-password"), AdminOnly, EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(raw, out var userId)) return Unauthorized();
        await _auth.ChangePasswordAsync(userId, request, ClientIp, ct);
        _logger.LogInformation("Contraseña de administrador cambiada (usuario {UserId}).", userId);
        return NoContent();
    }
}
