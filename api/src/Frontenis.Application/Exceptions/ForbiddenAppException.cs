namespace Frontenis.Application.Exceptions;

/// <summary>La persona está identificada pero no tiene permiso para esto (se traduce en HTTP 403).</summary>
public sealed class ForbiddenAppException : Exception
{
    public ForbiddenAppException(string message) : base(message) { }
}
