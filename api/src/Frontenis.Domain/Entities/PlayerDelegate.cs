using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Apadrinado: el jugador titular puede apuntar también a este otro jugador desde su dispositivo (p. ej. un hijo).</summary>
public sealed class PlayerDelegate
{
    private PlayerDelegate() { }

    public int Id { get; private set; }
    public int OwnerPlayerId { get; private set; }
    public int DelegatePlayerId { get; private set; }

    /// <exception cref="DomainException">Jugadores no válidos o el mismo jugador en los dos lados.</exception>
    public static PlayerDelegate Create(int ownerPlayerId, int delegatePlayerId)
    {
        if (ownerPlayerId <= 0 || delegatePlayerId <= 0) throw new DomainException("Jugador no válido.");
        if (ownerPlayerId == delegatePlayerId) throw new DomainException("Un jugador no puede apadrinarse a sí mismo.");
        return new PlayerDelegate { OwnerPlayerId = ownerPlayerId, DelegatePlayerId = delegatePlayerId };
    }
}
