using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
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
    public async Task<ActionResult<IReadOnlyList<PlayerHistoryRowDto>>> History([FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, [FromQuery] BallType[]? balls, CancellationToken ct) =>
        Ok(await _history.RankingAsync(Filter(competitionIds, trainingIds, balls), ct));

    /// <summary>Una o varias bolas, o ninguna (= todas, para la carta del jugador). Un valor desconocido es un error de solicitud.</summary>
    private static HistoryFilter Filter(int[]? competitionIds, int[]? trainingIds, BallType[]? balls)
    {
        if (balls is not null && balls.Any(b => !Enum.IsDefined(b))) throw new DomainException("Tipo de bola no válido.");
        return new((competitionIds ?? Array.Empty<int>()).Distinct().Take(500).ToList(),
            (trainingIds ?? Array.Empty<int>()).Distinct().Take(500).ToList(), balls?.Distinct().ToList());
    }

    /// <summary>Ficha histórica de un jugador: compañeros, rivales y últimos partidos.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<PlayerHistoryDto>> PlayerHistory(int id, [FromQuery] int[]? competitionIds, [FromQuery] int[]? trainingIds, [FromQuery] BallType[]? balls, CancellationToken ct) =>
        Ok(await _history.PlayerAsync(id, Filter(competitionIds, trainingIds, balls), ct));
}
