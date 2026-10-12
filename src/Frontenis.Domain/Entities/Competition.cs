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
    int QualifiersPerGroup,     // solo torneos (fase final)
    int Rounds = 0);            // solo ligas rotatorias (0 = automático)

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
    public int Rounds { get; private set; }
    public int? NextCompetitionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Group> Groups => _groups;
    public IReadOnlyCollection<CompetitionEntry> Entries => _entries;
    public IReadOnlyCollection<Match> Matches => _matches;

    public static Competition Create(CompetitionSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.Name)) throw new DomainException("El nombre es obligatorio.");
        if (s.Type == CompetitionType.RotatingLeague) return CreateRotating(s);
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

    private static Competition CreateRotating(CompetitionSettings s)
    {
        if (s.GroupCount < 1) throw new DomainException("Debe haber al menos 1 grupo.");
        var perGroup = s.PairsPerGroup; // mínimo de jugadores por grupo
        if (perGroup < 4)
            throw new DomainException("Cada grupo necesita al menos 4 jugadores.");
        if (s.PointsPerWin < 1) throw new DomainException("Una victoria debe valer al menos 1 punto.");
        if (s.PointsPerLoss < 0 || s.PointsPerLoss >= s.PointsPerWin)
            throw new DomainException("Los puntos por derrota deben ser >= 0 y menores que los de victoria.");
        var rounds = s.Rounds > 0 ? s.Rounds : perGroup / 2;
        if (rounds > perGroup - 1)
            throw new DomainException($"Con {perGroup} jugadores por grupo el máximo es {perGroup - 1} jornadas.");

        return new Competition
        {
            Name = s.Name.Trim(),
            Type = CompetitionType.RotatingLeague,
            Status = CompetitionStatus.Draft,
            PairCount = s.GroupCount * perGroup,
            GroupCount = s.GroupCount,
            PairsPerGroup = perGroup,
            PointsPerWin = s.PointsPerWin,
            PointsPerLoss = s.PointsPerLoss,
            DoubleRound = false,
            QualifiersPerGroup = 0,
            Rounds = rounds,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public bool IsRotating => Type == CompetitionType.RotatingLeague;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        var trimmed = name.Trim();
        if (trimmed.Length > 150) throw new DomainException("El nombre no puede superar los 150 caracteres.");
        Name = trimmed;
    }

    public CompetitionEntry AddPair(Pair pair)
    {
        if (IsRotating) throw new DomainException("En una liga rotatoria se inscriben jugadores sueltos, no parejas.");
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
        if (IsRotating) throw new DomainException("Una liga rotatoria se sortea con jugadores sueltos.");
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

    /// <summary>Reparte los jugadores al azar en grupos equilibrados por puesto y genera las jornadas.</summary>
    public void DrawRotating(IReadOnlyList<Player> players, IRandomizer randomizer, IRotationPlanner planner)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias se sortean con jugadores.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        var minimum = GroupCount * PairsPerGroup;
        if (players.Count < minimum)
            throw new DomainException($"Se necesitan al menos {minimum} jugadores ({players.Count} seleccionados).");
        if (players.Select(p => p.Id).Distinct().Count() != players.Count)
            throw new DomainException("Hay jugadores repetidos.");

        var fw = players.Where(p => p.Position == PlayerPosition.Delantero).ToList();
        randomizer.Shuffle(fw);
        var bk = players.Where(p => p.Position != PlayerPosition.Delantero).ToList();
        randomizer.Shuffle(bk);
        // Cada grupo recibe PairsPerGroup jugadores y los sobrantes van al último grupo (el de menos nivel).
        // Se reparte alternando delanteros y zagueros para que cada grupo quede equilibrado.
        var buckets = Enumerable.Range(0, GroupCount).Select(_ => new List<Player>()).ToList();
        var ordered = new List<Player>();
        for (var k = 0; k < Math.Max(fw.Count, bk.Count); k++)
        {
            if (k < fw.Count) ordered.Add(fw[k]);
            if (k < bk.Count) ordered.Add(bk[k]);
        }
        for (var g = 0; g < GroupCount; g++)
        {
            var take = g == GroupCount - 1 ? ordered.Count : PairsPerGroup;
            buckets[g].AddRange(ordered.Take(take));
            ordered.RemoveRange(0, Math.Min(take, ordered.Count));
        }
        PairCount = players.Count;
        BuildRotating(buckets, planner);
    }

    /// <summary>Crea grupos con los jugadores dados (en orden de nivel) y genera sus jornadas.</summary>
    public void BuildRotating(IReadOnlyList<IReadOnlyList<Player>> groups, IRotationPlanner planner)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias se construyen así.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        if (groups.Count != GroupCount) throw new DomainException("Número de grupos incorrecto.");

        for (var g = 0; g < groups.Count; g++)
        {
            var group = new Group(GroupName(g));
            _groups.Add(group);
            var order = 0;
            foreach (var round in planner.Plan(groups[g], Rounds))
            {
                foreach (var m in round.Matches)
                {
                    var home = new CompetitionEntry(new Pair(m.Home.First, m.Home.Second));
                    var away = new CompetitionEntry(new Pair(m.Away.First, m.Away.Second));
                    home.AssignToGroup(group);
                    away.AssignToGroup(group);
                    _entries.Add(home);
                    _entries.Add(away);
                    _matches.Add(Match.ForGroup(group, round.Number, ++order, home, away));
                }
            }
        }
        Status = CompetitionStatus.GroupStage;
    }

    /// <summary>Jugadores distintos que participan en un grupo de una liga rotatoria.</summary>
    public IReadOnlyList<Player> GroupPlayers(Group group) =>
        _entries
            .Where(e => e.Group == group || (e.GroupId.HasValue && e.GroupId == group.Id))
            .SelectMany(e => new[] { e.Pair.Player1, e.Pair.Player2 })
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

    /// <summary>Resultados por equipos (ids de jugadores) de un grupo de una liga rotatoria.</summary>
    public IReadOnlyList<TeamOutcome> GroupTeamOutcomes(Group group) =>
        _matches
            .Where(m => m.Phase == MatchPhase.Group && m.IsPlayed && (m.Group == group || m.GroupId == group.Id))
            .Select(m => new TeamOutcome(
                new[] { m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id },
                new[] { m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id },
                m.HomeScore!.Value, m.AwayScore!.Value))
            .ToList();

    public void SetNextCompetition(int id)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias tienen continuación.");
        if (Status != CompetitionStatus.Finished) throw new DomainException("La liga aún no ha terminado.");
        if (NextCompetitionId.HasValue) throw new DomainException("La siguiente liga ya fue creada.");
        NextCompetitionId = id;
    }

    /// <summary>Crea la siguiente liga con los grupos ya regrupados (tras ascensos y descensos).</summary>
    public static Competition CreateNext(Competition previous, IReadOnlyList<IReadOnlyList<Player>> rosters, IRotationPlanner planner)
    {
        if (!previous.IsRotating) throw new DomainException("Solo las ligas rotatorias tienen continuación.");
        if (previous.Status != CompetitionStatus.Finished) throw new DomainException("La liga aún no ha terminado.");
        if (previous.NextCompetitionId.HasValue) throw new DomainException("La siguiente liga ya fue creada.");
        var next = Create(new CompetitionSettings(
            NextName(previous.Name), CompetitionType.RotatingLeague, previous.PairCount, previous.GroupCount,
            previous.PairsPerGroup, previous.PointsPerWin, previous.PointsPerLoss, false, 0, previous.Rounds));
        next.PairCount = rosters.Sum(r => r.Count);
        next.BuildRotating(rosters, planner);
        return next;
    }

    private static string NextName(string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(name, @"^(.*) \((\d+)\)$");
        return m.Success ? $"{m.Groups[1].Value} ({int.Parse(m.Groups[2].Value) + 1})" : $"{name} (2)";
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
        if (Type != CompetitionType.Tournament)
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
