using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Domain.Entities;

/// <summary>Sorteo de grupos y generación del calendario de la fase de grupos.</summary>
public sealed partial class Competition
{
    /// <summary>Sortea los grupos y genera el calendario de la fase de grupos.</summary>
    public void Draw(IGroupDistributor distributor, IScheduleGenerator scheduler)
    {
        if (IsRotating) throw new DomainException("Una liga rotatoria se sortea con jugadores sueltos.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        if (_entries.Count != PairCount)
            throw new DomainException($"Faltan parejas por inscribir ({_entries.Count}/{PairCount}).");

        ApplyGroups(distributor.Distribute(_entries, GroupCount), scheduler);
    }

    /// <summary>Forma los grupos a mano (ids de inscripción por grupo) y genera el calendario.</summary>
    public void DrawManual(IReadOnlyList<IReadOnlyList<int>> groupEntryIds, IScheduleGenerator scheduler)
    {
        if (IsRotating) throw new DomainException("Una liga rotatoria se forma con jugadores sueltos.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("El sorteo ya se realizó.");
        if (_entries.Count != PairCount)
            throw new DomainException($"Faltan parejas por inscribir ({_entries.Count}/{PairCount}).");
        if (groupEntryIds.Count != GroupCount)
            throw new DomainException($"Debe haber exactamente {GroupCount} grupo(s).");

        var all = groupEntryIds.SelectMany(g => g).ToList();
        if (all.Count != _entries.Count || all.Distinct().Count() != all.Count || all.Any(id => _entries.All(e => e.Id != id)))
            throw new DomainException("Cada pareja debe estar en un grupo, y solo en uno.");
        if (groupEntryIds.Any(g => g.Count < 2)) throw new DomainException("Cada grupo necesita al menos 2 parejas.");
        if (Type == CompetitionType.Tournament && groupEntryIds.Any(g => g.Count < QualifiersPerGroup))
            throw new DomainException($"Cada grupo necesita al menos {QualifiersPerGroup} parejas (clasificados por grupo).");

        var buckets = groupEntryIds
            .Select(g => (IReadOnlyList<CompetitionEntry>)g.Select(id => _entries.First(e => e.Id == id)).ToList())
            .ToList();
        ApplyGroups(buckets, scheduler);
    }

    private void ApplyGroups(IReadOnlyList<IReadOnlyList<CompetitionEntry>> buckets, IScheduleGenerator scheduler)
    {
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
}
