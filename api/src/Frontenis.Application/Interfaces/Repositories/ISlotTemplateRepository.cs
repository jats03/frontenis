using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de plantillas semanales.</summary>
public interface ISlotTemplateRepository
{
    /// <summary>Lista las plantillas por día de la semana (lunes primero) y hora.</summary>
    Task<IReadOnlyList<SlotTemplate>> ListAsync(CancellationToken ct = default);
    /// <summary>Devuelve la plantilla o <c>null</c> si no existe.</summary>
    Task<SlotTemplate?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Añade una plantilla.</summary>
    Task AddAsync(SlotTemplate template, CancellationToken ct = default);
    /// <summary>Marca la plantilla para su eliminación (las franjas ya generadas se conservan).</summary>
    void Remove(SlotTemplate template);
}
