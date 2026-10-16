using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.IAttendanceCommands"/>; las reglas viven en la franja.</summary>
public sealed class AttendanceCommands : IAttendanceCommands
{
    private readonly ISlotRepository _slots;
    private readonly IPlayerRepository _players;
    private readonly ISlotDtoBuilder _builder;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public AttendanceCommands(ISlotRepository slots, IPlayerRepository players, ISlotDtoBuilder builder,
        IClock clock, IUnitOfWork uow)
    {
        _slots = slots;
        _players = players;
        _builder = builder;
        _clock = clock;
        _uow = uow;
    }

    public async Task<SlotDto> SignUpAsync(int slotId, int playerId, CancellationToken ct = default)
    {
        var slot = await _slots.GetOrThrowAsync(slotId, ct);
        await _players.GetOrThrowAsync(playerId, ct);
        slot.SignUp(playerId, _clock.Now);
        await _uow.SaveChangesAsync(ct);
        return await _builder.BuildAsync(slot, ct);
    }

    public async Task<SlotDto> WithdrawAsync(int slotId, int playerId, CancellationToken ct = default)
    {
        var slot = await _slots.GetOrThrowAsync(slotId, ct);
        slot.Withdraw(playerId, _clock.Now);
        await _uow.SaveChangesAsync(ct);
        return await _builder.BuildAsync(slot, ct);
    }
}
