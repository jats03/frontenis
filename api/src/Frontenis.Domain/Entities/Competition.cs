using Frontenis.Domain.Enums;
using Frontenis.Domain.Exceptions;
using Frontenis.Domain.Models;

namespace Frontenis.Domain.Entities;

/// <summary>Aggregate root: liga o torneo con sus grupos, parejas inscritas y partidos.</summary>
public sealed partial class Competition
{
    private readonly List<Group> _groups = new();
    private readonly List<CompetitionEntry> _entries = new();
    private readonly List<Match> _matches = new();

    private Competition() { Name = string.Empty; }

    public int Id { get; private set; }
    public string Name { get; private set; }
    public CompetitionType Type { get; private set; }
    public CompetitionStatus Status { get; private set; }
    public int PairCount { get; private set; }
    public int GroupCount { get; private set; }
    public int PairsPerGroup { get; private set; }
    public int PointsPerWin { get; private set; }
    public int PointsPerLoss { get; private set; }
    public bool DoubleRound { get; private set; }
    public int QualifiersPerGroup { get; private set; }
    public int Rounds { get; private set; }
    public int SetsPerMatch { get; private set; } = 1;
    public int? NextCompetitionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Group> Groups => _groups;
    public IReadOnlyCollection<CompetitionEntry> Entries => _entries;
    public IReadOnlyCollection<Match> Matches => _matches;

    /// <summary>Crea una competición en estado <c>Draft</c> validando formato, grupos, clasificados y puntuación.</summary>
    /// <exception cref="DomainException">La configuración no es válida.</exception>
    public static Competition Create(CompetitionSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.Name)) throw new DomainException("El nombre es obligatorio.");
        if (s.Type == CompetitionType.RotatingLeague) return CreateRotating(s);
        ValidateSetsPerMatch(s.SetsPerMatch);
        if (s.PairCount < 2) throw new DomainException("Se necesitan al menos 2 parejas.");
        if (s.GroupCount < 1) throw new DomainException("Debe haber al menos 1 grupo.");
        if (s.PairCount / s.GroupCount < 2)
            throw new DomainException("Cada grupo necesita al menos 2 parejas.");
        if (s.PairsPerGroup < 0) throw new DomainException("Parejas por grupo no válido.");
        if (s.PairsPerGroup > 0 && s.PairsPerGroup * s.GroupCount != s.PairCount)
            throw new DomainException("Grupos × parejas por grupo debe ser igual al número de parejas.");
        ValidatePoints(s);

        var perGroup = s.PairsPerGroup > 0 ? s.PairsPerGroup : (int)Math.Ceiling(s.PairCount / (double)s.GroupCount);
        var minGroupSize = s.PairCount / s.GroupCount;
        var qualifiers = 0;
        if (s.Type == CompetitionType.Tournament)
        {
            qualifiers = s.QualifiersPerGroup;
            if (qualifiers < 1 || qualifiers > minGroupSize)
                throw new DomainException($"Los clasificados por grupo deben estar entre 1 y {minGroupSize}.");
            if (qualifiers * s.GroupCount < 2)
                throw new DomainException("La fase final necesita al menos 2 clasificados en total.");
        }

        return new Competition
        {
            Name = s.Name.Trim(),
            Type = s.Type,
            Status = CompetitionStatus.Draft,
            PairCount = s.PairCount,
            GroupCount = s.GroupCount,
            PairsPerGroup = perGroup,
            PointsPerWin = s.PointsPerWin,
            PointsPerLoss = s.PointsPerLoss,
            DoubleRound = s.DoubleRound,
            QualifiersPerGroup = qualifiers,
            SetsPerMatch = s.SetsPerMatch,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Competition CreateRotating(CompetitionSettings s)
    {
        ValidateSetsPerMatch(s.SetsPerMatch);
        if (s.GroupCount < 1) throw new DomainException("Debe haber al menos 1 grupo.");
        var perGroup = s.PairsPerGroup; // mínimo de jugadores por grupo
        if (perGroup < 4)
            throw new DomainException("Cada grupo necesita al menos 4 jugadores.");
        ValidatePoints(s);
        if (s.Rounds < 0) throw new DomainException("Las jornadas no pueden ser negativas.");

        return new Competition
        {
            Name = s.Name.Trim(),
            Type = CompetitionType.RotatingLeague,
            Status = CompetitionStatus.Draft,
            PairCount = s.GroupCount * perGroup,
            GroupCount = s.GroupCount,
            PairsPerGroup = perGroup,
            PointsPerWin = s.PointsPerWin,
            PointsPerLoss = s.PointsPerLoss,
            DoubleRound = false,
            QualifiersPerGroup = 0,
            Rounds = s.Rounds, // 0 = todos con todos (automático, según el tamaño de cada grupo)
            SetsPerMatch = s.SetsPerMatch,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static void ValidatePoints(CompetitionSettings s)
    {
        if (s.PointsPerWin < 1) throw new DomainException("Una victoria debe valer al menos 1 punto.");
        if (s.PointsPerLoss < 0 || s.PointsPerLoss >= s.PointsPerWin)
            throw new DomainException("Los puntos por derrota deben ser >= 0 y menores que los de victoria.");
    }

    private static void ValidateSetsPerMatch(int sets)
    {
        if (sets is < 1 or > 2) throw new DomainException("Los sets por partido deben ser 1 o 2.");
    }

    /// <summary>Cambia 1 ↔ 2 sets por partido; solo si aún no se ha jugado ningún partido.</summary>
    public void ChangeSetsPerMatch(int sets)
    {
        ValidateSetsPerMatch(sets);
        if (_matches.Any(m => m.IsPlayed))
            throw new DomainException("No se puede cambiar el formato: ya hay partidos jugados.");
        SetsPerMatch = sets;
    }

    public bool IsRotating => Type == CompetitionType.RotatingLeague;

    /// <summary>Cambia el nombre (obligatorio, máximo 150 caracteres).</summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        var trimmed = name.Trim();
        if (trimmed.Length > 150) throw new DomainException("El nombre no puede superar los 150 caracteres.");
        Name = trimmed;
    }

    /// <summary>Inscribe una pareja mientras la competición está en inscripción y quedan plazas.</summary>
    /// <exception cref="DomainException">Estado incorrecto, competición completa o jugador ya inscrito.</exception>
    public CompetitionEntry AddPair(Pair pair)
    {
        if (IsRotating) throw new DomainException("En una liga rotatoria se inscriben jugadores sueltos, no parejas.");
        if (Status != CompetitionStatus.Draft) throw new DomainException("Ya no se pueden inscribir parejas.");
        if (_entries.Count >= PairCount) throw new DomainException("La competición ya tiene todas sus parejas.");
        var clash = _entries.Any(e => e.Pair.Contains(pair.Player1Id) || e.Pair.Contains(pair.Player2Id));
        if (clash && pair.Player1Id != 0) throw new DomainException("Algún jugador ya está inscrito en esta competición.");
        var entry = new CompetitionEntry(pair);
        _entries.Add(entry);
        return entry;
    }

    private void RefreshStatus()
    {
        if (Status == CompetitionStatus.Draft) return;
        if (Type != CompetitionType.Tournament)
        {
            Status = _matches.All(m => m.IsPlayed) ? CompetitionStatus.Finished : CompetitionStatus.GroupStage;
            return;
        }
        if (!HasPlayoffs) { Status = CompetitionStatus.GroupStage; return; }
        var final = _matches.Where(m => m.Phase == MatchPhase.Playoff).OrderByDescending(m => m.Round).First();
        Status = final.IsPlayed ? CompetitionStatus.Finished : CompetitionStatus.Playoffs;
    }

    private static string GroupName(int index) =>
        index < 26 ? $"Grupo {(char)('A' + index)}" : $"Grupo {index + 1}";
}
