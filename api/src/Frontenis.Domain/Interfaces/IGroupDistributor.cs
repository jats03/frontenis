namespace Frontenis.Domain.Interfaces;

// ---------- Sorteo de grupos ----------
/// <summary>Estrategia de reparto de participantes en grupos.</summary>
public interface IGroupDistributor
{
    /// <summary>Reparte los elementos en <paramref name="groupCount"/> grupos del mismo tamaño (o lo más parecido posible).</summary>
    IReadOnlyList<IReadOnlyList<T>> Distribute<T>(IReadOnlyList<T> items, int groupCount);
}
