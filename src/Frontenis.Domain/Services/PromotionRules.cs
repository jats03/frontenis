using Frontenis.Domain.Models;

namespace Frontenis.Domain.Services;

/// <summary>
/// Ascensos y descensos: el primero de cada grupo sube al grupo superior y el último baja al inferior.
/// El grupo de más nivel no asciende y el de menos nivel no desciende, así los grupos mantienen su tamaño.
/// </summary>
public static class PromotionRules
{
    /// <param name="groupsInOrder">Clasificación de cada grupo, del de más nivel (A) al de menos.</param>
    /// <param name="count">Cuántos suben y cuántos bajan por frontera entre grupos.</param>
    public static IReadOnlyList<PlayerMove> Compute(IReadOnlyList<IReadOnlyList<StandingRow>> groupsInOrder, int count = 1)
    {
        var moves = new List<PlayerMove>();
        for (var g = 0; g < groupsInOrder.Count; g++)
        {
            var rows = groupsInOrder[g];
            var n = Math.Min(count, rows.Count / 2); // nunca mover a todo un grupo
            if (g > 0)
                foreach (var row in rows.Take(n)) moves.Add(new PlayerMove(row.EntryId, g, g - 1));
            if (g < groupsInOrder.Count - 1)
                foreach (var row in rows.Skip(rows.Count - n)) moves.Add(new PlayerMove(row.EntryId, g, g + 1));
        }
        return moves;
    }

    /// <summary>Composición de los grupos de la siguiente liga tras aplicar los movimientos.</summary>
    public static IReadOnlyList<IReadOnlyList<int>> NextRosters(
        IReadOnlyList<IReadOnlyList<int>> currentRosters, IReadOnlyList<PlayerMove> moves)
    {
        var leaving = moves.Select(m => m.PlayerId).ToHashSet();
        var next = new List<IReadOnlyList<int>>();
        for (var g = 0; g < currentRosters.Count; g++)
        {
            var stay = currentRosters[g].Where(id => !leaving.Contains(id));
            var arrive = moves.Where(m => m.ToGroupIndex == g).Select(m => m.PlayerId);
            next.Add(stay.Concat(arrive).ToList());
        }
        return next;
    }
}
