using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>
/// Vínculo entre un dispositivo (móvil, tablet…) y su jugador titular, para apuntarse sin usuario ni contraseña.
/// Solo se guarda el hash del identificador que genera el dispositivo, nunca el identificador en claro.
/// </summary>
public sealed class DeviceLink
{
    private DeviceLink() { DeviceHash = string.Empty; }

    public int Id { get; private set; }
    public string DeviceHash { get; private set; }
    public int PlayerId { get; private set; }
    public DateTime LinkedAtUtc { get; private set; }

    /// <summary>Vincula un dispositivo (por su hash) con un jugador.</summary>
    /// <exception cref="DomainException">Hash vacío o jugador no válido.</exception>
    public static DeviceLink Create(string deviceHash, int playerId)
    {
        if (string.IsNullOrWhiteSpace(deviceHash)) throw new DomainException("Dispositivo no válido.");
        if (playerId <= 0) throw new DomainException("Jugador no válido.");
        return new DeviceLink { DeviceHash = deviceHash, PlayerId = playerId, LinkedAtUtc = DateTime.UtcNow };
    }
}
