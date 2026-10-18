using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Gestión de franjas concretas por el administrador.</summary>
public interface ISlotAdminCommands
{
    /// <summary>Crea una franja suelta (por ejemplo, un día extra).</summary>
    Task<SlotDto> CreateAsync(SaveSlotRequest request, CancellationToken ct = default);
    /// <summary>Cambia fecha, horario y apertura de una franja sin tocar su plantilla.</summary>
    Task<SlotDto> UpdateAsync(int id, SaveSlotRequest request, CancellationToken ct = default);
    /// <summary>Cancela la franja conservando los apuntados.</summary>
    Task<SlotDto> CancelAsync(int id, CancellationToken ct = default);
    /// <summary>Deshace la cancelación.</summary>
    Task<SlotDto> RestoreAsync(int id, CancellationToken ct = default);
    /// <summary>Abre ya las inscripciones de la franja.</summary>
    Task<SlotDto> OpenNowAsync(int id, CancellationToken ct = default);
    /// <summary>Deshace «Abrir ya»: la franja vuelve a quedar sin abrir hasta su hora habitual (conserva los apuntados).</summary>
    Task<SlotDto> CloseSignUpsAsync(int id, CancellationToken ct = default);
    /// <summary>Elimina la franja con sus apuntados.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
