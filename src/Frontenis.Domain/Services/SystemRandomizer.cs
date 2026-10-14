using Frontenis.Domain.Interfaces;

namespace Frontenis.Domain.Services;

public sealed class SystemRandomizer : IRandomizer
{
    private readonly Random _random;
    public SystemRandomizer() : this(new Random()) { }
    public SystemRandomizer(Random random) { _random = random; }

    public void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
