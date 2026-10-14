namespace Frontenis.Domain.Enums;

/// <summary>
/// League = liga de parejas fijas; Tournament = grupos + fase final;
/// RotatingLeague = liga por niveles donde las parejas cambian en cada jornada (clasificación individual).
/// </summary>
public enum CompetitionType { League = 1, Tournament = 2, RotatingLeague = 3 }
