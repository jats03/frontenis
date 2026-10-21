using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de los resultados enviados por jugadores.</summary>
public interface IMatchSubmissionRepository
{
    /// <summary>Envío pendiente de un partido o <c>null</c>.</summary>
    Task<MatchSubmission?> GetByMatchAsync(int matchId, CancellationToken ct = default);
    /// <summary>Envío por su identificador o <c>null</c>.</summary>
    Task<MatchSubmission?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Envíos pendientes de los partidos de una competición, los más antiguos primero.</summary>
    Task<IReadOnlyList<MatchSubmission>> ListByCompetitionAsync(int competitionId, CancellationToken ct = default);
    Task AddAsync(MatchSubmission submission, CancellationToken ct = default);
    void Remove(MatchSubmission submission);
}
