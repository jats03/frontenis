namespace Frontenis.Domain.Interfaces;

/// <summary>Regla que decide cuándo se abren las inscripciones de una franja.</summary>
public interface IOpeningPolicy
{
    /// <summary>Momento (hora local) de apertura para una franja que se celebra en <paramref name="slotDate"/>.</summary>
    DateTime OpensAtFor(DateOnly slotDate);
}
