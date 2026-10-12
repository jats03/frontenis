using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Services;

namespace Frontenis.Domain.Entities;

/// <summary>Parámetros configurables de una competición.</summary>
public sealed record CompetitionSettings(
    string Name,
    CompetitionType Type,
    int PairCount,
    int GroupCount,
    int PairsPerGroup,          // 0 = calcular automáticamente
    int PointsPerWin,
    int PointsPerLoss,
    bool DoubleRound,
    int QualifiersPerGroup);    // solo torneos (fase final)

/// <summary>Aggregate root: liga o torneo con sus grupos, parejas inscritas y partidos.</summary>
public sealed class Competition
{
    private readonly List<Group> _groups = new();
    private readonly List<CompetitionEntry> _entries = new();
    private readonly List<Match> _matches = new();

    private Competition() { Name = string.Empty; }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public CompetitionType Type { get; private set; }
    public CompetitionStatus Status { get; private set; }
    public int PairCount { get; private set; }
    public int GroupCount { get; private set; }
    public int PairsPerGroup { get; private set; }
    public int PointsPerWin { get; private set; }
    public int PointsPerLoss { get; private set; }
    public bool DoubleRound { get; private set; }
    public int QualifiersPerGroup { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Group> Groups => _groups;
    public IReadOnlyCollection<CompetitionEntry> Entries => _entries;
    public IReadOnlyCollection<Match> Matches => _matches;

    public static Competition Create(CompetitionSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.Name)) throw new DomainException("El nombre es obligatorio.");
        if (s.PairCount < 2) throw new DomainException("Se necesitan al menos 2 parejas.");
        if (s.GroupCount < 1) throw new DomainException("Debe haber al menos 1 grupo.");
        if (s.PairCount / s.GroupCount < 2)
            throw new DomainException("Cada grupo necesita al menos 2 parejas.");
        if (s.PairsPerGroup < 0) throw new DomainException("Parejas por grupo no válido.");
        if (s.PairsPerGroup > 0 && s.PairsPerGroup * s.GroupCount != s.PairCount)
            throw new DomainException("Grupos × parejas por grupo debe ser igual al número de parejas.");
        if (s.PointsPerWin < 1) throw new DomainException("Una victoria debe valer al menos 1 punto.");
        if (s.PointsPerLoss < 0 || s.PointsPerLoss >= s.PointsPerWin)
            throw new DomainException("Los puntos por derrota deben ser >= 0 y menores que los de victoria.");

        var perGroup = s.PairsPerGroup > 0 ? s.PairsPerGroup : (int)Math.Ceiling(s.PairCount / (double)s.GroupCount);
        var minGroupSize = s.PairCount / s.GroupCount;
        var qualifiers = 0;
        if (s.Type == CompetitionType.Tournament)
        {
            qualifiers = s.QualifiersPerGroup;
            if (qualifiers < 1 || qualifiers > minGroupSize)
                throw new DomainException($"Los clasificados por grupo deben estar entre 1 y {minGroupSize}.");
            if (qualifiers * s.GroupCount < 2)
                throw new DomainException("La fase final necesita al menos 2 clasificados en total.");
        }

        return new Competition
        {
            Name = s.Name.Trim(),
            Type = s.Type,
            Status = CompetitionStatus.Draft,
            PairCount = s.PairCount,
            GroupCount = s.GroupCount,
            PairsPerGroup = perGroup,
            PointsPerWin = s.PointsPerWin,
            PointsPerLoss = s.PointsPerLoss,
            DoubleRound = s.DoubleRound,
            QualifiersPerGroup = qualifiers,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public CompetitionEntry AddPair(Pair pair)
    {
        if (Status != CompetitionStatus.Draft) throw new DomainException("Ya no se pueden inscribir parejas.");
        if (_entries.Count >= PairCount) throw new DomainException("La competición ya tiene todas sus parejas.");
        var clash = _entries.Any(e => e.Pair.Contains(pair.Player1Id) || e.Pair.Contains(pair.Player2Id));
        if (clash && pair.Player1Id != 0) throw new DomainException("Algún jugador ya está inscrito en esta competición.");
        var entry = new CompetitionEntry(pair);
        _entries.Add(entry);
        return entry;
    }

    /// <summary>Sortea los grupos y genera el calendario de la fase de grupos.</summary>
    public void Draw(IGroupDistributor distributor, IScheduleGenerator scheduler)
    {
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        if (_entries.Count != PairCount)
            throw new DomainException($"Faltan parejas por inscribir ({_entries.Count}/{PairCount}).");

        var buckets = distributor.Distribute(_entries, GroupCount);
        for (var g = 0; g < buckets.Count; g++)
        {
            var group = new Group(GroupName(g));
            _groups.Add(group);
            foreach (var entry in buckets[g]) entry.AssignToGroup(group);

            var order = 0;
            foreach (var m in scheduler.Generate(buckets[g], DoubleRound))
                _matches.Add(Match.ForGroup(group, m.Round, ++order, m.Home, m.Away));
        }
        Status = CompetitionStatus.GroupStage;
    }

    public bool GroupStageCompleted =>
        _matches.Where(m => m.Phase == MatchPhase.Group).All(m => m.IsPlayed) &&
        _matches.Any(m => m.Phase == MatchPhase.Group);

    public bool HasPlayoffs => _matches.Any(m => m.Phase == MatchPhase.Playoff);

    /// <summary>Crea el cuadro de eliminatorias con los clasificados de cada grupo.</summary>
    public void GeneratePlayoffs(IStandingsCalculator calculator, IBracketGenerator bracket)
    {
        if (Type != CompetitionType.Tournament) throw new DomainException("Solo los torneos tienen fase final.");
        if (Status != CompetitionStatus.GroupStage || HasPlayoffs)
            throw new DomainException("No se puede generar la fase final en este estado.");
        if (!GroupStageCompleted) throw new DomainException("Aún hay partidos de grupo sin jugar.");

        var qualified = new List<(CompetitionEntry Entry, StandingRow Row)>();
        foreach (var group in _groups)
        {
            var groupEntries = _entries.Where(e => e.GroupId == group.Id || e.Group == group).ToList();
            var rows = calculator.Calculate(
                groupEntries.Select(e => e.Id).ToList(), GroupOutcomes(group), PointsPerWin, PointsPerLoss);
            foreach (var row in rows.Take(QualifiersPerGroup))
                qualified.Add((groupEntries.First(e => e.Id == row.EntryId), row));
        }

        // Cabezas de serie: primero los 1.º de grupo, luego los 2.º..., dentro de cada rango por rendimiento.
        var seeded = qualified
            .OrderBy(q => q.Row.Rank)
            .ThenByDescending(q => q.Row.ClassificationPoints)
            .ThenByDescending(q => q.Row.PointsDiff)
            .ThenByDescending(q => q.Row.PointsFor)
            .Select(q => q.Entry)
            .ToList();

        var plan = bracket.Generate(seeded.Count);
        var created = new Dictionary<(int Round, int Position), Match>();
        foreach (var node in plan.Nodes)
        {
            var match = Match.ForPlayoff(node.Round, node.Position + 1);
            created[(node.Round, node.Position)] = match;
            _matches.Add(match);
        }
        foreach (var node in plan.Nodes)
        {
            if (node.NextRound is null) continue;
            created[(node.Round, node.Position)].LinkTo(created[(node.NextRound.Value, node.NextPosition!.Value)], node.NextSlot!.Value);
        }
        foreach (var placement in plan.Placements)
            created[(placement.Round, placement.Position)].Place(placement.Slot, seeded[placement.Seed - 1]);

        Status = CompetitionStatus.Playoffs;
    }

    public Match RecordResult(int matchId, int homeScore, int awayScore)
    {
        var match = FindMatch(matchId);
        EnsureEditable(match);
        match.SetResult(homeScore, awayScore);
        Advance(match);
        RefreshStatus();
        return match;
    }

    public Match ClearResult(int matchId)
    {
        var match = FindMatch(matchId);
        EnsureEditable(match);
        match.ClearResult();
        if (match.NextMatch is not null) match.NextMatch.Place(match.NextSlot!.Value, null);
        RefreshStatus();
        return match;
    }

    public IReadOnlyList<MatchOutcome> GroupOutcomes(Group group) =>
        _matches
            .Where(m => m.Phase == MatchPhase.Group && m.IsPlayed && (m.Group == group || m.GroupId == group.Id))
            .Select(m => new MatchOutcome(m.HomeEntry!.Id, m.AwayEntry!.Id, m.HomeScore!.Value, m.AwayScore!.Value))
            .ToList();

    private Match FindMatch(int matchId) =>
        _matches.FirstOrDefault(m => m.Id == matchId) ?? throw new NotFoundException("Partido no encontrado.");

    private void EnsureEditable(Match match)
    {
        if (Status == CompetitionStatus.Draft) throw new DomainException("El torneo aún no ha comenzado.");
        if (match.Phase == MatchPhase.Group && HasPlayoffs)
            throw new DomainException("La fase de grupos está cerrada: ya existe la fase final.");
        if (match.NextMatch is { IsPlayed: true })
            throw new DomainException("No se puede modificar: la siguiente ronda ya tiene resultado.");
    }

    private static void Advance(Match match)
    {
        if (match.NextMatch is null) return;
        match.NextMatch.Place(match.NextSlot!.Value, match.Winner);
    }

    private void RefreshStatus()
    {
        if (Status == CompetitionStatus.Draft) return;
        if (Type == CompetitionType.League)
        {
            Status = _matches.All(m => m.IsPlayed) ? CompetitionStatus.Finished : CompetitionStatus.GroupStage;
            return;
        }
        if (!HasPlayoffs) { Status = CompetitionStatus.GroupStage; return; }
        var final = _matches.Where(m => m.Phase == MatchPhase.Playoff).OrderByDescending(m => m.Round).First();
        Status = final.IsPlayed ? CompetitionStatus.Finished : CompetitionStatus.Playoffs;
    }

    private static string GroupName(int index) =>
        index < 26 ? $"Grupo {(char)('A' + index)}" : $"Grupo {index + 1}";
}
