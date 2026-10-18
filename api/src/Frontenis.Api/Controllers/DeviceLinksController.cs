using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Dispositivos vinculados a jugadores (solo administrador).</summary>
[ApiController, AdminOnly]
[Route("api/device-links")]
public sealed class DeviceLinksController : ControllerBase
{
    private readonly IDeviceLinkAdminCommands _commands;
    public DeviceLinksController(IDeviceLinkAdminCommands commands) { _commands = commands; }

    /// <summary>Lista los jugadores con dispositivo vinculado.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceLinkDto>>> List(CancellationToken ct) =>
        Ok(await _commands.ListAsync(ct));

    /// <summary>Libera al jugador (por ejemplo, si cambió de móvil) para que pueda vincularse de nuevo.</summary>
    [HttpDelete("{playerId:int}")]
    public async Task<IActionResult> Release(int playerId, CancellationToken ct)
    {
        await _commands.ReleaseAsync(playerId, ct);
        return NoContent();
    }
}
