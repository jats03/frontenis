namespace Frontenis.Application.Dtos;

// ---------- Plantillas semanales ----------
public sealed record SlotTemplateDto(int Id, DayOfWeek Day, TimeOnly Start, TimeOnly End, bool Active);
public sealed record SaveSlotTemplateRequest(DayOfWeek Day, TimeOnly Start, TimeOnly End, bool Active = true);

// ---------- Franjas ----------
public sealed record SlotAttendeeDto(int PlayerId, string Name);

/// <param name="IsOpen">Ahora mismo se puede apuntar (no cancelada, ya abierta y sin empezar).</param>
/// <param name="FewPlayers">Hay menos jugadores del mínimo recomendado (solo con la franja abierta).</param>
public sealed record SlotDto(
    int Id, DateOnly Date, TimeOnly Start, TimeOnly End, DateTime OpensAt, bool IsCancelled, bool IsOpen,
    bool FewPlayers, int MinPlayers, int? TemplateId, int AttendeeCount, IReadOnlyList<SlotAttendeeDto> Attendees);

/// <param name="OpensAt">Si no se indica, se calcula con la regla de apertura del club.</param>
public sealed record SaveSlotRequest(DateOnly Date, TimeOnly Start, TimeOnly End, DateTime? OpensAt = null);

public sealed record GenerateSlotsRequest(DateOnly From, int Weeks);
public sealed record GenerateSlotsResultDto(int Created);
public sealed record OpenWeekRequest(DateOnly WeekStart);
public sealed record OpenWeekResultDto(int Opened);

public sealed record AttendanceRequest(int PlayerId);
