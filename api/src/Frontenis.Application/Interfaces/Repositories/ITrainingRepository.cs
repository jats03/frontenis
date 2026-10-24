using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de sesiones de entrenamiento.</summary>
public interface ITrainingRepository
{
    /// <summary>Lista las sesiones de entrenamiento con sus partidos.</summary>
    Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga la sesión con todos sus partidos.</summary>
    Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default);
    /// <summary>Indica si ya hay una sesión de entrenamiento ese día.</summary>
    Task<bool> ExistsOnAsync(DateTime date, CancellationToken ct = default);
    /// <summary>Indica si el entreno automático de ese día ya se gestionó.</summary>
    Task<bool> AutoRunExistsAsync(DateTime date, CancellationToken ct = default);
    /// <summary>Marca como gestionado el entreno automático de ese día.</summary>
    Task AddAutoRunAsync(DateTime date, CancellationToken ct = default);
    /// <summary>Añade una sesión nueva.</summary>
    Task AddAsync(TrainingSession session, CancellationToken ct = default);
    /// <summary>Marca la sesión (y sus partidos) para su eliminación.</summary>
    void Remove(TrainingSession session);
}
