using Frontenis.Domain.Entities;

namespace Frontenis.Domain.Models;

// ---------- Liga con rotación de parejas ----------
public sealed record PlannedMatch((Player First, Player Second) Home, (Player First, Player Second) Away);
