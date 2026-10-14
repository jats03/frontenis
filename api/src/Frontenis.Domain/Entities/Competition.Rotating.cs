using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;

namespace Frontenis.Domain.Entities;

/// <summary>Liga rotatoria: grupos de jugadores, parejas cambiantes y encadenado de ligas.</summary>
public sealed partial class Competition
{
    /// <summary>Liga rotatoria con los grupos formados a mano (jugadores por grupo, de mayor a menor nivel).</summary>
    public void DrawRotatingManual(IReadOnlyList<IReadOnlyList<Player>> groups, IRotationPlanner planner)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias se forman con jugadores.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        if (groups.Count != GroupCount) throw new DomainException($"Debe haber exactamente {GroupCount} grupo(s).");
        var all = groups.SelectMany(g => g).ToList();
        if (all.Select(p => p.Id).Distinct().Count() != all.Count) throw new DomainException("Hay jugadores repetidos.");
        if (groups.Any(g => g.Count < 4)) throw new DomainException("Cada grupo necesita al menos 4 jugadores.");
        PairCount = all.Count;
        BuildRotating(groups, planner);
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
            foreach (var round in planner.Plan(groups[g], Rounds > 0 ? Rounds : RotationMath.FullRounds(groups[g].Count)))
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
                m.HomeScore!.Value, m.AwayScore!.Value, m.HomeSets, m.AwaySets))
            .ToList();

    /// <summary>Clasificación individual de cada grupo de una liga rotatoria (grupos por orden de creación).</summary>
    public IReadOnlyList<GroupStandings> RotatingStandings(IIndividualStandingsCalculator calculator) =>
        _groups.OrderBy(g => g.Id)
            .Select(g => new GroupStandings(g.Id, g.Name, calculator.Calculate(
                GroupPlayers(g).Select(p => p.Id).ToList(), GroupTeamOutcomes(g), PointsPerWin, PointsPerLoss)))
            .ToList();

    /// <summary>Enlaza esta liga terminada con la que la continúa.</summary>
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
            previous.PairsPerGroup, previous.PointsPerWin, previous.PointsPerLoss, false, 0, previous.Rounds, previous.SetsPerMatch));
        next.PairCount = rosters.Sum(r => r.Count);
        next.BuildRotating(rosters, planner);
        return next;
    }

    private static string NextName(string name)
    {
        var m = System.Text.RegularExpressions.Regex.Match(name, @"^(.*) \((\d+)\)$");
        return m.Success ? $"{m.Groups[1].Value} ({int.Parse(m.Groups[2].Value) + 1})" : $"{name} (2)";
    }
}
