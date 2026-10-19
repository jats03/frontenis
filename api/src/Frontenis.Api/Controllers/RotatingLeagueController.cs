using Frontenis.Api.Authorization;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

/// <summary>Cambios en una liga rotatoria en curso (solo administrador).</summary>
[ApiController, AdminOnly]
public sealed class RotatingLeagueController : ControllerBase
{
    private readonly IRotatingLeagueCommands _commands;
    public RotatingLeagueController(IRotatingLeagueCommands commands) { _commands = commands; }

    /// <summary>Añade un jugador al último grupo y recalcula los partidos pendientes de ese grupo.</summary>
    [HttpPost("api/competitions/{id:int}/rotating/players")]
    public async Task<ActionResult<CompetitionDetailDto>> AddPlayer(int id, AddRotatingPlayerRequest request, CancellationToken ct) =>
        Ok(await _commands.AddPlayerAsync(id, request, ct));

    /// <summary>Mueve un partido pendiente a otra jornada de su grupo.</summary>
    [HttpPut("api/matches/{matchId:int}/round")]
    public async Task<ActionResult<CompetitionDetailDto>> MoveMatch(int matchId, MoveMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.MoveMatchAsync(matchId, request, ct));

    /// <summary>Intercambia las jornadas de dos partidos pendientes del mismo grupo.</summary>
    [HttpPut("api/matches/{matchId:int}/swap")]
    public async Task<ActionResult<CompetitionDetailDto>> SwapMatches(int matchId, SwapMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.SwapMatchesAsync(matchId, request, ct));

    /// <summary>Quita una jornada de un grupo (sin partidos jugados).</summary>
    [HttpDelete("api/competitions/{id:int}/groups/{groupId:int}/rounds/{round:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> RemoveRound(int id, int groupId, int round, CancellationToken ct) =>
        Ok(await _commands.RemoveRoundAsync(id, groupId, round, ct));
}
