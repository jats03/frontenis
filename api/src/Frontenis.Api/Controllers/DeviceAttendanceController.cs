using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Apuntarse y quitarse desde un dispositivo vinculado (sin cuenta): solo al titular y a sus apadrinados.</summary>
[ApiController]
[Route("api/slots/{slotId:int}/attend")]
[EnableRateLimiting(RateLimitPolicies.PublicWrite)]
public sealed class DeviceAttendanceController : ControllerBase
{
    private readonly IDeviceAttendanceCommands _commands;
    public DeviceAttendanceController(IDeviceAttendanceCommands commands) { _commands = commands; }

    /// <summary>Apunta a un jugador (el titular o un apadrinado) y devuelve la franja actualizada.</summary>
    [HttpPost]
    public async Task<ActionResult<SlotDto>> SignUp(int slotId, [FromHeader(Name = DeviceController.TokenHeader)] string? token,
        AttendanceRequest request, CancellationToken ct) =>
        Ok(await _commands.SignUpAsync(token, slotId, request.PlayerId, ct));

    /// <summary>Quita a un jugador y devuelve la franja actualizada.</summary>
    [HttpDelete("{playerId:int}")]
    public async Task<ActionResult<SlotDto>> Withdraw(int slotId, int playerId,
        [FromHeader(Name = DeviceController.TokenHeader)] string? token, CancellationToken ct) =>
        Ok(await _commands.WithdrawAsync(token, slotId, playerId, ct));
}
