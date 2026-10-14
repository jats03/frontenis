namespace Frontenis.Domain.Models;

// ---------- Calendario ----------
public sealed record ScheduledMatch<T>(int Round, T Home, T Away);
