using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Plantillas semanales de entrenamiento (solo administrador).</summary>
[ApiController, AdminOnly]
[Route("api/slot-templates")]
public sealed class SlotTemplatesController : ControllerBase
{
    private readonly ISlotTemplateQueries _queries;
    private readonly ISlotTemplateCommands _commands;

    public SlotTemplatesController(ISlotTemplateQueries queries, ISlotTemplateCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    /// <summary>Lista las plantillas, activas o no.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SlotTemplateDto>>> List(CancellationToken ct) =>
        Ok(await _queries.ListAsync(ct));

    /// <summary>Crea una plantilla.</summary>
    [HttpPost]
    public async Task<ActionResult<SlotTemplateDto>> Create(SaveSlotTemplateRequest request, CancellationToken ct) =>
        Ok(await _commands.CreateAsync(request, ct));

    /// <summary>Modifica una plantilla; las franjas ya generadas no cambian.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SlotTemplateDto>> Update(int id, SaveSlotTemplateRequest request, CancellationToken ct) =>
        Ok(await _commands.UpdateAsync(id, request, ct));

    /// <summary>Elimina una plantilla; las franjas ya generadas se conservan.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commands.DeleteAsync(id, ct);
        return NoContent();
    }
}
