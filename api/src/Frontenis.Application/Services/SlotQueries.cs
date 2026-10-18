using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ISlotQueries"/>.</summary>
public sealed class SlotQueries : ISlotQueries
{
    private const int MaxRangeDays = 92;

    private readonly ISlotRepository _slots;
    private readonly ISlotDtoBuilder _builder;
    private readonly IClock _clock;

    public SlotQueries(ISlotRepository slots, ISlotDtoBuilder builder, IClock clock)
    {
        _slots = slots;
        _builder = builder;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SlotDto>> ListAsync(DateOnly? from, DateOnly? to, SlotAudience audience = SlotAudience.Public, CancellationToken ct = default)
    {
        var start = from ?? Weeks.MondayOf(DateOnly.FromDateTime(_clock.Now));
        var end = to ?? start.AddDays(13);
        if (end < start) throw new DomainException("La fecha final no puede ser anterior a la inicial.");
        if (end.DayNumber - start.DayNumber > MaxRangeDays) throw new DomainException($"El rango no puede superar {MaxRangeDays} días.");
        var slots = await _slots.ListAsync(start, end, ct);
        if (audience == SlotAudience.Public)
        {
            var now = _clock.Now;
            slots = slots.Where(s => SlotVisibility.IsPublic(s, now)).ToList();
        }
        return await _builder.BuildManyAsync(slots, ct);
    }
}
