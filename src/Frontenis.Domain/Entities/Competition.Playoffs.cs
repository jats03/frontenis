using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Entities;

/// <summary>Fase final (cuadro eliminatorio) de los torneos.</summary>
public sealed partial class Competition
{
    /// <summary>Indica si todos los partidos de la fase de grupos están jugados.</summary>
    public bool GroupStageCompleted =>
        _matches.Where(m => m.Phase == MatchPhase.Group).All(m => m.IsPlayed) &&
        _matches.Any(m => m.Phase == MatchPhase.Group);

    /// <summary>Indica si ya se generó la fase final.</summary>
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

    private static void Advance(Match match)
    {
        if (match.NextMatch is null) return;
        match.NextMatch.Place(match.NextSlot!.Value, match.Winner);
    }
}
