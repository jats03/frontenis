namespace Frontenis.Domain.Exceptions;

/// <summary>Violación de una regla de negocio (HTTP 400).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
