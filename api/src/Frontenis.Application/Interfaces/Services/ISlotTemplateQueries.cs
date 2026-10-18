using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consulta de las plantillas semanales.</summary>
public interface ISlotTemplateQueries
{
    /// <summary>Lista las plantillas, activas o no.</summary>
    Task<IReadOnlyList<SlotTemplateDto>> ListAsync(CancellationToken ct = default);
}
