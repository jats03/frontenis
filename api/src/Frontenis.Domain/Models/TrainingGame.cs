namespace Frontenis.Domain.Models;

/// <summary>Partido de entrenamiento reducido a lo necesario para calcular (ordenado cronológicamente).</summary>
public sealed record TrainingGame(int Home1, int Home2, int Away1, int Away2, int HomeScore, int AwayScore, int Target)
{
    public bool HomeWon => HomeScore > AwayScore;
}
