using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

// ---------- Plantillas semanales ----------
public sealed record SlotTemplateDto(int Id, DayOfWeek Day, TimeOnly Start, TimeOnly End, bool Active);
public sealed record SaveSlotTemplateRequest(DayOfWeek Day, TimeOnly Start, TimeOnly End, bool Active = true);

// ---------- Franjas ----------
public sealed record SlotAttendeeDto(int PlayerId, string Name);

/// <param name="Status">Estado ahora mismo: pendiente de abrir, abierta, cerrada (ya empezada) o cancelada.</param>
/// <param name="FewPlayers">Hay menos jugadores del mínimo recomendado (solo con la franja abierta).</param>
public sealed record SlotDto(
    int Id, DateOnly Date, TimeOnly Start, TimeOnly End, DateTime OpensAt, SlotStatus Status,
    bool FewPlayers, int MinPlayers, int? TemplateId, int AttendeeCount, IReadOnlyList<SlotAttendeeDto> Attendees);

/// <param name="OpensAt">Si no se indica, se calcula con la regla de apertura del club.</param>
public sealed record SaveSlotRequest(DateOnly Date, TimeOnly Start, TimeOnly End, DateTime? OpensAt = null);

public sealed record GenerateSlotsRequest(DateOnly From, int Weeks);
public sealed record GenerateSlotsResultDto(int Created);
public sealed record OpenWeekRequest(DateOnly WeekStart);
public sealed record OpenWeekResultDto(int Opened);

public sealed record AttendanceRequest(int PlayerId);

// ---------- Dispositivos y apadrinados ----------
public sealed record PlayerRefDto(int Id, string Name);
public sealed record DeviceMeDto(PlayerRefDto Owner, IReadOnlyList<PlayerRefDto> Delegates);
public sealed record LinkDeviceRequest(int PlayerId);
public sealed record DeviceLinkDto(int PlayerId, string PlayerName, DateTime LinkedAtUtc);
public sealed record PlayerDelegateDto(int OwnerId, string OwnerName, int DelegateId, string DelegateName);
public sealed record AddPlayerDelegateRequest(int OwnerPlayerId, int DelegatePlayerId);
