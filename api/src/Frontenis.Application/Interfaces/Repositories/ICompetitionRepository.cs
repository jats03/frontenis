using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos del agregado <see cref="Frontenis.Domain.Entities.Competition"/>.</summary>
public interface ICompetitionRepository
{
    /// <summary>Lista las competiciones (sin cargar grupos ni partidos).</summary>
    Task<IReadOnlyList<Competition>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga el agregado completo (grupos, parejas con jugadores y partidos).</summary>
    Task<Competition?> GetDetailedAsync(int id, CancellationToken ct = default);
    /// <summary>Carga el agregado completo de la competición a la que pertenece el partido, o <c>null</c> si el partido no existe.</summary>
    Task<Competition?> GetDetailedByMatchIdAsync(int matchId, CancellationToken ct = default);
    /// <summary>Añade una competición nueva (se persiste con <see cref="IUnitOfWork.SaveChangesAsync"/>).</summary>
    Task AddAsync(Competition competition, CancellationToken ct = default);
    /// <summary>Elimina la competición con todos sus grupos, inscripciones y partidos.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
