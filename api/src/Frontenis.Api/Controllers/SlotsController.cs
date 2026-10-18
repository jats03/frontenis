using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Franjas de entrenamiento: consulta pública y gestión del administrador.</summary>
[ApiController]
[Route("api/slots")]
public sealed class SlotsController : ControllerBase
{
    private readonly ISlotQueries _queries;
    private readonly ISlotAdminCommands _commands;
    private readonly ISlotScheduleService _schedule;

    public SlotsController(ISlotQueries queries, ISlotAdminCommands commands, ISlotScheduleService schedule)
    {
        _queries = queries;
        _commands = commands;
        _schedule = schedule;
    }

    /// <summary>
    /// Franjas entre dos fechas. Sin fechas: esta semana y la siguiente. El público solo ve la semana en curso y las
    /// franjas abiertas; el administrador (con su sesión) las ve todas.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        Ok(await _queries.ListAsync(from, to, User.IsInRole(Roles.Admin) ? SlotAudience.Admin : SlotAudience.Public, ct));

    /// <summary>Crea una franja suelta, por ejemplo un día extra (administrador).</summary>
    [HttpPost, AdminOnly]
    public async Task<ActionResult<SlotDto>> Create(SaveSlotRequest request, CancellationToken ct) =>
        Ok(await _commands.CreateAsync(request, ct));

    /// <summary>Cambia fecha, horario y apertura de una franja (administrador).</summary>
    [HttpPut("{id:int}"), AdminOnly]
    public async Task<ActionResult<SlotDto>> Update(int id, SaveSlotRequest request, CancellationToken ct) =>
        Ok(await _commands.UpdateAsync(id, request, ct));

    /// <summary>Cancela la franja conservando los apuntados (administrador).</summary>
    [HttpPost("{id:int}/cancel"), AdminOnly]
    public async Task<ActionResult<SlotDto>> Cancel(int id, CancellationToken ct) =>
        Ok(await _commands.CancelAsync(id, ct));

    /// <summary>Deshace la cancelación (administrador).</summary>
    [HttpPost("{id:int}/restore"), AdminOnly]
    public async Task<ActionResult<SlotDto>> Restore(int id, CancellationToken ct) =>
        Ok(await _commands.RestoreAsync(id, ct));

    /// <summary>Abre ya las inscripciones de la franja (administrador).</summary>
    [HttpPost("{id:int}/open"), AdminOnly]
    public async Task<ActionResult<SlotDto>> OpenNow(int id, CancellationToken ct) =>
        Ok(await _commands.OpenNowAsync(id, ct));

    /// <summary>Elimina la franja con sus apuntados (administrador).</summary>
    [HttpDelete("{id:int}"), AdminOnly]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commands.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Genera las franjas de las próximas semanas a partir de las plantillas (administrador). Idempotente.</summary>
    [HttpPost("generate"), AdminOnly]
    public async Task<ActionResult<GenerateSlotsResultDto>> Generate(GenerateSlotsRequest request, CancellationToken ct) =>
        Ok(await _schedule.GenerateAsync(request, ct));

    /// <summary>Deshace «Abrir ya»: la franja vuelve a quedar sin abrir hasta su hora habitual (administrador).</summary>
    [HttpPost("{id:int}/close"), AdminOnly]
    public async Task<ActionResult<SlotDto>> CloseSignUps(int id, CancellationToken ct) =>
        Ok(await _commands.CloseSignUpsAsync(id, ct));

    /// <summary>Vuelve a cerrar las inscripciones abiertas antes de tiempo en toda una semana (administrador).</summary>
    [HttpPost("close-week"), AdminOnly]
    public async Task<ActionResult<CloseWeekResultDto>> CloseWeek(OpenWeekRequest request, CancellationToken ct) =>
        Ok(await _schedule.CloseWeekAsync(request, ct));

    /// <summary>Abre ya las inscripciones de toda una semana (administrador).</summary>
    [HttpPost("open-week"), AdminOnly]
    public async Task<ActionResult<OpenWeekResultDto>> OpenWeek(OpenWeekRequest request, CancellationToken ct) =>
        Ok(await _schedule.OpenWeekAsync(request, ct));
}
