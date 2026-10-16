using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>
/// Compone los DTO de franjas. El estado (abierta, pocos jugadores) depende de la hora actual, por eso estas
/// consultas no se cachean: una caché de horas dejaría franjas "abiertas" ya empezadas.
/// </summary>
public sealed class SlotDtoBuilder : ISlotDtoBuilder
{
    private readonly IPlayerRepository _players;
    private readonly IClock _clock;
    private readonly BookingOptions _options;

    public SlotDtoBuilder(IPlayerRepository players, IClock clock, BookingOptions options)
    {
        _players = players;
        _clock = clock;
        _options = options;
    }

    public async Task<SlotDto> BuildAsync(TrainingSlot slot, CancellationToken ct = default) =>
        (await BuildManyAsync(new[] { slot }, ct))[0];

    public async Task<IReadOnlyList<SlotDto>> BuildManyAsync(IReadOnlyCollection<TrainingSlot> slots, CancellationToken ct = default)
    {
        var ids = slots.SelectMany(s => s.Attendees).Select(a => a.PlayerId).Distinct().ToList();
        var names = ids.Count == 0
            ? new Dictionary<int, string>()
            : (await _players.GetByIdsAsync(ids, ct)).ToDictionary(p => p.Id, p => p.Name);
        var now = _clock.Now;
        return slots.Select(s => ToDto(s, now, names)).ToList();
    }

    private SlotDto ToDto(TrainingSlot s, DateTime now, IReadOnlyDictionary<int, string> names)
    {
        var attendees = s.Attendees
            .OrderBy(a => a.RegisteredAtUtc).ThenBy(a => a.Id)
            .Select(a => new SlotAttendeeDto(a.PlayerId, names.GetValueOrDefault(a.PlayerId, "—")))
            .ToList();
        return new SlotDto(s.Id, s.Date, s.Start, s.End, s.OpensAt, s.IsCancelled, s.IsOpen(now),
            s.HasFewPlayers(_options.MinPlayers, now), _options.MinPlayers, s.TemplateId, attendees.Count, attendees);
    }
}
