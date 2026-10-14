using Frontenis.Domain.Entities;

namespace Frontenis.Application.Interfaces.Repositories;

public interface ITrainingRepository
{
    Task<IReadOnlyList<TrainingSession>> ListAsync(CancellationToken ct = default);
    /// <summary>Carga la sesión con todos sus partidos.</summary>
    Task<TrainingSession?> GetAsync(int id, CancellationToken ct = default);
    Task AddAsync(TrainingSession session, CancellationToken ct = default);
    void Remove(TrainingSession session);
}
