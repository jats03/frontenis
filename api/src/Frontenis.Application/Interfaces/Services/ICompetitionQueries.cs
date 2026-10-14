using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consultas públicas (solo lectura) sobre competiciones.</summary>
public interface ICompetitionQueries
{
    Task<IReadOnlyList<CompetitionSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<CompetitionDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<GroupStandingsDto>> GetStandingsAsync(int id, CancellationToken ct = default);
}
