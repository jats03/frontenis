using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>
/// Resultado de un partido de competición enviado por un jugador desde su dispositivo, pendiente de que el
/// administrador lo apruebe. Solo hay uno por partido, y solo quien lo envió puede corregirlo.
/// </summary>
public sealed class MatchSubmission
{
    private MatchSubmission() { }

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int PlayerId { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }

    public int Set1Home { get; private set; }
    public int Set1Away { get; private set; }
    public int? Set2Home { get; private set; }
    public int? Set2Away { get; private set; }
    public int? Set3Home { get; private set; }
    public int? Set3Away { get; private set; }

    /// <summary>Marcador de cada set enviado.</summary>
    public IReadOnlyList<SetScore> Sets
    {
        get
        {
            var list = new List<SetScore> { new(Set1Home, Set1Away) };
            if (Set2Home.HasValue && Set2Away.HasValue) list.Add(new SetScore(Set2Home.Value, Set2Away.Value));
            if (Set3Home.HasValue && Set3Away.HasValue) list.Add(new SetScore(Set3Home.Value, Set3Away.Value));
            return list;
        }
    }

    /// <summary>Crea el envío. Las reglas del marcador las valida antes la competición.</summary>
    public static MatchSubmission Create(int matchId, int playerId, IReadOnlyList<SetScore> sets)
    {
        if (matchId <= 0 || playerId <= 0) throw new DomainException("Envío no válido.");
        var s = new MatchSubmission { MatchId = matchId, PlayerId = playerId };
        s.Replace(playerId, sets);
        return s;
    }

    /// <summary>Sustituye el marcador enviado (solo lo puede hacer quien lo envió).</summary>
    /// <exception cref="DomainException">Otro jugador distinto del que lo envió, o marcador sin sets.</exception>
    public void Replace(int playerId, IReadOnlyList<SetScore> sets)
    {
        if (playerId != PlayerId) throw new DomainException("Solo quien envió el resultado puede modificarlo.");
        if (sets.Count is < 1 or > 3) throw new DomainException("El resultado debe tener entre 1 y 3 sets.");
        Set1Home = sets[0].Home; Set1Away = sets[0].Away;
        Set2Home = sets.Count > 1 ? sets[1].Home : null; Set2Away = sets.Count > 1 ? sets[1].Away : null;
        Set3Home = sets.Count > 2 ? sets[2].Home : null; Set3Away = sets.Count > 2 ? sets[2].Away : null;
        SubmittedAtUtc = DateTime.UtcNow;
    }
}
