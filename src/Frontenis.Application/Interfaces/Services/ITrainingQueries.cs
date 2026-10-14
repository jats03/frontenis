using Frontenis.Application.Dtos;

namespace Frontenis.Application.Interfaces.Services;

public interface ITrainingQueries
{
    Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default);
}
