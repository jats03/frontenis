using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ISlotAdminCommands"/>.</summary>
public sealed class SlotAdminCommands : ISlotAdminCommands
{
    private readonly ISlotRepository _slots;
    private readonly ISlotDtoBuilder _builder;
    private readonly IOpeningPolicy _opening;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public SlotAdminCommands(ISlotRepository slots, ISlotDtoBuilder builder, IOpeningPolicy opening,
        IClock clock, IUnitOfWork uow)
    {
        _slots = slots;
        _builder = builder;
        _opening = opening;
        _clock = clock;
        _uow = uow;
    }

    public async Task<SlotDto> CreateAsync(SaveSlotRequest r, CancellationToken ct = default)
    {
        var slot = TrainingSlot.Create(r.Date, r.Start, r.End, r.OpensAt ?? _opening.OpensAtFor(r.Date));
        await _slots.AddAsync(slot, ct);
        await _uow.SaveChangesAsync(ct);
        return await _builder.BuildAsync(slot, ct);
    }

    public async Task<SlotDto> UpdateAsync(int id, SaveSlotRequest r, CancellationToken ct = default)
    {
        var slot = await _slots.GetOrThrowAsync(id, ct);
        slot.Reschedule(r.Date, r.Start, r.End, r.OpensAt ?? _opening.OpensAtFor(r.Date));
        await _uow.SaveChangesAsync(ct);
        return await _builder.BuildAsync(slot, ct);
    }

    public Task<SlotDto> CancelAsync(int id, CancellationToken ct = default) =>
        ChangeAsync(id, slot => slot.Cancel(), ct);

    public Task<SlotDto> RestoreAsync(int id, CancellationToken ct = default) =>
        ChangeAsync(id, slot => slot.Restore(), ct);

    public Task<SlotDto> OpenNowAsync(int id, CancellationToken ct = default) =>
        ChangeAsync(id, slot => slot.OpenNow(_clock.Now), ct);

    public Task<SlotDto> CloseSignUpsAsync(int id, CancellationToken ct = default) =>
        ChangeAsync(id, slot => slot.CloseSignUps(_clock.Now, _opening.OpensAtFor(slot.Date)), ct);

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _slots.Remove(await _slots.GetOrThrowAsync(id, ct));
        await _uow.SaveChangesAsync(ct);
    }

    /// <summary>Carga la franja, aplica el cambio, guarda y devuelve su DTO.</summary>
    private async Task<SlotDto> ChangeAsync(int id, Action<TrainingSlot> change, CancellationToken ct)
    {
        var slot = await _slots.GetOrThrowAsync(id, ct);
        change(slot);
        await _uow.SaveChangesAsync(ct);
        return await _builder.BuildAsync(slot, ct);
    }
}
