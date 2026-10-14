namespace Frontenis.Application.Exceptions;

/// <summary>Credenciales o sesión no válidas (se traduce en HTTP 401).</summary>
public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
