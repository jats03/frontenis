using Frontenis.Domain.Enums;

namespace Frontenis.Domain.Models;

/// <summary>Parámetros configurables de una competición.</summary>
public sealed record CompetitionSettings(
    string Name,
    CompetitionType Type,
    int PairCount,
    int GroupCount,
    int PairsPerGroup,          // 0 = calcular automáticamente
    int PointsPerWin,
    int PointsPerLoss,
    bool DoubleRound,
    int QualifiersPerGroup,     // solo torneos (fase final)
    int Rounds = 0,             // solo ligas rotatorias (0 = automático)
    int SetsPerMatch = 1,       // 1 o 2 sets por partido (con 2, un 3.º de desempate)
    bool ManualRounds = false); // solo ligas rotatorias: no se generan partidos, las jornadas se crean a mano
