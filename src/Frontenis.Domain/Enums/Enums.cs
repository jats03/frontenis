namespace Frontenis.Domain.Enums;

public enum PlayerPosition { Delantero = 1, Zaguero = 2 }

/// <summary>
/// League = liga de parejas fijas; Tournament = grupos + fase final;
/// RotatingLeague = liga por niveles donde las parejas cambian en cada jornada (clasificación individual).
/// </summary>
public enum CompetitionType { League = 1, Tournament = 2, RotatingLeague = 3 }

public enum CompetitionStatus { Draft = 1, GroupStage = 2, Playoffs = 3, Finished = 4 }

public enum MatchPhase { Group = 1, Playoff = 2 }

public enum BracketSlot { Home = 1, Away = 2 }
