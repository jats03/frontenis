using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;

namespace Frontenis.Domain.Entities;

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
    public const int MaxMatches = 300;
    public IReadOnlyCollection<TrainingMatch> Matches => _matches;

    /// <summary>Crea una sesión validando el nombre y la puntuación por victoria/derrota.</summary>
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

    /// <summary>Cambia el nombre (obligatorio, máximo 150 caracteres).</summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (name.Trim().Length > 150) throw new DomainException("El nombre no puede superar los 150 caracteres.");
        Name = name.Trim();
    }

    /// <summary>Añade un partido 2 contra 2 a la sesión. Si no se indica la bola, se toma la preolímpica.</summary>
    /// <exception cref="DomainException">Jugadores repetidos, marcador no válido, empate, bola no válida o límite de partidos alcanzado.</exception>
    public TrainingMatch AddMatch(int home1, int home2, int away1, int away2, int homeScore, int awayScore, BallType? ball = null)
    {
        ValidatePlayers(home1, home2, away1, away2);
        if (homeScore < 0 || awayScore < 0 || homeScore > 999 || awayScore > 999)
            throw new DomainException("Marcador no válido.");
        if (homeScore == awayScore) throw new DomainException("No puede haber empate.");
        if (ball is { } b && !Enum.IsDefined(b)) throw new DomainException("Tipo de bola no válido.");
        if (_matches.Count >= MaxMatches) throw new DomainException($"La sesión ya tiene el máximo de {MaxMatches} partidos.");
        var order = _matches.Count == 0 ? 1 : _matches.Max(m => m.Order) + 1;
        var match = new TrainingMatch(order, home1, home2, away1, away2, homeScore, awayScore, ball ?? BallTypes.Default);
        _matches.Add(match);
        return match;
    }

    /// <summary>
    /// Cambia los cuatro jugadores de un partido ya guardado (para corregir errores al registrarlo). El marcador, la bola,
    /// el orden y la hora del partido se conservan.
    /// </summary>
    /// <exception cref="DomainException">Faltan jugadores o hay jugadores repetidos.</exception>
    /// <exception cref="NotFoundException">El partido no existe en la sesión.</exception>
    public TrainingMatch UpdateMatchPlayers(int matchId, int home1, int home2, int away1, int away2)
    {
        var match = _matches.FirstOrDefault(m => m.Id == matchId)
            ?? throw new NotFoundException("Partido no encontrado.");
        ValidatePlayers(home1, home2, away1, away2);
        match.ChangePlayers(home1, home2, away1, away2);
        return match;
    }

    private static void ValidatePlayers(int home1, int home2, int away1, int away2)
    {
        var players = new[] { home1, home2, away1, away2 };
        if (players.Any(p => p <= 0)) throw new DomainException("Selecciona los cuatro jugadores.");
        if (players.Distinct().Count() != 4) throw new DomainException("Los cuatro jugadores deben ser distintos.");
    }

    /// <summary>Elimina un partido de la sesión.</summary>
    /// <exception cref="NotFoundException">El partido no existe en la sesión.</exception>
    public void RemoveMatch(int matchId)
    {
        var match = _matches.FirstOrDefault(m => m.Id == matchId)
            ?? throw new NotFoundException("Partido no encontrado.");
        _matches.Remove(match);
    }
}
