using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Interfaces;

/// <summary>Estrategia para formar parejas a partir de una lista de jugadores.</summary>
public interface IPairingStrategy
{
    /// <summary>Forma parejas con los jugadores dados (por ejemplo, un zaguero con un delantero) y devuelve las parejas y los jugadores sobrantes.</summary>
    PairingResult Generate(IReadOnlyList<Player> players);
}
