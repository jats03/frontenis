using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Ciclo de vida de una competición: alta, renombrado, formato y baja.</summary>
public interface ICompetitionCommands
{
    /// <summary>Crea una competición (torneo, liga o liga rotatoria) en estado de inscripción.</summary>
    Task<CompetitionSummaryDto> CreateAsync(CreateCompetitionRequest request, CancellationToken ct = default);
    /// <summary>Cambia el nombre de la competición.</summary>
    Task<CompetitionSummaryDto> RenameAsync(int id, RenameCompetitionRequest request, CancellationToken ct = default);
    /// <summary>Cambia entre 1 y 2 sets por partido; solo es posible si aún no se ha jugado ningún partido.</summary>
    Task<CompetitionSummaryDto> ChangeSetsPerMatchAsync(int id, SetsPerMatchRequest request, CancellationToken ct = default);
    /// <summary>Elimina la competición y todos sus datos.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}
