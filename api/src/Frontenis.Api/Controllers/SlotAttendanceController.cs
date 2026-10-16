using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Apuntados de una franja. De momento solo el administrador; el acceso de los jugadores llegará con el vínculo por dispositivo.</summary>
[ApiController, AdminOnly]
[Route("api/slots/{slotId:int}/attendees")]
public sealed class SlotAttendanceController : ControllerBase
{
    private readonly IAttendanceCommands _commands;
    public SlotAttendanceController(IAttendanceCommands commands) { _commands = commands; }

    /// <summary>Apunta a un jugador y devuelve la franja actualizada.</summary>
    [HttpPost]
    public async Task<ActionResult<SlotDto>> SignUp(int slotId, AttendanceRequest request, CancellationToken ct) =>
        Ok(await _commands.SignUpAsync(slotId, request.PlayerId, ct: ct));

    /// <summary>Quita a un jugador y devuelve la franja actualizada.</summary>
    [HttpDelete("{playerId:int}")]
    public async Task<ActionResult<SlotDto>> Withdraw(int slotId, int playerId, CancellationToken ct) =>
        Ok(await _commands.WithdrawAsync(slotId, playerId, ct));
}
