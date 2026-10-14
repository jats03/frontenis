namespace Frontenis.Domain.Services;

/// <summary>Cálculos de la liga rotatoria.</summary>
public static class RotationMath
{
    /// <summary>
    /// Jornadas necesarias para que todos los jugadores de un grupo jueguen con todos como compañeros:
    /// con n múltiplo de 4 son n-1; si en cada jornada descansan jugadores hacen falta algunas más.
    /// </summary>
    public static int FullRounds(int players)
    {
        var pairsPerRound = 2 * (players / 4);
        var allPairs = players * (players - 1) / 2;
        return (int)Math.Ceiling(allPairs / (double)pairsPerRound);
    }
}
