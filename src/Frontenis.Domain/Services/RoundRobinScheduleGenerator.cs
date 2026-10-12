namespace Frontenis.Domain.Services;

/// <summary>Calendario todos contra todos con el método del círculo.</summary>
public sealed class RoundRobinScheduleGenerator : IScheduleGenerator
{
    public IReadOnlyList<ScheduledMatch<T>> Generate<T>(IReadOnlyList<T> participants, bool doubleRound)
    {
        var slots = participants.Select(p => (Has: true, Value: p)).ToList();
        if (slots.Count % 2 == 1) slots.Add((false, default!)); // descanso
        var n = slots.Count;
        var rounds = n - 1;
        var first = new List<ScheduledMatch<T>>();

        for (var r = 0; r < rounds; r++)
        {
            for (var i = 0; i < n / 2; i++)
            {
                var a = slots[i];
                var b = slots[n - 1 - i];
                if (!a.Has || !b.Has) continue;
                // alterna local/visitante para equilibrar
                first.Add(i == 0 && r % 2 == 1 ? new(r + 1, b.Value, a.Value) : new(r + 1, a.Value, b.Value));
            }
            // rota todos menos el primero
            var last = slots[n - 1];
            slots.RemoveAt(n - 1);
            slots.Insert(1, last);
        }

        if (!doubleRound) return first;
        var second = first.Select(m => new ScheduledMatch<T>(m.Round + rounds, m.Away, m.Home));
        return first.Concat(second).ToList();
    }
}
