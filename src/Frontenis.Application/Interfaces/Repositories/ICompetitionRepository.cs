using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

public interface ICompetitionRepository
{
    Task<IReadOnlyList<Competition>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga el agregado completo (grupos, parejas con jugadores y partidos).</summary>
    Task<Competition?> GetDetailedAsync(int id, CancellationToken ct = default);
    Task<Competition?> GetDetailedByMatchIdAsync(int matchId, CancellationToken ct = default);
    Task AddAsync(Competition competition, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
