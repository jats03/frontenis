using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;

namespace Frontenis.Application.Services;

public sealed class TrainingQueries : ITrainingQueries
{
    private readonly ITrainingRepository _trainings;
    private readonly ITrainingDetailBuilder _detail;

    public TrainingQueries(ITrainingRepository trainings, ITrainingDetailBuilder detail)
    {
        _trainings = trainings;
        _detail = detail;
    }

    public async Task<IReadOnlyList<TrainingSummaryDto>> ListAsync(CancellationToken ct = default) =>
        (await _trainings.ListAsync(ct)).Select(s => s.ToSummary()).ToList();

    public async Task<TrainingDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        await _detail.BuildAsync(await _trainings.GetOrThrowAsync(id, ct), ct);
}
