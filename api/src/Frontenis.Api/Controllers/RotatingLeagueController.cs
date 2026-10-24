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

    /// <summary>Saca a un jugador de un grupo si no ha jugado ningún partido.</summary>
    [HttpDelete("api/competitions/{id:int}/groups/{groupId:int}/players/{playerId:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> RemovePlayer(int id, int groupId, int playerId, CancellationToken ct) =>
        Ok(await _commands.RemovePlayerAsync(id, groupId, playerId, ct));

    /// <summary>Mueve un partido pendiente a otra jornada de su grupo.</summary>
    [HttpPut("api/matches/{matchId:int}/round")]
    public async Task<ActionResult<CompetitionDetailDto>> MoveMatch(int matchId, MoveMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.MoveMatchAsync(matchId, request, ct));

    /// <summary>Borra un partido de una jornada (con o sin resultado).</summary>
    [HttpDelete("api/matches/{matchId:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> RemoveMatch(int matchId, CancellationToken ct) =>
        Ok(await _commands.RemoveMatchAsync(matchId, ct));

    /// <summary>Añade un partido a una jornada de un grupo.</summary>
    [HttpPost("api/competitions/{id:int}/groups/{groupId:int}/matches")]
    public async Task<ActionResult<CompetitionDetailDto>> AddMatch(int id, int groupId, AddMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.AddMatchAsync(id, groupId, request, ct));

    /// <summary>Intercambia las jornadas de dos partidos pendientes del mismo grupo.</summary>
    [HttpPut("api/matches/{matchId:int}/swap")]
    public async Task<ActionResult<CompetitionDetailDto>> SwapMatches(int matchId, SwapMatchRequest request, CancellationToken ct) =>
        Ok(await _commands.SwapMatchesAsync(matchId, request, ct));

    /// <summary>Intercambia dos jornadas enteras de un grupo.</summary>
    [HttpPut("api/competitions/{id:int}/groups/{groupId:int}/rounds/swap")]
    public async Task<ActionResult<CompetitionDetailDto>> SwapRounds(int id, int groupId, SwapRoundsRequest request, CancellationToken ct) =>
        Ok(await _commands.SwapRoundsAsync(id, groupId, request, ct));

    /// <summary>Quita una jornada de un grupo (sin partidos jugados).</summary>
    [HttpDelete("api/competitions/{id:int}/groups/{groupId:int}/rounds/{round:int}")]
    public async Task<ActionResult<CompetitionDetailDto>> RemoveRound(int id, int groupId, int round, CancellationToken ct) =>
        Ok(await _commands.RemoveRoundAsync(id, groupId, round, ct));
}
