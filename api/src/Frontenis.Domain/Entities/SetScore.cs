namespace Frontenis.Domain.Entities;

/// <summary>Tantos de un set (local, visitante).</summary>
public readonly record struct SetScore(int Home, int Away)
{
    /// <summary>Indica si el set lo ganó el equipo local.</summary>
    public bool HomeWon => Home > Away;

    /// <summary>
    /// Sets de un partido a partir de las columnas guardadas. Los partidos anteriores a los sets (sin columnas de set)
    /// se tratan como un único set con el marcador final.
    /// </summary>
    public static IReadOnlyList<SetScore> FromColumns(
        int? s1h, int? s1a, int? s2h, int? s2a, int? s3h, int? s3a, int? totalHome, int? totalAway)
    {
        var list = new List<SetScore>();
        if (s1h.HasValue && s1a.HasValue) list.Add(new SetScore(s1h.Value, s1a.Value));
        if (s2h.HasValue && s2a.HasValue) list.Add(new SetScore(s2h.Value, s2a.Value));
        if (s3h.HasValue && s3a.HasValue) list.Add(new SetScore(s3h.Value, s3a.Value));
        if (list.Count == 0 && totalHome.HasValue && totalAway.HasValue) list.Add(new SetScore(totalHome.Value, totalAway.Value));
        return list;
    }
}
