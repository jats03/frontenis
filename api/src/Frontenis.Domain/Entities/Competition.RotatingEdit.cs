using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;

namespace Frontenis.Domain.Entities;

/// <summary>Cambios en una liga rotatoria ya sorteada: añadir un jugador, mover partidos de jornada y quitar jornadas.</summary>
public sealed partial class Competition
{
    /// <summary>
    /// Añade un jugador al último grupo (el de menos nivel) y vuelve a planificar por completo las jornadas de ese grupo.
    /// Solo se permite si en ese grupo no se ha jugado ningún partido: así el reparto de compañeros y rivales sigue
    /// siendo equilibrado. Devuelve las parejas que han quedado sin partido, para que quien persiste las elimine.
    /// </summary>
    /// <exception cref="DomainException">Liga no sorteada o terminada, jugador que ya participa o grupo con partidos jugados.</exception>
    public IReadOnlyList<Pair> AddPlayerToLastGroup(Player player, IRotationPlanner planner)
    {
        EnsureRotatingInProgress();
        if (_entries.Any(e => e.Pair.Contains(player.Id))) throw new DomainException("Ese jugador ya participa en la liga.");

        var group = _groups.OrderBy(g => g.Id).Last();
        if (GroupMatches(group).Any(m => m.IsPlayed))
            throw new DomainException("Solo se puede añadir un jugador si en el último grupo no se ha jugado ningún partido.");

        var players = GroupPlayers(group).Append(player).ToList();
        var removed = RemoveGroupMatches(group, _ => true);

        // Jornadas totales: las fijadas en la liga o, si no, las necesarias para que todos jueguen con todos.
        var total = Rounds > 0 ? Rounds : RotationMath.FullRounds(players.Count);
        AppendRounds(group, planner.Plan(players, total), 0);

        PairCount++;
        RefreshStatus();
        return removed;
    }

    /// <summary>
    /// Mueve un partido pendiente a otra jornada de su grupo (o a una nueva al final). Ningún jugador del partido
    /// puede jugar ya en esa jornada. Si la jornada de origen se queda vacía, desaparece y las siguientes se renumeran.
    /// </summary>
    public Match MoveMatchToRound(int matchId, int round)
    {
        EnsureRotatingInProgress();
        var match = FindMatch(matchId);
        if (match.Phase != MatchPhase.Group || match.Group is null && match.GroupId is null)
            throw new DomainException("Solo se mueven partidos de la fase de grupos.");
        if (match.IsPlayed) throw new DomainException("No se puede mover un partido que ya tiene resultado.");
        if (round < 1) throw new DomainException("La jornada debe ser 1 o mayor.");

        var group = _groups.First(g => g == match.Group || g.Id == match.GroupId);
        var inGroup = GroupMatches(group);
        var lastRound = inGroup.Max(m => m.Round);
        if (round > lastRound + 1) throw new DomainException($"La jornada máxima a la que se puede mover es la {lastRound + 1}.");
        if (round == match.Round) return match;

        var busy = inGroup.Where(m => m != match && m.Round == round).SelectMany(PlayerIds).ToHashSet();
        if (PlayerIds(match).Any(busy.Contains))
            throw new DomainException($"Algún jugador de ese partido ya juega en la jornada {round}.");

        match.Reschedule(round, inGroup.Max(m => m.Order) + 1);
        CompactRounds(group);
        return match;
    }

    /// <summary>
    /// Quita una jornada de un grupo con todos sus partidos, siempre que ninguno tenga resultado; las siguientes
    /// jornadas se renumeran. Devuelve las parejas que han quedado sin partido.
    /// </summary>
    public IReadOnlyList<Pair> RemoveRound(int groupId, int round)
    {
        EnsureRotatingInProgress();
        var group = _groups.FirstOrDefault(g => g.Id == groupId) ?? throw new NotFoundException("Grupo no encontrado.");
        var inRound = GroupMatches(group).Where(m => m.Round == round).ToList();
        if (inRound.Count == 0) throw new NotFoundException("Esa jornada no existe.");
        if (inRound.Any(m => m.IsPlayed)) throw new DomainException($"No se puede quitar la jornada {round}: ya tiene partidos jugados.");
        if (GroupMatches(group).All(m => m.Round == round)) throw new DomainException("El grupo tiene que conservar al menos una jornada.");

        var removed = RemoveGroupMatches(group, m => m.Round == round);
        CompactRounds(group);
        RefreshStatus();
        return removed;
    }

    private void EnsureRotatingInProgress()
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias admiten este cambio.");
        if (Status == CompetitionStatus.Draft) throw new DomainException("La liga aún no se ha sorteado.");
        if (Status == CompetitionStatus.Finished) throw new DomainException("La liga ya ha terminado.");
    }

    private List<Match> GroupMatches(Group group) =>
        _matches.Where(m => m.Phase == MatchPhase.Group && (m.Group == group || (m.GroupId.HasValue && m.GroupId == group.Id))).ToList();

    private static IEnumerable<int> PlayerIds(Match m) => new[]
    {
        m.HomeEntry!.Pair.Player1Id, m.HomeEntry.Pair.Player2Id, m.AwayEntry!.Pair.Player1Id, m.AwayEntry.Pair.Player2Id,
    };

    /// <summary>Quita los partidos del grupo que cumplan la condición, con sus inscripciones; devuelve sus parejas.</summary>
    private List<Pair> RemoveGroupMatches(Group group, Func<Match, bool> where)
    {
        var doomed = GroupMatches(group).Where(where).ToList();
        var entries = doomed.SelectMany(m => new[] { m.HomeEntry!, m.AwayEntry! }).Distinct().ToList();
        foreach (var m in doomed) _matches.Remove(m);
        foreach (var e in entries) _entries.Remove(e);
        return entries.Select(e => e.Pair).Distinct().ToList();
    }

    /// <summary>Renumera las jornadas del grupo como 1, 2, 3… sin huecos, conservando su orden.</summary>
    private void CompactRounds(Group group)
    {
        var matches = GroupMatches(group);
        var number = 0;
        foreach (var round in matches.Select(m => m.Round).Distinct().OrderBy(r => r))
        {
            number++;
            foreach (var m in matches.Where(m => m.Round == round)) m.Reschedule(number, m.Order);
        }
    }
}
