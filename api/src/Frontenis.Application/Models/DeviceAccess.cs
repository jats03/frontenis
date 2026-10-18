namespace Frontenis.Application.Models;

/// <summary>A quién puede apuntar un dispositivo: a su jugador titular y a sus apadrinados.</summary>
public sealed record DeviceAccess(int OwnerPlayerId, IReadOnlyList<int> DelegateIds)
{
    /// <summary>El jugador puede apuntarse o quitarse desde este dispositivo.</summary>
    public bool Allows(int playerId) => playerId == OwnerPlayerId || DelegateIds.Contains(playerId);
}
