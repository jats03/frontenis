namespace Frontenis.Domain.Enums;

/// <summary>Estado de una franja en un instante dado (se calcula, no se guarda).</summary>
public enum SlotStatus
{
    /// <summary>Aún no han abierto las inscripciones.</summary>
    Pending = 1,
    /// <summary>Se puede apuntar y quitar.</summary>
    Open = 2,
    /// <summary>Ya ha empezado: no admite cambios.</summary>
    Closed = 3,
    /// <summary>Cancelada por el administrador.</summary>
    Cancelled = 4
}
