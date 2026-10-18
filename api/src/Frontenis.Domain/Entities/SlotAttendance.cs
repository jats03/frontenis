namespace Frontenis.Domain.Entities;

/// <summary>Jugador apuntado a una franja de entrenamiento.</summary>
public sealed class SlotAttendance
{
    private SlotAttendance() { }

    internal SlotAttendance(int playerId, DateTime registeredAtUtc, int? registeredByPlayerId)
    {
        PlayerId = playerId;
        RegisteredAtUtc = registeredAtUtc;
        RegisteredByPlayerId = registeredByPlayerId;
    }

    public int Id { get; private set; }
    public int SlotId { get; private set; }
    public int PlayerId { get; private set; }
    public DateTime RegisteredAtUtc { get; private set; }
    /// <summary>Jugador titular desde cuyo dispositivo se apuntó, o <c>null</c> si lo hizo el administrador.</summary>
    public int? RegisteredByPlayerId { get; private set; }
}
