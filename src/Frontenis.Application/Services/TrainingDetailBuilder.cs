using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Mapping;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;

namespace Frontenis.Application.Services;

/// <summary>Compone el detalle de una sesión: partidos con nombres y clasificaciones por pareja y por jugador.</summary>
public sealed class TrainingDetailBuilder : ITrainingDetailBuilder
{
    private readonly IPlayerRepository _players;

    public TrainingDetailBuilder(IPlayerRepository players) { _players = players; }

    public async Task<TrainingDetailDto> BuildAsync(TrainingSession session, CancellationToken ct = default)
    {
        var names = (await _players.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        string Name(int id) => names.GetValueOrDefault(id, "—");
        var games = session.ToGames();

        TrainingRowDto Row(TrainingRow r) => new(
            r.Rank, r.Player2Id == 0 ? Name(r.Player1Id) : $"{Name(r.Player1Id)} / {Name(r.Player2Id)}",
            r.Player2Id == 0 ? new[] { r.Player1Id } : new[] { r.Player1Id, r.Player2Id },
            r.Played, r.Won, r.Lost, r.PointsFor, r.PointsAgainst, r.PointsDiff, r.ClassificationPoints);

        return new TrainingDetailDto(
            session.ToSummary(),
            session.Matches.OrderBy(m => m.Order).ThenBy(m => m.Id).Select(m => new TrainingMatchDto(
                m.Id, m.Order, $"{Name(m.Home1Id)} / {Name(m.Home2Id)}", $"{Name(m.Away1Id)} / {Name(m.Away2Id)}",
                new[] { m.Home1Id, m.Home2Id }, new[] { m.Away1Id, m.Away2Id },
                m.TargetPoints, m.HomeScore, m.AwayScore, m.HomeWon)).ToList(),
            TrainingStandings.ByPair(games, session.PointsPerWin, session.PointsPerLoss).Select(Row).ToList(),
            TrainingStandings.ByPlayer(games, session.PointsPerWin, session.PointsPerLoss).Select(Row).ToList());
    }
}
