namespace Frontenis.Domain.Entities;

/// <summary>
/// Jugador asignado a un grupo de una liga rotatoria cuyas jornadas se crean a mano: mientras el grupo no tiene
/// partidos, esta es la única forma de saber quién lo forma.
/// </summary>
public sealed class GroupMember
{
    private GroupMember() { Player = null!; }

    public GroupMember(Player player) { Player = player; PlayerId = player.Id; }

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int PlayerId { get; private set; }
    public Player Player { get; private set; }
}
