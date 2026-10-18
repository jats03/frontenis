using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Gestión de las plantillas semanales.</summary>
public interface ISlotTemplateCommands
{
    /// <summary>Crea una plantilla activa.</summary>
    Task<SlotTemplateDto> CreateAsync(SaveSlotTemplateRequest request, CancellationToken ct = default);
    /// <summary>Modifica una plantilla; las franjas ya generadas no cambian.</summary>
    Task<SlotTemplateDto> UpdateAsync(int id, SaveSlotTemplateRequest request, CancellationToken ct = default);
    /// <summary>Elimina la plantilla; las franjas ya generadas se conservan.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
