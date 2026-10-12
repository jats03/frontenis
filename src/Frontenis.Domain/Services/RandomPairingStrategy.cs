using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>
/// Empareja zagueros con delanteros al azar. Si sobran de un tipo, los sobrantes
/// se emparejan entre sí (caso extremo). Si el total es impar, un jugador queda sin pareja.
/// </summary>
public sealed class RandomPairingStrategy : IPairingStrategy
{
    private readonly IRandomizer _randomizer;

    public RandomPairingStrategy(IRandomizer randomizer) { _randomizer = randomizer; }

    public PairingResult Generate(IReadOnlyList<Player> players)
    {
        if (players.Count < 2) throw new DomainException("Se necesitan al menos 2 jugadores para emparejar.");

        var forwards = players.Where(p => p.Position == PlayerPosition.Delantero).ToList();
        var backs = players.Where(p => p.Position == PlayerPosition.Zaguero).ToList();
        _randomizer.Shuffle(forwards);
        _randomizer.Shuffle(backs);

        var pairs = new List<(Player, Player)>();
        var mixed = Math.Min(forwards.Count, backs.Count);
        for (var i = 0; i < mixed; i++) pairs.Add((backs[i], forwards[i]));

        var leftovers = forwards.Skip(mixed).Concat(backs.Skip(mixed)).ToList(); // todos del mismo tipo
        Player? unpaired = null;
        for (var i = 0; i + 1 < leftovers.Count; i += 2) pairs.Add((leftovers[i], leftovers[i + 1]));
        if (leftovers.Count % 2 == 1) unpaired = leftovers[^1];

        return new PairingResult(pairs, unpaired);
    }
}
