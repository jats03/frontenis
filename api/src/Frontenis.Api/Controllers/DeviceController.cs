using Frontenis.Api.Auditing.Interfaces;
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
    private readonly IActorResolver _actors;
    private readonly ILogger<DeviceController> _logger;
    public DeviceController(IDeviceLinkService devices, IActorResolver actors, ILogger<DeviceController> logger)
    {
        _devices = devices;
        _actors = actors;
        _logger = logger;
    }

    /// <summary>Jugador titular y apadrinados de este dispositivo; 204 si aún no está vinculado.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<DeviceMeDto>> Me([FromHeader(Name = TokenHeader)] string? token, CancellationToken ct)
    {
        var me = await _devices.GetMeAsync(token, ct);
        if (me is null) return NoContent();
        // Ya se ha consultado la BD para esto: se recuerda quién es el dispositivo para el registro de actividad
        // (las lecturas siguientes salen con su nombre sin consultar nada más).
        _actors.Remember(HttpContext, me.Owner.Id, me.Owner.Name);
        return Ok(me);
    }

    /// <summary>Jugadores que aún no tienen dispositivo y se pueden elegir al vincular.</summary>
    [HttpGet("available-players")]
    public async Task<ActionResult<IReadOnlyList<PlayerRefDto>>> AvailablePlayers(CancellationToken ct) =>
        Ok(await _devices.ListAvailablePlayersAsync(ct));

    /// <summary>Todos los jugadores para identificarse, marcando los que ya tienen dispositivo vinculado.</summary>
    [HttpGet("players")]
    public async Task<ActionResult<IReadOnlyList<DevicePlayerDto>>> SelectablePlayers(CancellationToken ct) =>
        Ok(await _devices.ListSelectablePlayersAsync(ct));

    /// <summary>Vincula este dispositivo con el jugador elegido.</summary>
    [HttpPost("link"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<ActionResult<DeviceMeDto>> Link([FromHeader(Name = TokenHeader)] string? token, LinkDeviceRequest request, CancellationToken ct)
    {
        var me = await _devices.LinkAsync(token, request.PlayerId, request.Replace, ct);
        // Solo al identificarse y solo en el log (nunca en la base de datos): una pista de quién se vincula y desde dónde.
        _logger.LogInformation("Dispositivo identificado como {Player} (id {PlayerId}) desde la IP {Ip}",
            me.Owner.Name, me.Owner.Id, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida");
        return Ok(me);
    }

    /// <summary>Desvincula este dispositivo («no soy yo»).</summary>
    [HttpDelete("link"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> Unlink([FromHeader(Name = TokenHeader)] string? token, CancellationToken ct)
    {
        await _devices.UnlinkAsync(token, ct);
        return NoContent();
    }
}
