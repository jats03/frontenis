using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

/// <summary>Partido suelto de una sesión de entrenamiento: dos parejas libres jugando "a X tantos".</summary>
public sealed class TrainingMatch
{
    private TrainingMatch() { }

    internal TrainingMatch(int order, int home1, int home2, int away1, int away2, int target, int homeScore, int awayScore)
    {
        Order = order;
        Home1Id = home1; Home2Id = home2; Away1Id = away1; Away2Id = away2;
        TargetPoints = target; HomeScore = homeScore; AwayScore = awayScore;
        PlayedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int SessionId { get; private set; }
    public int Order { get; private set; }
    public int Home1Id { get; private set; }
    public int Home2Id { get; private set; }
    public int Away1Id { get; private set; }
    public int Away2Id { get; private set; }
    public int TargetPoints { get; private set; }
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public DateTime PlayedAtUtc { get; private set; }

    public bool HomeWon => HomeScore > AwayScore;
}

/// <summary>Sesión de entrenamiento (un día) con sus partidos sueltos y su sistema de puntos.</summary>
public sealed class TrainingSession
{
    private readonly List<TrainingMatch> _matches = new();

    private TrainingSession() { Name = string.Empty; }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public DateTime SessionDate { get; private set; }
    public int PointsPerWin { get; private set; }
    public int PointsPerLoss { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<TrainingMatch> Matches => _matches;

    public static TrainingSession Create(string name, DateTime date, int pointsPerWin, int pointsPerLoss)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (name.Trim().Length > 150) throw new DomainException("El nombre no puede superar los 150 caracteres.");
        if (pointsPerWin < 1) throw new DomainException("Una victoria debe valer al menos 1 punto.");
        if (pointsPerLoss < 0 || pointsPerLoss >= pointsPerWin)
            throw new DomainException("Los puntos por derrota deben ser >= 0 y menores que los de victoria.");
        return new TrainingSession
        {
            Name = name.Trim(), SessionDate = date.Date, PointsPerWin = pointsPerWin,
            PointsPerLoss = pointsPerLoss, CreatedAtUtc = DateTime.UtcNow
        };
    }

    public TrainingMatch AddMatch(int home1, int home2, int away1, int away2, int target, int homeScore, int awayScore)
    {
        var players = new[] { home1, home2, away1, away2 };
        if (players.Any(p => p <= 0)) throw new DomainException("Selecciona los cuatro jugadores.");
        if (players.Distinct().Count() != 4) throw new DomainException("Los cuatro jugadores deben ser distintos.");
        if (target < 1 || target > 999) throw new DomainException("Los tantos del partido deben estar entre 1 y 999.");
        if (homeScore < 0 || awayScore < 0 || homeScore > 999 || awayScore > 999)
            throw new DomainException("Marcador no válido.");
        if (homeScore == awayScore) throw new DomainException("No puede haber empate.");
        var order = _matches.Count == 0 ? 1 : _matches.Max(m => m.Order) + 1;
        var match = new TrainingMatch(order, home1, home2, away1, away2, target, homeScore, awayScore);
        _matches.Add(match);
        return match;
    }

    public void RemoveMatch(int matchId)
    {
        var match = _matches.FirstOrDefault(m => m.Id == matchId)
            ?? throw new NotFoundException("Partido no encontrado.");
        _matches.Remove(match);
    }
}
