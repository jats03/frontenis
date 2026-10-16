using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>
/// Identificación de un dispositivo con su jugador, sin cuenta. El dispositivo envía su identificador aleatorio
/// en la cabecera <c>X-Device-Token</c>; el servidor solo guarda su hash.
/// </summary>
[ApiController]
[Route("api/device")]
public sealed class DeviceController : ControllerBase
{
    public const string TokenHeader = "X-Device-Token";

    private readonly IDeviceLinkService _devices;
    public DeviceController(IDeviceLinkService devices) { _devices = devices; }

    /// <summary>Jugador titular y apadrinados de este dispositivo; 204 si aún no está vinculado.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<DeviceMeDto>> Me([FromHeader(Name = TokenHeader)] string? token, CancellationToken ct)
    {
        var me = await _devices.GetMeAsync(token, ct);
        return me is null ? NoContent() : Ok(me);
    }

    /// <summary>Jugadores que aún no tienen dispositivo y se pueden elegir al vincular.</summary>
    [HttpGet("available-players")]
    public async Task<ActionResult<IReadOnlyList<PlayerRefDto>>> AvailablePlayers(CancellationToken ct) =>
        Ok(await _devices.ListAvailablePlayersAsync(ct));

    /// <summary>Vincula este dispositivo con el jugador elegido.</summary>
    [HttpPost("link"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<ActionResult<DeviceMeDto>> Link([FromHeader(Name = TokenHeader)] string? token, LinkDeviceRequest request, CancellationToken ct) =>
        Ok(await _devices.LinkAsync(token, request.PlayerId, ct));

    /// <summary>Desvincula este dispositivo («no soy yo»).</summary>
    [HttpDelete("link"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Unlink([FromHeader(Name = TokenHeader)] string? token, CancellationToken ct)
    {
        await _devices.UnlinkAsync(token, ct);
        return NoContent();
    }
}
