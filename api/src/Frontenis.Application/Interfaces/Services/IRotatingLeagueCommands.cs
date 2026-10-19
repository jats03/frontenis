using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Cambios del administrador en una liga rotatoria en curso. Todos devuelven la liga actualizada.</summary>
public interface IRotatingLeagueCommands
{
    /// <summary>Añade un jugador al último grupo y recalcula los partidos pendientes de ese grupo; los jugados se conservan.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Liga sin sortear o terminada, o jugador que ya participa.</exception>
    Task<CompetitionDetailDto> AddPlayerAsync(int competitionId, AddRotatingPlayerRequest request, CancellationToken ct = default);
    /// <summary>Mueve un partido pendiente a otra jornada de su grupo.</summary>
    Task<CompetitionDetailDto> MoveMatchAsync(int matchId, MoveMatchRequest request, CancellationToken ct = default);
    /// <summary>Quita una jornada de un grupo (sin partidos jugados) y renumera las siguientes.</summary>
    Task<CompetitionDetailDto> RemoveRoundAsync(int competitionId, int groupId, int round, CancellationToken ct = default);
}
