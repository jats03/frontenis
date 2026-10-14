using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

// ---------- Histórico de jugadores ----------
public sealed record PlayerHistoryRowDto(
    int PlayerId, string Name, PlayerPosition Position, int Played, int Won, int Lost, double WinRate,
    int SetsFor, int SetsAgainst, int TantosFor, int TantosAgainst, int TantosDiff,
    int ClassificationPoints, int Competitions);

public sealed record RelationDto(
    int PlayerId, string Name, int Played, int Won, int Lost, double WinRate, int TantosFor, int TantosAgainst);

public sealed record RecentMatchDto(
    int MatchId, int CompetitionId, string CompetitionName, string Partner, string Rivals,
    IReadOnlyList<SetScoreDto> Sets, bool Won, bool IsTraining = false);

public sealed record PlayerHistoryDto(
    PlayerHistoryRowDto Summary,
    IReadOnlyList<RelationDto> Partners,
    IReadOnlyList<RelationDto> Rivals,
    IReadOnlyList<RecentMatchDto> Recent);
