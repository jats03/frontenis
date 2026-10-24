namespace Frontenis.Domain.Entities;

/// <summary>Grupo de una competición, al que se asignan parejas y partidos.</summary>
public sealed class Group
{
    private Group() { Name = string.Empty; }

    public Group(string name) { Name = name; }

    public int Id { get; private set; }
    public int CompetitionId { get; private set; }
    public string Name { get; private set; }

    private readonly List<GroupMember> _members = new();

    /// <summary>Jugadores asignados al grupo (solo en ligas con jornadas manuales; en las demás salen de los partidos).</summary>
    public IReadOnlyCollection<GroupMember> Members => _members;

    /// <summary>Quita a un jugador de la lista del grupo (si estaba).</summary>
    public void RemoveMember(int playerId)
    {
        var member = _members.FirstOrDefault(m => m.PlayerId == playerId);
        if (member is not null) _members.Remove(member);
    }

    /// <summary>Asigna un jugador al grupo si no estaba ya.</summary>
    public void AddMember(Player player)
    {
        if (_members.All(m => m.PlayerId != player.Id)) _members.Add(new GroupMember(player));
    }
}
