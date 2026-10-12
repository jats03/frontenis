using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Services;

/// <summary>Abstracción de aleatoriedad para poder testear sorteos deterministas (DIP).</summary>
public interface IRandomizer
{
    void Shuffle<T>(IList<T> items);
}

public sealed class SystemRandomizer : IRandomizer
{
    private readonly Random _random;
    public SystemRandomizer() : this(new Random()) { }
    public SystemRandomizer(Random random) { _random = random; }

    public void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}

// ---------- Emparejamiento de jugadores ----------
public sealed record PairingResult(IReadOnlyList<(Player First, Player Second)> Pairs, Player? Unpaired);

public interface IPairingStrategy
{
    PairingResult Generate(IReadOnlyList<Player> players);
}

// ---------- Sorteo de grupos ----------
public interface IGroupDistributor
{
    IReadOnlyList<IReadOnlyList<T>> Distribute<T>(IReadOnlyList<T> items, int groupCount);
}

// ---------- Calendario ----------
public sealed record ScheduledMatch<T>(int Round, T Home, T Away);

public interface IScheduleGenerator
{
    IReadOnlyList<ScheduledMatch<T>> Generate<T>(IReadOnlyList<T> participants, bool doubleRound);
}

// ---------- Clasificación ----------
public sealed record MatchOutcome(int HomeId, int AwayId, int HomeScore, int AwayScore);

public sealed record StandingRow(
    int EntryId, int Rank, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int PointsDiff, int ClassificationPoints);

public interface IStandingsCalculator
{
    IReadOnlyList<StandingRow> Calculate(
        IReadOnlyList<int> entryIds, IEnumerable<MatchOutcome> outcomes, int pointsPerWin, int pointsPerLoss);
}

// ---------- Cuadro de eliminatorias ----------
public sealed record BracketNode(int Round, int Position, int? NextRound, int? NextPosition, Frontenis.Domain.Enums.BracketSlot? NextSlot);
public sealed record BracketPlacement(int Round, int Position, Frontenis.Domain.Enums.BracketSlot Slot, int Seed);
public sealed record BracketPlan(IReadOnlyList<BracketNode> Nodes, IReadOnlyList<BracketPlacement> Placements);

public interface IBracketGenerator
{
    BracketPlan Generate(int participantCount);
}
