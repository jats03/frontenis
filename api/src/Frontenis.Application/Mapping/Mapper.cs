using Frontenis.Application.Dtos;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;

namespace Frontenis.Application.Mapping;

/// <summary>Conversión de entidades de competiciones y jugadores a DTO.</summary>
internal static class Mapper
{
    public static PlayerDto ToDto(this Player p) => new(p.Id, p.Name, p.Position, p.GroupNumber);

    public static PairDto ToDto(this CompetitionEntry e) => new(
        e.Id, e.PairId, e.Pair.DisplayName, e.Pair.Player1.ToDto(), e.Pair.Player2.ToDto(), e.GroupId);

    public static CompetitionSummaryDto ToSummary(this Competition c) => new(
        c.Id, c.Name, c.Type, c.Status, c.PairCount, c.GroupCount, c.PairsPerGroup,
        c.PointsPerWin, c.PointsPerLoss, c.DoubleRound, c.QualifiersPerGroup,
        c.Entries.Count, c.CreatedAtUtc, c.Rounds, c.NextCompetitionId, c.SetsPerMatch, c.ManualRounds);

    public static MatchDto ToDto(this Match m) => new(
        m.Id, m.Phase, m.GroupId, m.Group?.Name, m.Round, m.Order,
        m.HomeEntry?.Id, m.HomeEntry?.Pair.DisplayName,
        m.AwayEntry?.Id, m.AwayEntry?.Pair.DisplayName,
        m.HomeScore, m.AwayScore, m.IsPlayed, m.Winner?.Id,
        m.IsPlayed ? m.Sets.Select(s => new SetScoreDto(s.Home, s.Away)).ToList() : null,
        m.IsPlayed ? m.HomeSets : null, m.IsPlayed ? m.AwaySets : null, m.Ball);

    public static CompetitionDetailDto ToDetailDto(this Competition c) => new(
        c.ToSummary(),
        c.Groups.OrderBy(g => g.Id).Select(g => new GroupDto(g.Id, g.Name,
            c.Type == CompetitionType.RotatingLeague
                ? c.GroupPlayers(g).OrderBy(p => p.Name).Select(p => new PlayerRefDto(p.Id, p.Name)).ToList()
                : null)).ToList(),
        c.Entries.OrderBy(e => e.Id).Select(e => e.ToDto()).ToList(),
        c.Matches.OrderBy(m => m.Phase).ThenBy(m => m.GroupId).ThenBy(m => m.Round).ThenBy(m => m.Order)
            .Select(m => m.ToDto()).ToList());
}
