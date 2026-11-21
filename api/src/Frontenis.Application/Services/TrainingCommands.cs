using Frontenis.Application.Dtos;
using Frontenis.Application.Extensions;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Application.Services;

/// <summary>Implementación de <see cref="Interfaces.Services.ITrainingCommands"/>.</summary>
public sealed class TrainingCommands : ITrainingCommands
{
    private readonly ITrainingRepository _trainings;
    private readonly IPlayerRepository _players;
    private readonly ITrainingDetailBuilder _detail;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public TrainingCommands(ITrainingRepository trainings, IPlayerRepository players,
        ITrainingDetailBuilder detail, IUnitOfWork uow, IClock clock)
    {
        _clock = clock;
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

    public async Task<TrainingDetailDto> AddMatchAsync(int id, AddTrainingMatchRequest r, bool byAdmin = false, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        // Pasado el día del entreno ya no se registran partidos (el administrador sí, para correcciones).
        if (!byAdmin && session.IsClosedFor(_clock.Now)) throw new DomainException(TrainingSession.ClosedMessage);
        var p = await ResolvePlayersAsync(new[] { r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id }, ct);
        session.AddMatch(p[0], p[1], p[2], p[3], r.HomeScore, r.AwayScore, r.Ball);
        await _uow.SaveChangesAsync(ct);
        return await _detail.BuildAsync(session, ct);
    }

    public async Task<TrainingDetailDto> UpdateMatchAsync(int id, int matchId, UpdateTrainingMatchRequest r, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        var p = await ResolvePlayersAsync(new[] { r.Home1Id, r.Home2Id, r.Away1Id, r.Away2Id }, ct);
        session.UpdateMatch(matchId, p[0], p[1], p[2], p[3], r.HomeScore, r.AwayScore, r.Ball);
        await _uow.SaveChangesAsync(ct);
        return await _detail.BuildAsync(session, ct);
    }

    /// <summary>
    /// Comprueba que los jugadores existen. El id 0 es el comodín «Jugador de prueba» (como mucho uno por partido): se crea la
    /// primera vez que se usa y se sustituye por su id real.
    /// </summary>
    private async Task<int[]> ResolvePlayersAsync(int[] ids, CancellationToken ct)
    {
        var real = ids.Where(i => i != 0).Distinct().ToList();
        var found = await _players.GetByIdsAsync(real.Where(i => i > 0).ToList(), ct);
        if (real.Any(i => i < 0) || found.Count != real.Count) throw new NotFoundException("Algún jugador no existe.");
        if (!ids.Contains(0)) return ids;
        var test = await _players.GetOrCreateTestAsync(ct);
        if (test.Id == 0) await _uow.SaveChangesAsync(ct); // hace falta su id para guardar el partido
        return ids.Select(i => i == 0 ? test.Id : i).ToArray();
    }

    public async Task<TrainingDetailDto> RemoveMatchAsync(int id, int matchId, CancellationToken ct = default)
    {
        var session = await _trainings.GetOrThrowAsync(id, ct);
        session.RemoveMatch(matchId);
        await _uow.SaveChangesAsync(ct);
        return await _detail.BuildAsync(session, ct);
    }
}
