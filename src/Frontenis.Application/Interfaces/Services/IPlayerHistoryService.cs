using Frontenis.Application.Dtos;
using Frontenis.Application.Models;

namespace Frontenis.Application.Interfaces.Services;

public interface IPlayerHistoryService
{
    /// <summary>Ranking histórico de todos los jugadores (opcionalmente de una sola competición).</summary>
    Task<IReadOnlyList<PlayerHistoryRowDto>> RankingAsync(HistoryFilter filter, CancellationToken ct = default);
    Task<PlayerHistoryDto> PlayerAsync(int playerId, HistoryFilter filter, CancellationToken ct = default);
}
