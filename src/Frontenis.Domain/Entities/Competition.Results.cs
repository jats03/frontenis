using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Entities;

/// <summary>Registro, borrado y consulta de resultados de partidos.</summary>
public sealed partial class Competition
{
    /// <summary>Registra un resultado simple (un solo set).</summary>
    public Match RecordResult(int matchId, int homeScore, int awayScore) =>
        RecordResult(matchId, new[] { new SetScore(homeScore, awayScore) });

    /// <summary>Registra el resultado por sets, avanza al ganador en el cuadro y recalcula el estado de la competición.</summary>
    /// <exception cref="DomainException">Marcador no válido o partido no editable.</exception>
    public Match RecordResult(int matchId, IReadOnlyList<SetScore> sets)
    {
        var match = FindMatch(matchId);
        EnsureEditable(match);
        match.SetResult(sets, SetsPerMatch);
        Advance(match);
        RefreshStatus();
        return match;
    }

    /// <summary>Borra el resultado de un partido; no se permite si la ronda siguiente ya tiene resultado.</summary>
    public Match ClearResult(int matchId)
    {
        var match = FindMatch(matchId);
        EnsureEditable(match);
        match.ClearResult();
        if (match.NextMatch is not null) match.NextMatch.Place(match.NextSlot!.Value, null);
        RefreshStatus();
        return match;
    }

    /// <summary>Resultados de los partidos jugados del grupo, listos para calcular la clasificación.</summary>
    public IReadOnlyList<MatchOutcome> GroupOutcomes(Group group) =>
        _matches
            .Where(m => m.Phase == MatchPhase.Group && m.IsPlayed && (m.Group == group || m.GroupId == group.Id))
            .Select(m => new MatchOutcome(m.HomeEntry!.Id, m.AwayEntry!.Id, m.HomeScore!.Value, m.AwayScore!.Value, m.HomeSets, m.AwaySets))
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
}
