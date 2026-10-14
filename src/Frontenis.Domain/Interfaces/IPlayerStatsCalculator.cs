using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Calcula estadísticas de jugadores a partir de partidos jugados.</summary>
public interface IPlayerStatsCalculator
{
    /// <summary>Estadísticas de todos los jugadores que aparecen en los partidos.</summary>
    IReadOnlyList<PlayerStats> CalculateAll(IReadOnlyList<PlayedMatch> matches);
    /// <summary>Estadísticas (victorias, derrotas, puntos, compañeros…) de un jugador concreto.</summary>
    PlayerStats Calculate(int playerId, IReadOnlyList<PlayedMatch> matches);
}
