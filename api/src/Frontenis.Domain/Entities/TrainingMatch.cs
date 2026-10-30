using Frontenis.Domain.Enums;

namespace Frontenis.Domain.Entities;

/// <summary>Partido suelto de una sesión de entrenamiento: dos parejas libres jugando "a X tantos".</summary>
public sealed class TrainingMatch
{
    private TrainingMatch() { }

    internal TrainingMatch(int order, int home1, int home2, int away1, int away2, int homeScore, int awayScore, BallType ball = BallTypes.Default)
    {
        Order = order;
        Home1Id = home1; Home2Id = home2; Away1Id = away1; Away2Id = away2;
        HomeScore = homeScore; AwayScore = awayScore;
        PlayedAtUtc = DateTime.UtcNow;
        Ball = ball;
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
    /// <summary>Bola con la que se jugó (los partidos anteriores a este dato figuran con la bola por defecto).</summary>
    public BallType Ball { get; private set; } = BallTypes.Default;

    /// <summary>Corrige jugadores, marcador y bola (error al registrar el partido); el orden y la hora no cambian.</summary>
    internal void Correct(int home1, int home2, int away1, int away2, int homeScore, int awayScore, BallType ball)
    {
        Home1Id = home1; Home2Id = home2; Away1Id = away1; Away2Id = away2;
        HomeScore = homeScore; AwayScore = awayScore;
        Ball = ball;
    }

    /// <summary>Indica si ganó la pareja local.</summary>
    public bool HomeWon => HomeScore > AwayScore;
}
