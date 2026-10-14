namespace Frontenis.Domain.Interfaces;

/// <summary>Abstracción de aleatoriedad para poder testear sorteos deterministas (DIP).</summary>
public interface IRandomizer
{
    void Shuffle<T>(IList<T> items);
}
