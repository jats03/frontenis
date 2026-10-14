using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Resultados de los partidos de una competición.</summary>
public interface IMatchService
{
    /// <summary>Registra el resultado de un partido de competición, por sets o con un marcador simple, y avanza al ganador en el cuadro si procede.</summary>
    /// <exception cref="Frontenis.Domain.Exceptions.DomainException">Marcador no válido o competición en un estado que no permite modificar el partido.</exception>
    Task<MatchDto> SetResultAsync(int matchId, SetResultRequest request, CancellationToken ct = default);
    /// <summary>Borra el resultado de un partido, siempre que la ronda siguiente aún no tenga resultado.</summary>
    Task<MatchDto> ClearResultAsync(int matchId, CancellationToken ct = default);
}
