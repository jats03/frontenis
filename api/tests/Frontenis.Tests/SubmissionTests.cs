using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;
using Frontenis.Domain.Services;
using Xunit;

namespace Frontenis.Tests;

public class SubmissionTests
{
    private static Competition Started()
    {
        var c = Competition.Create(new CompetitionSettings("Liga", CompetitionType.RotatingLeague, 0, 1, 4, 2, 0, false, 0));
        var players = Enumerable.Range(1, 4).Select(i => TestHelpers.Player(i, $"J{i}", i % 2 == 0 ? PlayerPosition.Delantero : PlayerPosition.Zaguero)).ToList();
        c.DrawRotating(players, new SystemRandomizer(new Random(3)), new RotationPlanner(new SystemRandomizer(new Random(3))));
        var gid = 1; foreach (var g in c.Groups) g.WithId(gid++);
        var mid = 1; foreach (var m in c.Matches) m.WithId(mid++);
        return c;
    }
    private static SetScore[] Win() => new[] { new SetScore(10, 5) };

    [Fact] public void Valid_submission_is_accepted() { var c = Started(); Assert.NotNull(c.EnsureSubmittable(c.Matches.First().Id, Win())); }

    [Fact] public void Played_match_cannot_receive_submission()
    {
        var c = Started(); var m = c.Matches.First(); c.RecordResult(m.Id, 10, 5);
        Assert.Throws<DomainException>(() => c.EnsureSubmittable(m.Id, Win()));
    }

    [Fact] public void Invalid_sets_are_rejected() { var c = Started(); Assert.Throws<DomainException>(() => c.EnsureSubmittable(c.Matches.First().Id, new[] { new SetScore(-1, 5) })); }

    [Fact] public void Unknown_match_is_rejected() { var c = Started(); Assert.ThrowsAny<Exception>(() => c.EnsureSubmittable(999, Win())); }

    [Fact] public void Only_the_sender_can_modify()
    {
        var s = MatchSubmission.Create(1, 7, Win());
        s.Replace(7, new[] { new SetScore(10, 8) });
        Assert.Equal(8, s.Sets[0].Away);
        Assert.Throws<DomainException>(() => s.Replace(8, Win()));
    }

    [Fact] public void Submission_roundtrips_three_sets()
    {
        var s = MatchSubmission.Create(1, 7, new[] { new SetScore(10, 5), new SetScore(3, 10), new SetScore(10, 9) });
        Assert.Equal(3, s.Sets.Count);
        Assert.Equal(9, s.Sets[2].Away);
    }

    [Fact] public void Approved_sets_are_recorded()
    {
        var c = Started(); var m = c.Matches.First();
        var s = MatchSubmission.Create(m.Id, 1, Win());
        c.RecordResult(m.Id, s.Sets);
        Assert.True(m.IsPlayed);
    }
}
