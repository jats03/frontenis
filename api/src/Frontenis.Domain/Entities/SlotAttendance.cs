namespace Frontenis.Domain.Entities;

/// <summary>Jugador apuntado a una franja de entrenamiento.</summary>
public sealed class SlotAttendance
{
    private SlotAttendance() { }

    internal SlotAttendance(int playerId, DateTime registeredAtUtc)
    {
        PlayerId = playerId;
        RegisteredAtUtc = registeredAtUtc;
    }

    public int Id { get; private set; }
    public int SlotId { get; private set; }
    public int PlayerId { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }
}
