namespace Frontenis.Domain.Entities;

/// <summary>Partido suelto de una sesión de entrenamiento: dos parejas libres jugando "a X tantos".</summary>
public sealed class TrainingMatch
{
    private TrainingMatch() { }

    internal TrainingMatch(int order, int home1, int home2, int away1, int away2, int homeScore, int awayScore)
    {
        Order = order;
        Home1Id = home1; Home2Id = home2; Away1Id = away1; Away2Id = away2;
        HomeScore = homeScore; AwayScore = awayScore;
        PlayedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int SessionId { get; private set; }
    public int Order { get; private set; }
    public int Home1Id { get; private set; }
    public int Home2Id { get; private set; }
    public int Away1Id { get; private set; }
    public int Away2Id { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public DateTime PlayedAtUtc { get; private set; }

    /// <summary>Indica si ganó la pareja local.</summary>
    public bool HomeWon => HomeScore > AwayScore;
}
