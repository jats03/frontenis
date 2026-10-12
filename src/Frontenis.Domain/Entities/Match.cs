using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

public sealed class Match
{
    private Match() { }

    internal Match(MatchPhase phase, int round, int order)
    {
        Phase = phase;
        Round = round;
        Order = order;
    }

    public int Id { get; private set; }
    public int CompetitionId { get; private set; }
    public MatchPhase Phase { get; private set; }
    public int? GroupId { get; private set; }
    public Group? Group { get; private set; }
    public int Round { get; private set; }
    public int Order { get; private set; }

    public int? HomeEntryId { get; private set; }
    public CompetitionEntry? HomeEntry { get; private set; }
    public int? AwayEntryId { get; private set; }
    public CompetitionEntry? AwayEntry { get; private set; }

    public int? HomeScore { get; private set; }
    public int? AwayScore { get; private set; }

    public int? NextMatchId { get; private set; }
    public Match? NextMatch { get; private set; }
    public BracketSlot? NextSlot { get; private set; }

    public bool IsPlayed => HomeScore.HasValue && AwayScore.HasValue;
    public bool IsReady => HomeEntry is not null && AwayEntry is not null;

    public CompetitionEntry? Winner =>
        !IsPlayed ? null : HomeScore > AwayScore ? HomeEntry : AwayEntry;

    internal static Match ForGroup(Group group, int round, int order, CompetitionEntry home, CompetitionEntry away) =>
        new(MatchPhase.Group, round, order) { Group = group, HomeEntry = home, AwayEntry = away };

    internal static Match ForPlayoff(int round, int order) => new(MatchPhase.Playoff, round, order);

    internal void LinkTo(Match next, BracketSlot slot)
    {
        NextMatch = next;
        NextSlot = slot;
    }

    internal void Place(BracketSlot slot, CompetitionEntry? entry)
    {
        if (slot == BracketSlot.Home) HomeEntry = entry; else AwayEntry = entry;
    }

    internal void SetResult(int homeScore, int awayScore)
    {
        if (!IsReady) throw new DomainException("El partido aún no tiene las dos parejas definidas.");
        if (homeScore < 0 || awayScore < 0) throw new DomainException("Los puntos no pueden ser negativos.");
        if (homeScore == awayScore) throw new DomainException("En frontenis no hay empates: debe haber un ganador.");
        HomeScore = homeScore;
        AwayScore = awayScore;
    }

    internal void ClearResult()
    {
        HomeScore = null;
        AwayScore = null;
    }
}
