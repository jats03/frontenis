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
    /// <summary>Añade un partido a una jornada de un grupo, sin más condiciones.</summary>
    Task<CompetitionDetailDto> AddMatchAsync(int competitionId, int groupId, AddMatchRequest request, CancellationToken ct = default);
    /// <summary>Intercambia las jornadas de dos partidos pendientes del mismo grupo.</summary>
    Task<CompetitionDetailDto> SwapMatchesAsync(int matchId, SwapMatchRequest request, CancellationToken ct = default);
    /// <summary>Intercambia dos jornadas enteras de un grupo, tengan o no partidos jugados.</summary>
    Task<CompetitionDetailDto> SwapRoundsAsync(int competitionId, int groupId, SwapRoundsRequest request, CancellationToken ct = default);
    /// <summary>Quita una jornada de un grupo (sin partidos jugados) y renumera las siguientes.</summary>
    Task<CompetitionDetailDto> RemoveRoundAsync(int competitionId, int groupId, int round, CancellationToken ct = default);
}
