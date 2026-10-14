namespace Frontenis.Domain.Exceptions;

/// <summary>Recurso inexistente (HTTP 404).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
