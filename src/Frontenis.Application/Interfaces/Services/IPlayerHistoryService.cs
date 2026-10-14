using Frontenis.Application.Dtos;
using Frontenis.Application.Models;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Estadísticas históricas de los jugadores.</summary>
public interface IPlayerHistoryService
{
    /// <summary>Ranking histórico de todos los jugadores (opcionalmente de una sola competición).</summary>
    Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default);
    /// <summary>Ficha histórica de un jugador: totales, compañeros, rivales y últimos partidos.</summary>
    Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default);
}
