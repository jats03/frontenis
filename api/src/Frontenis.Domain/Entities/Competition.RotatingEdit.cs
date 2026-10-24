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
        if (_entries.Any(e => e.Pair.Contains(player.Id)) || _groups.Any(g => g.Members.Any(m => m.PlayerId == player.Id)))
            throw new DomainException("Ese jugador ya participa en la liga.");

        var group = _groups.OrderBy(g => g.Id).Last();
        if (ManualRounds)
        {
            // Jornadas a mano: basta con asignarlo al grupo; sus partidos los crea el administrador.
            group.AddMember(player);
            PairCount++;
            return Array.Empty<Pair>();
        }
        if (GroupMatches(group).Any(m => m.IsPlayed))
            throw new DomainException("Solo se puede añadir un jugador si en el último grupo no se ha jugado ningún partido.");

        var players = GroupPlayers(group).Append(player).ToList();
        var removed = RemoveGroupMatches(group, _ => true);

        // Todos contra todos: las jornadas necesarias para que cada jugador sea compañero de todos los demás del grupo,
        // aunque la liga se hubiera creado con menos jornadas fijas (con un jugador más ya no bastarían).
        AppendRounds(group, planner.Plan(players, RotationMath.FullRounds(players.Count)), 0);

        PairCount++;
        RefreshStatus();
        return removed;
    }

    /// <summary>
    /// Mueve un partido pendiente a otra jornada de su grupo (o a una nueva al final). Da igual que algún jugador del
    /// partido ya juegue en esa jornada. Si la jornada de origen se queda vacía, desaparece y las siguientes se renumeran.
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

        match.Reschedule(round, inGroup.Max(m => m.Order) + 1);
        CompactRounds(group);
        return match;
    }

    /// <summary>
    /// Añade un partido a una jornada de un grupo (o a una nueva, la siguiente a la última). Sin más condiciones:
    /// vale aunque la jornada ya tenga partidos jugados, aunque algún jugador ya juegue en ella o aunque la liga
    /// estuviera terminada (en ese caso vuelve a estar en juego). Los cuatro jugadores deben ser distintos y del grupo.
    /// </summary>
    public Match AddMatchToRound(int groupId, int round, int home1Id, int home2Id, int away1Id, int away2Id)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias admiten este cambio.");
        if (Status == CompetitionStatus.Draft) throw new DomainException("La liga aún no se ha sorteado.");
        var group = _groups.FirstOrDefault(g => g.Id == groupId) ?? throw new NotFoundException("Grupo no encontrado.");

        var ids = new[] { home1Id, home2Id, away1Id, away2Id };
        if (ids.Distinct().Count() != 4) throw new DomainException("Elige cuatro jugadores distintos.");
        var players = GroupPlayers(group).ToDictionary(p => p.Id);
        if (ids.Any(id => !players.ContainsKey(id))) throw new DomainException("Los cuatro jugadores deben ser de este grupo.");

        var inGroup = GroupMatches(group);
        var lastRound = inGroup.Select(m => m.Round).DefaultIfEmpty(0).Max();
        if (round < 1 || round > lastRound + 1) throw new DomainException($"La jornada debe estar entre 1 y {lastRound + 1}.");

        var home = new CompetitionEntry(new Pair(players[home1Id], players[home2Id]));
        var away = new CompetitionEntry(new Pair(players[away1Id], players[away2Id]));
        home.AssignToGroup(group);
        away.AssignToGroup(group);
        _entries.Add(home);
        _entries.Add(away);
        var match = Match.ForGroup(group, round, inGroup.Select(m => m.Order).DefaultIfEmpty(0).Max() + 1, home, away);
        _matches.Add(match);
        RefreshStatus();
        return match;
    }

    /// <summary>
    /// Borra un partido de una jornada, tenga o no resultado. Si la jornada se queda vacía desaparece y las siguientes
    /// se renumeran. El grupo tiene que conservar al menos un partido. Devuelve las parejas que han quedado sin partido.
    /// </summary>
    public IReadOnlyList<Pair> RemoveMatch(int matchId)
    {
        if (!IsRotating) throw new DomainException("Solo las ligas rotatorias admiten este cambio.");
        if (Status == CompetitionStatus.Draft) throw new DomainException("La liga aún no se ha sorteado.");
        var match = FindMatch(matchId);
        if (match.Phase != MatchPhase.Group) throw new DomainException("Solo se borran partidos de la fase de grupos.");

        var group = _groups.First(g => g == match.Group || g.Id == match.GroupId);
        if (GroupMatches(group).Count == 1) throw new DomainException("El grupo tiene que conservar al menos un partido.");

        var removed = RemoveGroupMatches(group, m => m == match);
        CompactRounds(group);
        RefreshStatus();
        return removed;
    }

    /// <summary>
    /// Intercambia las jornadas de dos partidos pendientes del mismo grupo. Un jugador puede acabar jugando
    /// varias veces en la misma jornada: no se impide.
    /// </summary>
    public void SwapMatchRounds(int matchId, int otherMatchId)
    {
        EnsureRotatingInProgress();
        if (matchId == otherMatchId) throw new DomainException("Elige dos partidos distintos.");
        var a = FindMatch(matchId);
        var b = FindMatch(otherMatchId);
        if (a.Phase != MatchPhase.Group || b.Phase != MatchPhase.Group)
            throw new DomainException("Solo se intercambian partidos de la fase de grupos.");
        if (a.IsPlayed || b.IsPlayed) throw new DomainException("No se puede intercambiar un partido que ya tiene resultado.");

        var group = _groups.First(g => g == a.Group || g.Id == a.GroupId);
        var inGroup = GroupMatches(group);
        if (!inGroup.Contains(b)) throw new DomainException("Los dos partidos tienen que ser del mismo grupo.");
        if (a.Round == b.Round) throw new DomainException("Los dos partidos ya están en la misma jornada.");

        (var roundA, var orderA) = (a.Round, a.Order);
        a.Reschedule(b.Round, b.Order);
        b.Reschedule(roundA, orderA);
    }

    /// <summary>
    /// Intercambia dos jornadas enteras de un grupo: los partidos de una pasan a la otra y viceversa.
    /// Da igual que haya partidos jugados: el orden de las jornadas no afecta a los resultados.
    /// </summary>
    public void SwapRounds(int groupId, int roundA, int roundB)
    {
        EnsureRotatingInProgress();
        var group = _groups.FirstOrDefault(g => g.Id == groupId) ?? throw new NotFoundException("Grupo no encontrado.");
        if (roundA == roundB) throw new DomainException("Elige dos jornadas distintas.");
        var inGroup = GroupMatches(group);
        var a = inGroup.Where(m => m.Round == roundA).ToList();
        var b = inGroup.Where(m => m.Round == roundB).ToList();
        if (a.Count == 0 || b.Count == 0) throw new NotFoundException("Esa jornada no existe.");

        foreach (var m in a) m.Reschedule(roundB, m.Order);
        foreach (var m in b) m.Reschedule(roundA, m.Order);
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
