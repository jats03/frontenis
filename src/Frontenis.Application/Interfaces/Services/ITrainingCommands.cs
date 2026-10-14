using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface ITrainingCommands
{
    Task<TrainingSummaryDto> CreateAsync(CreateTrainingRequest request, CancellationToken ct = default);
    Task<TrainingSummaryDto> RenameAsync(int id, RenameTrainingRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest request, CancellationToken ct = default);
    Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default);
}
