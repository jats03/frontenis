using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Sorteos, calendario, fase final y continuación de ligas.</summary>
public interface ICompetitionDrawService
{
    /// <summary>Sortea las parejas inscritas en grupos y genera el calendario de la fase de grupos.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Faltan parejas, el sorteo ya se hizo o la competición es rotatoria.</exception>
    Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default);
    /// <summary>Igual que el sorteo, pero con los grupos formados a mano por el administrador.</summary>
    Task<CompetitionDetailDto> DrawManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    /// <summary>Genera el cuadro de la fase final con los clasificados de cada grupo (solo torneos con la fase de grupos terminada).</summary>
    Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default);
    /// <summary>Liga rotatoria: reparte los jugadores en grupos y genera las jornadas con parejas cambiantes.</summary>
    Task<CompetitionDetailDto> DrawRotatingAsync(int id, PlayersSelection selection, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawRotatingManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    /// <summary>Liga rotatoria terminada: crea la siguiente con ascensos y descensos aplicados.</summary>
    Task<CompetitionSummaryDto> CreateNextLeagueAsync(int id, CancellationToken ct = default);
}
