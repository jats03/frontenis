using Frontenis.Domain.Enums;

namespace Frontenis.Domain.Entities;

/// <summary>
/// Copia de un resultado enviado por un jugador que el administrador rechazó. Se conserva aparte para poder
/// recuperarlo si se rechazó sin querer.
/// </summary>
public sealed class RejectedSubmission
{
    private RejectedSubmission() { }

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int CompetitionId { get; private set; }
    public int PlayerId { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime RejectedAtUtc { get; private set; }
    public BallType Ball { get; private set; } = BallTypes.Default;

    public int Set1Home { get; private set; }
    public int Set1Away { get; private set; }
    public int? Set2Home { get; private set; }
    public int? Set2Away { get; private set; }
    public int? Set3Home { get; private set; }
    public int? Set3Away { get; private set; }

    /// <summary>Marcador de cada set del envío rechazado.</summary>
    public IReadOnlyList<SetScore> Sets
    {
        get
        {
            var list = new List<SetScore> { new(Set1Home, Set1Away) };
            if (Set2Home.HasValue && Set2Away.HasValue) list.Add(new SetScore(Set2Home.Value, Set2Away.Value));
            if (Set3Home.HasValue && Set3Away.HasValue) list.Add(new SetScore(Set3Home.Value, Set3Away.Value));
            return list;
        }
    }

    /// <summary>Guarda una copia del envío que se va a rechazar.</summary>
    public static RejectedSubmission From(MatchSubmission s, int competitionId, DateTime rejectedAtUtc)
    {
        var sets = s.Sets;
        return new RejectedSubmission
        {
            MatchId = s.MatchId, CompetitionId = competitionId, PlayerId = s.PlayerId,
            SubmittedAtUtc = s.SubmittedAtUtc, RejectedAtUtc = rejectedAtUtc, Ball = s.Ball,
            Set1Home = sets[0].Home, Set1Away = sets[0].Away,
            Set2Home = sets.Count > 1 ? sets[1].Home : null, Set2Away = sets.Count > 1 ? sets[1].Away : null,
            Set3Home = sets.Count > 2 ? sets[2].Home : null, Set3Away = sets.Count > 2 ? sets[2].Away : null,
        };
    }
}
