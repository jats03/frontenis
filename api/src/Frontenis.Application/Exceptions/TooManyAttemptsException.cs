namespace Frontenis.Application.Exceptions;

/// <summary>Demasiados intentos fallidos: hay que esperar <see cref="RetryAfter"/>.</summary>
public sealed class TooManyAttemptsException : Exception
{
    public TimeSpan RetryAfter { get; }
    public TooManyAttemptsException(TimeSpan retryAfter)
        : base($"Demasiados intentos. Inténtalo de nuevo en {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minuto(s).")
    {
        RetryAfter = retryAfter;
    }
}
