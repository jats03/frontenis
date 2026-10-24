namespace Frontenis.Application.Exceptions;

/// <summary>La operación choca con el estado actual de los datos (se traduce en HTTP 409).</summary>
public sealed class ConflictException : Exception
{
    /// <summary>Tipo de conflicto para el log (p. ej. "registro duplicado"); nunca datos personales.</summary>
    public string ConflictType { get; }

    public ConflictException(string message, string conflictType = "estado", Exception? inner = null)
        : base(message, inner)
    {
        ConflictType = conflictType;
    }
}
