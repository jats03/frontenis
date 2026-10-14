using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Calcula, para un grupo de jugadores, las parejas y enfrentamientos de cada jornada.</summary>
public interface IRotationPlanner
{
    IReadOnlyList<PlannedRound> Plan(IReadOnlyList<Player> groupPlayers, int rounds);
}
