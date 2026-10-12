namespace Frontenis.Domain.Services;

/// <summary>Sorteo aleatorio repartiendo uno a uno para que los grupos queden equilibrados.</summary>
public sealed class ShuffleGroupDistributor : IGroupDistributor
{
    private readonly IRandomizer _randomizer;

    public ShuffleGroupDistributor(IRandomizer randomizer) { _randomizer = randomizer; }

    public IReadOnlyList<IReadOnlyList<T>> Distribute<T>(IReadOnlyList<T> items, int groupCount)
    {
        var shuffled = items.ToList();
        _randomizer.Shuffle(shuffled);
        var groups = Enumerable.Range(0, groupCount).Select(_ => new List<T>()).ToList();
        for (var i = 0; i < shuffled.Count; i++) groups[i % groupCount].Add(shuffled[i]);
        return groups.Select(g => (IReadOnlyList<T>)g).ToList();
    }
}
