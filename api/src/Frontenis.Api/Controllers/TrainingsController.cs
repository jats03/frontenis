using Frontenis.Api.Authorization;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/trainings")]
public sealed class TrainingsController : ControllerBase
{
    private readonly ITrainingQueries _queries;
    private readonly ITrainingCommands _commands;
    private readonly ITrainingSuggestionService _suggestions;
    private readonly IDeviceAccessResolver _devices;

    public TrainingsController(ITrainingQueries queries, ITrainingCommands commands, ITrainingSuggestionService suggestions,
        IDeviceAccessResolver devices)
    {
        _queries = queries;
        _commands = commands;
        _suggestions = suggestions;
        _devices = devices;
    }

    /// <summary>Lista las sesiones de entrenamiento (público).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrainingSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _queries.ListAsync(ct));

    /// <summary>Detalle de una sesión: partidos y clasificaciones (público).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingDetailDto>> Get(int id, CancellationToken ct) =>
        Ok(await _queries.GetAsync(id, ct));

    /// <summary>Crea una sesión de entrenamiento (administrador).</summary>
    [HttpPost, AdminOnly]
    public async Task<ActionResult<TrainingSummaryDto>> Create(CreateTrainingRequest request, CancellationToken ct) =>
        Ok(await _commands.CreateAsync(request, ct));

    /// <summary>Elimina una sesión con sus partidos (administrador).</summary>
    [HttpDelete("{id:int}"), AdminOnly]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _commands.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Renombra la sesión (administrador).</summary>
    [HttpPut("{id:int}/name"), AdminOnly]
    public async Task<ActionResult<TrainingSummaryDto>> Rename(int id, RenameTrainingRequest request, CancellationToken ct) =>
        Ok(await _commands.RenameAsync(id, request, ct));

    /// <summary>Registrar un partido de la sesión: solo el administrador o un jugador con el dispositivo vinculado. Borrar sigue siendo solo del administrador.</summary>
    [HttpPost("{id:int}/matches"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<ActionResult<TrainingDetailDto>> AddMatch(
        int id, AddTrainingMatchRequest request, [FromHeader(Name = DeviceController.TokenHeader)] string? deviceToken, CancellationToken ct)
    {
        if (!User.IsInRole(Roles.Admin)) await _devices.ResolveAsync(deviceToken, ct); // 401 si el dispositivo no está vinculado
        return Ok(await _commands.AddMatchAsync(id, request, ct));
    }

    /// <summary>Elimina un partido de la sesión (administrador).</summary>
    [HttpDelete("{id:int}/matches/{matchId:int}"), AdminOnly]
    public async Task<ActionResult<TrainingDetailDto>> RemoveMatch(int id, int matchId, CancellationToken ct) =>
        Ok(await _commands.RemoveMatchAsync(id, matchId, ct));

    /// <summary>Solo lectura (no guarda nada): propone los próximos partidos entre los jugadores presentes.</summary>
    [HttpPost("{id:int}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<TrainingSuggestionDto>>> Suggest(int id, TrainingSuggestRequest request, CancellationToken ct) =>
        Ok(await _suggestions.SuggestAsync(id, request, ct));
}
