namespace Frontenis.Domain.Enums;

/// <summary>Tipo de bola con la que se juega un partido (de entrenamiento o de competición).</summary>
public enum BallType
{
    /// <summary>Bola lenta.</summary>
    Slow = 1,
    /// <summary>Bola preolímpica.</summary>
    PreOlympic = 2,
    /// <summary>Bola olímpica.</summary>
    Olympic = 3,
}

/// <summary>Bola que se toma cuando no se indica ninguna.</summary>
public static class BallTypes
{
    public const BallType Default = BallType.PreOlympic;
}
