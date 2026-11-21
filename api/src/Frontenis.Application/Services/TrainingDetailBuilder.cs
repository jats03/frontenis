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
        // Los comodines (el «Jugador de prueba») no salen en la lista de jugadores: se buscan solo si hace falta.
        var inMatches = session.Matches.SelectMany(m => new[] { m.Home1Id, m.Home2Id, m.Away1Id, m.Away2Id }).Distinct().ToList();
        var missing = inMatches.Where(id => !names.ContainsKey(id)).ToList();
        var wildcards = new HashSet<int>();
        if (missing.Count > 0)
            foreach (var p in await _players.GetByIdsAsync(missing, ct))
            {
                names[p.Id] = p.Name;
                if (p.IsReserve) wildcards.Add(p.Id);
            }
        string Name(int id) => names.GetValueOrDefault(id, "—");
        // En las respuestas, el comodín va con el id 0 (el mismo que se envía para elegirlo).
        int PublicId(int id) => wildcards.Contains(id) ? 0 : id;
        var games = session.ToGames();

        TrainingRowDto Row(TrainingRow r) => new(
            r.Rank, r.Player2Id == 0 ? Name(r.Player1Id) : $"{Name(r.Player1Id)} / {Name(r.Player2Id)}",
            r.Player2Id == 0 ? new[] { r.Player1Id } : new[] { r.Player1Id, r.Player2Id },
            r.Played, r.Won, r.Lost, r.PointsFor, r.PointsAgainst, r.PointsDiff, r.ClassificationPoints);

        // Los partidos anteriores a que se guardara la hora no la tienen (fecha por defecto): se devuelven sin hora.
        static DateTime? PlayedAt(DateTime utc) => utc.Year < 2000 ? null : DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return new TrainingDetailDto(
            session.ToSummary(),
            session.Matches.OrderBy(m => m.Order).ThenBy(m => m.Id).Select(m => new TrainingMatchDto(
                m.Id, m.Order, $"{Name(m.Home1Id)} / {Name(m.Home2Id)}", $"{Name(m.Away1Id)} / {Name(m.Away2Id)}",
                new[] { PublicId(m.Home1Id), PublicId(m.Home2Id) }, new[] { PublicId(m.Away1Id), PublicId(m.Away2Id) },
                m.HomeScore, m.AwayScore, m.HomeWon, PlayedAt(m.PlayedAtUtc), m.Ball)).ToList(),
            TrainingStandings.ByPair(games, session.PointsPerWin, session.PointsPerLoss, wildcards).Select(Row).ToList(),
            TrainingStandings.ByPlayer(games, session.PointsPerWin, session.PointsPerLoss, wildcards).Select(Row).ToList());
    }
}
