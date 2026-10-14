using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Services;

/// <summary>Construye el detalle de una sesión de entrenamiento (partidos y clasificaciones con nombres).</summary>
public interface ITrainingDetailBuilder
{
    Task<TrainingDetailDto> BuildAsync(TrainingSession session, CancellationToken ct = default);
}
