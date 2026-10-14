using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Consultas públicas (solo lectura) sobre entrenamientos.</summary>
public interface ITrainingQueries
{
    /// <summary>Lista las sesiones de entrenamiento.</summary>
    Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default);
    /// <summary>Detalle de una sesión: partidos y clasificaciones.</summary>
    Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default);
}
