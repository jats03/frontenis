using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Services;

/// <summary>Cuadro de eliminación directa con cabezas de serie y byes para el tamaño potencia de 2.</summary>
public sealed class SingleEliminationBracketGenerator : IBracketGenerator
{
    public BracketPlan Generate(int participantCount)
    {
        if (participantCount < 2) throw new DomainException("La fase final necesita al menos 2 parejas.");

        var size = 2;
        while (size < participantCount) size *= 2;
        var totalRounds = (int)Math.Log2(size);
        var order = SeedOrder(size);

        var nodes = new List<BracketNode>();
        var placements = new List<BracketPlacement>();
        var byeNodes = new HashSet<int>(); // posiciones de ronda 1 que son bye

        // Ronda 1
        for (var p = 0; p < size / 2; p++)
        {
            var seedA = order[2 * p];
            var seedB = order[2 * p + 1];
            var aReal = seedA <= participantCount;
            var bReal = seedB <= participantCount;
            var slot = p % 2 == 0 ? BracketSlot.Home : BracketSlot.Away;

            if (aReal && bReal)
            {
                nodes.Add(NodeFor(1, p, totalRounds));
                placements.Add(new BracketPlacement(1, p, BracketSlot.Home, seedA));
                placements.Add(new BracketPlacement(1, p, BracketSlot.Away, seedB));
            }
            else
            {
                byeNodes.Add(p);
                var seed = aReal ? seedA : seedB; // el que pasa directo
                placements.Add(new BracketPlacement(2, p / 2, slot, seed));
            }
        }

        // Rondas siguientes
        for (var r = 2; r <= totalRounds; r++)
            for (var p = 0; p < size / (1 << r); p++)
                nodes.Add(NodeFor(r, p, totalRounds));

        return new BracketPlan(nodes, placements);
    }

    private static BracketNode NodeFor(int round, int position, int totalRounds) =>
        round == totalRounds
            ? new BracketNode(round, position, null, null, null)
            : new BracketNode(round, position, round + 1, position / 2,
                position % 2 == 0 ? BracketSlot.Home : BracketSlot.Away);

    /// <summary>Orden estándar de cabezas de serie: 1 vs último, 2 vs penúltimo, sin cruzar favoritos pronto.</summary>
    private static List<int> SeedOrder(int size)
    {
        var current = new List<int> { 1 };
        while (current.Count < size)
        {
            var total = current.Count * 2 + 1;
            current = current.SelectMany(s => new[] { s, total - s }).ToList();
        }
        return current;
    }
}
