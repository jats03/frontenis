using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frontenis.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayerHistoryController : ControllerBase
{
    private readonly IPlayerHistoryService _history;

    public PlayerHistoryController(IPlayerHistoryService history) { _history = history; }

    /// <summary>Histórico de todos los jugadores (puntos, victorias, derrotas…), opcionalmente de una competición.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<PlayerHistoryRowDto>>> History([FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, CancellationToken ct) =>
        Ok(await _history.RankingAsync(Filter(competitionIds, trainingIds), ct));

    private static HistoryFilter Filter(int[]? competitionIds, int[]? trainingIds) =>
        new((competitionIds ?? Array.Empty<int>()).Distinct().Take(500).ToList(),
            (trainingIds ?? Array.Empty<int>()).Distinct().Take(500).ToList());

    /// <summary>Ficha histórica de un jugador: compañeros, rivales y últimos partidos.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<PlayerHistoryDto>> PlayerHistory(int id, [FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, CancellationToken ct) =>
        Ok(await _history.PlayerAsync(id, Filter(competitionIds, trainingIds), ct));
}
