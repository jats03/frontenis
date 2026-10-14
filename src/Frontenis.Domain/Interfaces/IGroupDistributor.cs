namespace Frontenis.Domain.Interfaces;

// ---------- Sorteo de grupos ----------
public interface IGroupDistributor
{
    IReadOnlyList<IReadOnlyList<T>> Distribute<T>(IReadOnlyList<T> items, int groupCount);
}
