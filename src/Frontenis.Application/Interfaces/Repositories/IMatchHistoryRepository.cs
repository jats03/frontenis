using Frontenis.Application.Models;
using Frontenis.Domain.Models;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Lectura del histórico de partidos jugados, para estadísticas de jugadores.</summary>
public interface IMatchHistoryRepository
{
    /// <summary>Partidos jugados (de competiciones y entrenamientos) que cumplen el filtro; con <paramref name="playerId"/> solo aquellos en los que participó ese jugador.</summary>
    Task<IReadOnlyList<PlayedMatch>> ListPlayedAsync(HistoryFilter filter, int? playerId, CancellationToken ct = default);
}
