using Frontenis.Application.Models;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos del registro de acciones. Escribe por su cuenta, sin pasar por la unidad de trabajo.</summary>
public interface IAuditRepository
{
    /// <summary>Guarda el registro de una acción.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken ct = default);
    /// <summary>Registros que cumplen los filtros (más recientes primero), con el total sin paginar.</summary>
    Task<(IReadOnlyList<AuditEntry> Items, int Total)> SearchAsync(AuditFilter filter, CancellationToken ct = default);
    /// <summary>Borra los registros anteriores a la fecha indicada y devuelve cuántos eran.</summary>
    Task<int> PurgeOlderThanAsync(DateTime cutoffUtc, CancellationToken ct = default);
}
