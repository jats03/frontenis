using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de sesiones de entrenamiento.</summary>
public interface ITrainingRepository
{
    /// <summary>Lista las sesiones de entrenamiento con sus partidos.</summary>
    Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga la sesión con todos sus partidos.</summary>
    Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Añade una sesión nueva.</summary>
    Task AddAsync(TrainingSession session, CancellationToken ct = default);
    /// <summary>Marca la sesión (y sus partidos) para su eliminación.</summary>
    void Remove(TrainingSession session);
}
