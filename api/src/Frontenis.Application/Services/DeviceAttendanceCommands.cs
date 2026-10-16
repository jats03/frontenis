using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>
/// Apuntarse desde un dispositivo: comprueba a quién puede apuntar y delega el resto en
/// <see cref="IAttendanceCommands"/>, que aplica las reglas de la franja.
/// </summary>
public sealed class DeviceAttendanceCommands : IDeviceAttendanceCommands
{
    private readonly IDeviceAccessResolver _access;
    private readonly IAttendanceCommands _attendance;

    public DeviceAttendanceCommands(IDeviceAccessResolver access, IAttendanceCommands attendance)
    {
        _access = access;
        _attendance = attendance;
    }

    public async Task<SlotDto> SignUpAsync(string? deviceToken, int slotId, int playerId, CancellationToken ct = default)
    {
        var access = await Authorize(deviceToken, playerId, ct);
        return await _attendance.SignUpAsync(slotId, playerId, access.OwnerPlayerId, ct);
    }

    public async Task<SlotDto> WithdrawAsync(string? deviceToken, int slotId, int playerId, CancellationToken ct = default)
    {
        await Authorize(deviceToken, playerId, ct);
        return await _attendance.WithdrawAsync(slotId, playerId, ct);
    }

    private async Task<DeviceAccess> Authorize(string? deviceToken, int playerId, CancellationToken ct)
    {
        var access = await _access.ResolveAsync(deviceToken, ct);
        if (!access.Allows(playerId)) throw new DomainException("Desde este dispositivo no puedes apuntar a ese jugador.");
        return access;
    }
}
