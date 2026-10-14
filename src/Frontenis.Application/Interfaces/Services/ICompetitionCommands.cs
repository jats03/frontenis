using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Ciclo de vida de una competición: alta, renombrado, formato y baja.</summary>
public interface ICompetitionCommands
{
    Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest request, CancellationToken ct = default);
    Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default);
    Task<CompetitionSummaryDto> ChangeSetsPerMatchAsync(int id, SetsPerMatchRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
