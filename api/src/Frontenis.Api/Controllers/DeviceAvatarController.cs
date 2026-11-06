using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Cambiar el avatar desde un dispositivo vinculado (sin cuenta): el del titular y el de sus apadrinados.</summary>
[ApiController]
[Route("api/device/avatar")]
[EnableRateLimiting(RateLimitPolicies.PublicWrite)]
public sealed class DeviceAvatarController : ControllerBase
{
    private readonly IAvatarService _avatars;
    public DeviceAvatarController(IAvatarService avatars) { _avatars = avatars; }

    /// <summary>Cambia el avatar (1 a 36) o lo deja como el de siempre (vacío).</summary>
    [HttpPut("{playerId:int}")]
    public async Task<ActionResult<AvatarDto>> Set(int playerId, [FromHeader(Name = DeviceController.TokenHeader)] string? token,
        SaveAvatarRequest request, CancellationToken ct) =>
        Ok(await _avatars.SetForDeviceAsync(token, playerId, request.Avatar, ct));
}
