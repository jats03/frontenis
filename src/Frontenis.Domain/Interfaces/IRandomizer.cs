namespace Frontenis.Domain.Interfaces;

/// <summary>Abstracción de aleatoriedad para poder testear sorteos deterministas (DIP).</summary>
public interface IRandomizer
{
    /// <summary>Mezcla la lista en el sitio.</summary>
    void Shuffle<T>(IList<T> items);
}
