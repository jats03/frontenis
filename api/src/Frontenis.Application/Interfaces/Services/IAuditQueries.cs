using Frontenis.Application.Dtos;
using Frontenis.Application.Models;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consulta del registro de actividad (solo administrador).</summary>
public interface IAuditQueries
{
    /// <summary>Página de registros, los más recientes primero, que cumplen los filtros.</summary>
    Task<AuditPageDto> SearchAsync(AuditFilter filter, CancellationToken ct = default);
}
