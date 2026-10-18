using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>
/// Franja concreta de entrenamiento (una fecha y un horario) a la que se apuntan los jugadores.
/// Fecha, horas y apertura son hora local del club (Madrid), sin zona horaria; por eso las reglas reciben
/// siempre "ahora" como parámetro (hora local) en lugar de leer el reloj del sistema.
/// </summary>
public sealed class TrainingSlot
{
    /// <summary>Tope de seguridad de apuntados por franja.</summary>
    public const int MaxAttendees = 100;

    private readonly List<SlotAttendance> _attendees = new();

    private TrainingSlot() { }

    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    /// <summary>Momento (hora local) a partir del cual los jugadores pueden apuntarse.</summary>
    public DateTime OpensAt { get; private set; }
    public bool IsCancelled { get; private set; }
    /// <summary>Plantilla de la que se generó, o <c>null</c> si es una franja extra o su plantilla se eliminó.</summary>
    public int? TemplateId { get; private set; }
    public IReadOnlyCollection<SlotAttendance> Attendees => _attendees;

    /// <summary>Momento (hora local) en que empieza la franja.</summary>
    public DateTime StartsAt => Date.ToDateTime(Start);

    /// <summary>Crea una franja.</summary>
    /// <exception cref="DomainException">Horario no válido o apertura posterior al inicio de la franja.</exception>
    public static TrainingSlot Create(DateOnly date, TimeOnly start, TimeOnly end, DateTime opensAt, int? templateId = null)
    {
        ValidateTimes(start, end);
        var slot = new TrainingSlot { TemplateId = templateId };
        slot.Apply(date, start, end, opensAt);
        return slot;
    }

    /// <summary>Cambia fecha, horario y apertura de esta franja (no afecta a la plantilla).</summary>
    public void Reschedule(DateOnly date, TimeOnly start, TimeOnly end, DateTime opensAt)
    {
        ValidateTimes(start, end);
        Apply(date, start, end, opensAt);
    }

    /// <summary>Cancela la franja: deja de aceptar inscripciones y se conservan los apuntados.</summary>
    public void Cancel() => IsCancelled = true;

    /// <summary>Deshace la cancelación.</summary>
    public void Restore() => IsCancelled = false;

    /// <summary>Abre ya las inscripciones (apertura manual). Devuelve <c>false</c> si ya estaban abiertas.</summary>
    /// <exception cref="DomainException">La franja está cancelada o ya ha empezado.</exception>
    public bool OpenNow(DateTime now)
    {
        if (IsCancelled) throw new DomainException("La franja está cancelada.");
        if (now >= StartsAt) throw new DomainException("La franja ya ha empezado.");
        if (OpensAt <= now) return false;
        OpensAt = now;
        return true;
    }

    /// <summary>
    /// Se puede deshacer la apertura anticipada: la franja está abierta, no ha empezado ni está cancelada, y su
    /// apertura programada (<paramref name="scheduledOpening"/>) todavía no ha llegado.
    /// </summary>
    public bool CanCloseSignUps(DateTime now, DateTime scheduledOpening) =>
        !IsCancelled && now < StartsAt && OpensAt <= now && scheduledOpening > now;

    /// <summary>
    /// Vuelve a cerrar las inscripciones (deshace «Abrir ya»): la franja queda sin abrir hasta su apertura programada.
    /// Los apuntados se conservan. Devuelve <c>false</c> si ya estaba sin abrir.
    /// </summary>
    /// <exception cref="DomainException">Cancelada, ya empezada o abierta por su horario habitual.</exception>
    public bool CloseSignUps(DateTime now, DateTime scheduledOpening)
    {
        if (IsCancelled) throw new DomainException("La franja está cancelada.");
        if (now >= StartsAt) throw new DomainException("La franja ya ha empezado.");
        if (OpensAt > now) return false;
        if (scheduledOpening <= now)
            throw new DomainException("Esta franja se abre sola a esta hora. Si no quieres que se apunten, cancélala.");
        OpensAt = scheduledOpening;
        return true;
    }

    /// <summary>Estado de la franja en el instante <paramref name="now"/> (hora local).</summary>
    public SlotStatus StatusAt(DateTime now)
    {
        if (IsCancelled) return SlotStatus.Cancelled;
        if (now >= StartsAt) return SlotStatus.Closed;
        return now >= OpensAt ? SlotStatus.Open : SlotStatus.Pending;
    }

    /// <summary>Se puede apuntar: no cancelada, ya abierta y todavía no empezada.</summary>
    public bool IsOpen(DateTime now) => StatusAt(now) == SlotStatus.Open;

    /// <summary>Hay menos jugadores del mínimo recomendado. Solo avisa mientras la franja está abierta.</summary>
    public bool HasFewPlayers(int minPlayers, DateTime now) => IsOpen(now) && _attendees.Count < minPlayers;

    /// <summary>Apunta a un jugador. <paramref name="registeredByPlayerId"/> es quien lo hace (<c>null</c> si es el administrador).</summary>
    /// <exception cref="DomainException">Cancelada, aún sin abrir, ya empezada, jugador repetido o franja llena.</exception>
    public SlotAttendance SignUp(int playerId, DateTime now, int? registeredByPlayerId = null)
    {
        if (playerId <= 0) throw new DomainException("Jugador no válido.");
        EnsureOpen(now);
        if (_attendees.Any(a => a.PlayerId == playerId)) throw new DomainException("El jugador ya está apuntado.");
        if (_attendees.Count >= MaxAttendees) throw new DomainException("La franja ha alcanzado el máximo de apuntados.");
        var attendance = new SlotAttendance(playerId, DateTime.UtcNow, registeredByPlayerId);
        _attendees.Add(attendance);
        return attendance;
    }

    /// <summary>Quita a un jugador. Mientras la franja no haya empezado se puede hacer aunque aún no esté abierta.</summary>
    /// <exception cref="DomainException">La franja ya ha empezado.</exception>
    /// <exception cref="NotFoundException">El jugador no estaba apuntado.</exception>
    public void Withdraw(int playerId, DateTime now)
    {
        if (now >= StartsAt) throw new DomainException("La franja ya ha empezado.");
        var attendance = _attendees.FirstOrDefault(a => a.PlayerId == playerId)
            ?? throw new NotFoundException("El jugador no está apuntado a esta franja.");
        _attendees.Remove(attendance);
    }

    internal static void ValidateTimes(TimeOnly start, TimeOnly end)
    {
        if (end <= start) throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
    }

    private void EnsureOpen(DateTime now)
    {
        if (IsCancelled) throw new DomainException("La franja está cancelada.");
        if (now >= StartsAt) throw new DomainException("La franja ya ha empezado.");
        if (now < OpensAt) throw new DomainException("Las inscripciones de esta franja todavía no están abiertas.");
    }

    private void Apply(DateOnly date, TimeOnly start, TimeOnly end, DateTime opensAt)
    {
        if (opensAt > date.ToDateTime(start)) throw new DomainException("La apertura no puede ser posterior al inicio de la franja.");
        Date = date;
        Start = start;
        End = end;
        OpensAt = DateTime.SpecifyKind(opensAt, DateTimeKind.Unspecified);
    }
}
