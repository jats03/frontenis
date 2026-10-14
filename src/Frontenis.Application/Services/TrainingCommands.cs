using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ITrainingCommands"/>.</summary>
public sealed class TrainingCommands : ITrainingCommands
{
    private readonly ITrainingRepository _trainings;
    private readonly IPlayerRepository _players;
    private readonly ITrainingDetailBuilder _detail;
    private readonly IUnitOfWork _uow;

    public TrainingCommands(ITrainingRepository trainings, IPlayerRepository players,
        ITrainingDetailBuilder detail, IUnitOfWork uow)
    {
        _trainings = trainings;
        _players = players;
        _detail = detail;
        _uow = uow;
    }

    public async Task<TrainingSummaryDto> CreateAsync(CreateTrainingRequest r, CancellationToken ct = default)
    {
        var session = TrainingSession.Create(r.Name, r.Date, r.PointsPerWin, r.PointsPerLoss);
        await _trainings.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);
        return session.ToSummary();
    }

    public async Task<TrainingSummaryDto> RenameAsync(int id, RenameTrainingRequest r, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        session.Rename(r.Name);
        await _uow.SaveChangesAsync(ct);
        return session.ToSummary();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _trainings.Remove(await _trainings.GetOrThrowAsync(id, ct));
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest r, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        var ids = new[] { r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id };
        var found = await _players.GetByIdsAsync(ids.Distinct().ToList(), ct);
        if (found.Count != ids.Distinct().Count()) throw new NotFoundException("Algún jugador no existe.");
        session.AddMatch(r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id, r.TargetPoints, r.HomeScore, r.AwayScore);
        await _uow.SaveChangesAsync(ct);
        return await _detail.BuildAsync(session, ct);
    }

    public async Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        session.RemoveMatch(matchId);
        await _uow.SaveChangesAsync(ct);
        return await _detail.BuildAsync(session, ct);
    }
}
