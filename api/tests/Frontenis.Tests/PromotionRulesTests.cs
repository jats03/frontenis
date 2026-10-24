using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

/// <summary>
/// Ascensos y descensos con clasificaciones construidas a mano (sin sorteos ni azar). El grupo g tiene jugadores
/// con id g*10+1, g*10+2… en el orden de la clasificación: el primero es el id más bajo y el último el más alto.
/// </summary>
public class PromotionRulesTests
{
    private static IReadOnlyList<IReadOnlyList<StandingRow>> Tables(params int[] groupSizes) =>
        groupSizes.Select((size, g) => (IReadOnlyList<StandingRow>)Enumerable.Range(1, size)
            .Select(rank => new StandingRow(g * 10 + rank, rank, 0, 0, 0, 0, 0, 0, 0)).ToList()).ToList();

    private static IReadOnlyList<IReadOnlyList<int>> Rosters(params int[] groupSizes) =>
        groupSizes.Select((size, g) => (IReadOnlyList<int>)Enumerable.Range(1, size).Select(rank => g * 10 + rank).ToList()).ToList();

    /// <summary>«jugador:origen>destino», para comparar con valores literales.</summary>
    private static string[] Describe(IEnumerable<PlayerMove> moves) =>
        moves.Select(m => $"{m.PlayerId}:{m.FromGroupIndex}>{m.ToGroupIndex}").ToArray();

    public static TheoryData<string, int[], int, string[]> Cases => new()
    {
        { "1 grupo, count 1", new[] { 4 }, 1, Array.Empty<string>() },
        { "1 grupo, count 3", new[] { 4 }, 3, Array.Empty<string>() },
        { "2 grupos, count 1", new[] { 4, 4 }, 1, new[] { "4:0>1", "11:1>0" } },
        { "2 grupos, count 2", new[] { 4, 4 }, 2, new[] { "3:0>1", "4:0>1", "11:1>0", "12:1>0" } },
        { "2 grupos, count 3 (tope 2)", new[] { 4, 4 }, 3, new[] { "3:0>1", "4:0>1", "11:1>0", "12:1>0" } },
        { "3 grupos, count 1", new[] { 4, 4, 4 }, 1, new[] { "4:0>1", "11:1>0", "14:1>2", "21:2>1" } },
        { "3 grupos, count 2", new[] { 4, 4, 4 }, 2,
            new[] { "3:0>1", "4:0>1", "11:1>0", "12:1>0", "13:1>2", "14:1>2", "21:2>1", "22:2>1" } },
        { "4 grupos, count 1", new[] { 4, 4, 4, 4 }, 1,
            new[] { "4:0>1", "11:1>0", "14:1>2", "21:2>1", "24:2>3", "31:3>2" } },
        { "4 grupos, count 3 (tope 2)", new[] { 4, 4, 4, 4 }, 3,
            new[] { "3:0>1", "4:0>1", "11:1>0", "12:1>0", "13:1>2", "14:1>2", "21:2>1", "22:2>1", "23:2>3", "24:2>3", "31:3>2", "32:3>2" } },
        { "grupos de 3, count 2 (tope 1)", new[] { 3, 3 }, 2, new[] { "3:0>1", "11:1>0" } },
        { "grupos de 2, count 1", new[] { 2, 2 }, 1, new[] { "2:0>1", "11:1>0" } },
        { "grupos de 1 no mueven a nadie", new[] { 1, 1, 1 }, 1, Array.Empty<string>() },
        { "grupos de distinto tamaño, count 1", new[] { 5, 3, 2 }, 1, new[] { "5:0>1", "11:1>0", "13:1>2", "21:2>1" } },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Compute_gives_the_expected_moves(string name, int[] groupSizes, int count, string[] expected)
    {
        Assert.NotNull(name);
        Assert.Equal(expected, Describe(PromotionRules.Compute(Tables(groupSizes), count)));
    }

    [Fact]
    public void Default_count_moves_one_player_per_border()
    {
        Assert.Equal(new[] { "4:0>1", "11:1>0" }, Describe(PromotionRules.Compute(Tables(4, 4))));
    }

    [Fact]
    public void Groups_of_four_with_count_three_only_move_two_players_per_border()
    {
        var moves = PromotionRules.Compute(Tables(4, 4), 3);

        Assert.Equal(2, moves.Count(m => m.IsPromotion));
        Assert.Equal(2, moves.Count(m => !m.IsPromotion));
        Assert.Equal(4, moves.Count);
    }

    [Fact]
    public void Top_group_never_promotes_and_bottom_group_never_relegates()
    {
        var moves = PromotionRules.Compute(Tables(4, 4, 4), 2);

        Assert.DoesNotContain(moves, m => m.FromGroupIndex == 0 && m.IsPromotion);
        Assert.DoesNotContain(moves, m => m.FromGroupIndex == 2 && !m.IsPromotion);
    }

    [Fact]
    public void No_groups_means_no_moves()
    {
        Assert.Empty(PromotionRules.Compute(Array.Empty<IReadOnlyList<StandingRow>>()));
    }

    [Fact]
    public void NextRosters_with_crossed_moves_in_three_groups()
    {
        var rosters = Rosters(4, 4, 4);
        var moves = PromotionRules.Compute(Tables(4, 4, 4));

        var next = PromotionRules.NextRosters(rosters, moves);

        Assert.Equal(new[] { 1, 2, 3, 11 }, next[0]);
        Assert.Equal(new[] { 12, 13, 4, 21 }, next[1]);
        Assert.Equal(new[] { 22, 23, 24, 14 }, next[2]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NextRosters_keeps_group_sizes_and_never_duplicates_or_loses_a_player(int count)
    {
        var rosters = Rosters(4, 4, 4, 4);
        var next = PromotionRules.NextRosters(rosters, PromotionRules.Compute(Tables(4, 4, 4, 4), count));

        Assert.Equal(new[] { 4, 4, 4, 4 }, next.Select(g => g.Count).ToArray());
        Assert.Equal(rosters.SelectMany(g => g).OrderBy(x => x), next.SelectMany(g => g).OrderBy(x => x));
    }

    [Fact]
    public void NextRosters_without_moves_keeps_every_group_as_it_was()
    {
        var rosters = Rosters(3, 3);
        Assert.Equal(rosters, PromotionRules.NextRosters(rosters, Array.Empty<PlayerMove>()));
    }

    [Fact]
    public void A_single_group_stays_the_same()
    {
        var rosters = Rosters(4);
        var next = PromotionRules.NextRosters(rosters, PromotionRules.Compute(Tables(4), 2));
        Assert.Equal(rosters, next);
    }
}
