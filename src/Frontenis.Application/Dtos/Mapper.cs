using Frontenis.Domain.Entities;

namespace Frontenis.Application.Dtos;

internal static class Mapper
{
    public static PlayerDto ToDto(this Player p) => new(p.Id, p.Name, p.Position);

    public static PairDto ToDto(this CompetitionEntry e) => new(
        e.Id, e.PairId, e.Pair.DisplayName, e.Pair.Player1.ToDto(), e.Pair.Player2.ToDto(), e.GroupId);

    public static CompetitionSummaryDto ToSummary(this Competition c) => new(
        c.Id, c.Name, c.Type, c.Status, c.PairCount, c.GroupCount, c.PairsPerGroup,
        c.PointsPerWin, c.PointsPerLoss, c.DoubleRound, c.QualifiersPerGroup,
        c.Entries.Count, c.CreatedAtUtc, c.Rounds, c.NextCompetitionId);

    public static MatchDto ToDto(this Match m) => new(
        m.Id, m.Phase, m.GroupId, m.Group?.Name, m.Round, m.Order,
        m.HomeEntry?.Id, m.HomeEntry?.Pair.DisplayName,
        m.AwayEntry?.Id, m.AwayEntry?.Pair.DisplayName,
        m.HomeScore, m.AwayScore, m.IsPlayed, m.Winner?.Id);
}
