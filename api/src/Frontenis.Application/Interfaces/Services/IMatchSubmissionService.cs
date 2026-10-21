using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Resultados que envían los jugadores identificados y que el administrador aprueba o rechaza.</summary>
public interface IMatchSubmissionService
{
    /// <summary>
    /// Envía (o corrige) el resultado de un partido. Queda pendiente de aprobación. Si ya hay uno enviado por otro
    /// jugador, solo ese jugador puede modificarlo.
    /// </summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Marcador no válido, partido ya jugado o envío de otro jugador.</exception>
    Task<MatchSubmissionDto> SubmitAsync(int matchId, int playerId, SubmitResultRequest request, CancellationToken ct = default);
    /// <summary>Envíos pendientes de aprobación de una competición (solo de partidos aún sin resultado).</summary>
    Task<IReadOnlyList<MatchSubmissionDto>> ListAsync(int competitionId, CancellationToken ct = default);
    /// <summary>El administrador aprueba el envío: se registra como resultado oficial del partido.</summary>
    Task<MatchDto> ApproveAsync(int submissionId, CancellationToken ct = default);
    /// <summary>El administrador rechaza el envío: se descarta y el jugador puede volver a enviarlo.</summary>
    Task RejectAsync(int submissionId, CancellationToken ct = default);
}
