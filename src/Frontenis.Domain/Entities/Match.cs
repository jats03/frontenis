using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Partido de una competición (fase de grupos o final). Guarda el resultado por sets y, en la fase final, el partido al que avanza el ganador.</summary>
public sealed class Match
{
    private Match() { }

    internal Match(MatchPhase phase, int round, int order)
    {
        Phase = phase;
        Round = round;
        Order = order;
    }

    public int Id { get; private set; }
    public int CompetitionId { get; private set; }
    public MatchPhase Phase { get; private set; }
    public int? GroupId { get; private set; }
    public Group? Group { get; private set; }
    public int Round { get; private set; }
    public int Order { get; private set; }

    public int? HomeEntryId { get; private set; }
    public CompetitionEntry? HomeEntry { get; private set; }
    public int? AwayEntryId { get; private set; }
    public CompetitionEntry? AwayEntry { get; private set; }

    /// <summary>Tantos totales (suma de todos los sets).</summary>
    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }

    // Marcador de cada set (hasta 3). Vacíos en partidos antiguos: se interpretan como un único set.
    public int? Set1Home { get; private set; }
    public int? Set1Away { get; private set; }
    public int? Set2Home { get; private set; }
    public int? Set2Away { get; private set; }
    public int? Set3Home { get; private set; }
    public int? Set3Away { get; private set; }

    public IReadOnlyList<SetScore> Sets =>
        SetScore.FromColumns(Set1Home, Set1Away, Set2Home, Set2Away, Set3Home, Set3Away, HomeScore, AwayScore);
    public int HomeSets => Sets.Count(s => s.HomeWon);
    public int AwaySets => Sets.Count(s => !s.HomeWon);

    public int? NextMatchId { get; private set; }
    public Match? NextMatch { get; private set; }
    public BracketSlot? NextSlot { get; private set; }

    /// <summary>Indica si el partido ya tiene resultado.</summary>
    public bool IsPlayed => HomeScore.HasValue && AwayScore.HasValue;
    /// <summary>Indica si ya están definidas las dos parejas (en la fase final pueden faltar hasta que avance la ronda anterior).</summary>
    public bool IsReady => HomeEntry is not null && AwayEntry is not null;

    /// <summary>Pareja ganadora (más sets ganados) o <c>null</c> si no se ha jugado.</summary>
    public CompetitionEntry? Winner =>
        !IsPlayed ? null : HomeSets > AwaySets ? HomeEntry : AwayEntry;

    internal static Match ForGroup(Group group, int round, int order, CompetitionEntry home, CompetitionEntry away) =>
        new(MatchPhase.Group, round, order) { Group = group, HomeEntry = home, AwayEntry = away };

    internal static Match ForPlayoff(int round, int order) => new(MatchPhase.Playoff, round, order);

    internal void LinkTo(Match next, BracketSlot slot)
    {
        NextMatch = next;
        NextSlot = slot;
    }

    internal void Place(BracketSlot slot, CompetitionEntry? entry)
    {
        if (slot == BracketSlot.Home) HomeEntry = entry; else AwayEntry = entry;
    }

    /// <summary>Registra el resultado por sets. Con 1 set por partido: un set; con 2: dos sets y, si van 1-1, un tercero.</summary>
    internal void SetResult(IReadOnlyList<SetScore> sets, int setsPerMatch)
    {
        if (!IsReady) throw new DomainException("El partido aún no tiene las dos parejas definidas.");
        ValidateSets(sets, setsPerMatch);
        Set1Home = sets[0].Home; Set1Away = sets[0].Away;
        Set2Home = sets.Count > 1 ? sets[1].Home : null; Set2Away = sets.Count > 1 ? sets[1].Away : null;
        Set3Home = sets.Count > 2 ? sets[2].Home : null; Set3Away = sets.Count > 2 ? sets[2].Away : null;
        HomeScore = sets.Sum(s => s.Home);
        AwayScore = sets.Sum(s => s.Away);
    }

    internal static void ValidateSets(IReadOnlyList<SetScore> sets, int setsPerMatch)
    {
        if (setsPerMatch == 1)
        {
            if (sets.Count != 1) throw new DomainException("Este partido se juega a un solo set.");
        }
        else
        {
            if (sets.Count < 2) throw new DomainException("Introduce el resultado de los dos sets.");
            if (sets.Count > 3) throw new DomainException("Como máximo hay 3 sets.");
        }
        foreach (var s in sets)
        {
            if (s.Home < 0 || s.Away < 0) throw new DomainException("Los puntos no pueden ser negativos.");
            if (s.Home > 999 || s.Away > 999) throw new DomainException("Marcador no válido.");
            if (s.Home == s.Away) throw new DomainException("En frontenis no hay empates: cada set debe tener un ganador.");
        }
        if (setsPerMatch == 2)
        {
            var oneOne = sets[0].HomeWon != sets[1].HomeWon;
            if (oneOne && sets.Count == 2) throw new DomainException("Van 1-1 en sets: falta el 3.er set de desempate.");
            if (!oneOne && sets.Count == 3) throw new DomainException("Un equipo ganó los dos primeros sets: no hay 3.er set.");
        }
    }

    internal void ClearResult()
    {
        HomeScore = null;
        AwayScore = null;
        Set1Home = Set1Away = Set2Home = Set2Away = Set3Home = Set3Away = null;
    }
}
