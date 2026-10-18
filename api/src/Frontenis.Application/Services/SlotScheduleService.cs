using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ISlotScheduleService"/>.</summary>
public sealed class SlotScheduleService : ISlotScheduleService
{
    public const int MaxWeeks = 8;

    private readonly ISlotRepository _slots;
    private readonly ISlotTemplateRepository _templates;
    private readonly ISlotGenerator _generator;
    private readonly IOpeningPolicy _opening;
    private readonly IClock _clock;
    private readonly IUnitOfWork _uow;

    public SlotScheduleService(ISlotRepository slots, ISlotTemplateRepository templates, ISlotGenerator generator,
        IOpeningPolicy opening, IClock clock, IUnitOfWork uow)
    {
        _slots = slots;
        _templates = templates;
        _generator = generator;
        _opening = opening;
        _clock = clock;
        _uow = uow;
    }

    public async Task<GenerateSlotsResultDto> GenerateAsync(GenerateSlotsRequest r, CancellationToken ct = default)
    {
        if (r.Weeks < 1 || r.Weeks > MaxWeeks) throw new DomainException($"Indica entre 1 y {MaxWeeks} semanas.");
        if (r.From < DateOnly.FromDateTime(_clock.Now)) throw new DomainException("No se pueden generar franjas en fechas pasadas.");

        var to = r.From.AddDays(r.Weeks * 7 - 1);
        var planned = _generator.Generate(await _templates.ListAsync(ct), r.From, to);
        var existing = (await _slots.GeneratedKeysAsync(r.From, to, ct)).ToHashSet();

        var created = planned
            .Where(p => !existing.Contains(new GeneratedSlotKey(p.TemplateId, Weeks.MondayOf(p.Date))))
            .Select(p => TrainingSlot.Create(p.Date, p.Start, p.End, _opening.OpensAtFor(p.Date), p.TemplateId))
            .ToList();

        if (created.Count > 0)
        {
            await _slots.AddRangeAsync(created, ct);
            await _uow.SaveChangesAsync(ct);
        }
        return new GenerateSlotsResultDto(created.Count);
    }

    public async Task<OpenWeekResultDto> OpenWeekAsync(OpenWeekRequest r, CancellationToken ct = default)
    {
        var monday = Weeks.MondayOf(r.WeekStart);
        var now = _clock.Now;
        var opened = 0;
        foreach (var slot in await _slots.ListForUpdateAsync(monday, monday.AddDays(6), ct))
            if (!slot.IsCancelled && slot.StartsAt > now && slot.OpenNow(now)) opened++;

        if (opened > 0) await _uow.SaveChangesAsync(ct);
        return new OpenWeekResultDto(opened);
    }

    public async Task<CloseWeekResultDto> CloseWeekAsync(OpenWeekRequest r, CancellationToken ct = default)
    {
        var monday = Weeks.MondayOf(r.WeekStart);
        var now = _clock.Now;
        var closed = 0;
        foreach (var slot in await _slots.ListForUpdateAsync(monday, monday.AddDays(6), ct))
        {
            var scheduled = _opening.OpensAtFor(slot.Date);
            if (slot.CanCloseSignUps(now, scheduled) && slot.CloseSignUps(now, scheduled)) closed++;
        }

        if (closed > 0) await _uow.SaveChangesAsync(ct);
        return new CloseWeekResultDto(closed);
    }
}
