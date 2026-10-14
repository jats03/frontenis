using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Sorteos, calendario, fase final y continuación de ligas.</summary>
public interface ICompetitionDrawService
{
    Task<CompetitionDetailDto> DrawAsync(int id, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    Task<CompetitionDetailDto> GeneratePlayoffsAsync(int id, CancellationToken ct = default);
    /// <summary>Liga rotatoria: reparte los jugadores en grupos y genera las jornadas con parejas cambiantes.</summary>
    Task<CompetitionDetailDto> DrawRotatingAsync(int id, PlayersSelection selection, CancellationToken ct = default);
    Task<CompetitionDetailDto> DrawRotatingManualAsync(int id, ManualDrawRequest request, CancellationToken ct = default);
    /// <summary>Liga rotatoria terminada: crea la siguiente con ascensos y descensos aplicados.</summary>
    Task<CompetitionSummaryDto> CreateNextLeagueAsync(int id, CancellationToken ct = default);
}
