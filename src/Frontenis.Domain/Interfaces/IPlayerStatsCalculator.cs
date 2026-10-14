using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

public interface IPlayerStatsCalculator
{
    /// <summary>Estadísticas de todos los jugadores que aparecen en los partidos.</summary>
    IReadOnlyList<PlayerStats> CalculateAll(IReadOnlyList<PlayedMatch> matches);
    PlayerStats Calculate(int playerId, IReadOnlyList<PlayedMatch> matches);
}
