using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

public sealed record LoginRequest(string Username, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);

/// <summary>Grupos formados a mano: ids de inscripción (parejas) o de jugadores (liga rotatoria) por grupo.</summary>
public sealed record ManualDrawRequest(IReadOnlyList<IReadOnlyList<int>> Groups);
public sealed record RenameCompetitionRequest(string Name);
public sealed record PlayerRequest(string Name, PlayerPosition Position, int? GroupNumber = null);
public sealed record PlayerDto(int Id, string Name, PlayerPosition Position, int? GroupNumber = null);

public sealed record PairDto(int EntryId, int PairId, string Name, PlayerDto Player1, PlayerDto Player2, int? GroupId);
public sealed record PairingPreviewDto(IReadOnlyList<PlayerPairDto> Pairs, PlayerDto? Unpaired);
public sealed record PlayerPairDto(PlayerDto Player1, PlayerDto Player2);
public sealed record PlayersSelection(IReadOnlyList<int> PlayerIds);
public sealed record ManualPairRequest(int Player1Id, int Player2Id);

public sealed record CreateCompetitionRequest(
    string Name,
    CompetitionType Type,
    int PairCount,
    int GroupCount,
    int PairsPerGroup,
    int PointsPerWin,
    int PointsPerLoss,
    bool DoubleRound,
    int QualifiersPerGroup,
    int Rounds = 0,
    int SetsPerMatch = 1);

public sealed record CompetitionSummaryDto(
    int Id, string Name, CompetitionType Type, CompetitionStatus Status,
    int PairCount, int GroupCount, int PairsPerGroup, int PointsPerWin, int PointsPerLoss,
    bool DoubleRound, int QualifiersPerGroup, int RegisteredPairs, DateTime CreatedAtUtc,
    int Rounds = 0, int? NextCompetitionId = null, int SetsPerMatch = 1);

public sealed record SetsPerMatchRequest(int SetsPerMatch);
public sealed record SetScoreDto(int Home, int Away);

public sealed record GroupDto(int Id, string Name);

public sealed record MatchDto(
    int Id, MatchPhase Phase, int? GroupId, string? GroupName, int Round, int Order,
    int? HomeEntryId, string? HomeName, int? AwayEntryId, string? AwayName,
    int? HomeScore, int? AwayScore, bool IsPlayed, int? WinnerEntryId,
    IReadOnlyList<SetScoreDto>? Sets = null, int? HomeSets = null, int? AwaySets = null);

public sealed record CompetitionDetailDto(
    CompetitionSummaryDto Summary,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<PairDto> Pairs,
    IReadOnlyList<MatchDto> Matches);

/// <summary>Resultado: lista de sets (preferido) o, por compatibilidad, un único marcador Home/Away.</summary>
public sealed record SetResultRequest(int HomeScore = 0, int AwayScore = 0, IReadOnlyList<SetScoreDto>? Sets = null);

public sealed record StandingDto(
    int Rank, int EntryId, string PairName, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int PointsDiff, int ClassificationPoints, bool Qualifies, int Movement = 0);
public sealed record GroupStandingsDto(int GroupId, string GroupName, IReadOnlyList<StandingDto> Rows);

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

// ---------- Entrenamientos (partidos sueltos) ----------
public sealed record CreateTrainingRequest(string Name, DateTime Date, int PointsPerWin = 3, int PointsPerLoss = 0);
public sealed record AddTrainingMatchRequest(
    int Home1Id, int Home2Id, int Away1Id, int Away2Id, int TargetPoints, int HomeScore, int AwayScore);
public sealed record TrainingSuggestRequest(IReadOnlyList<int> PlayerIds, int Count = 5);

public sealed record TrainingSummaryDto(
    int Id, string Name, DateTime Date, int PointsPerWin, int PointsPerLoss, int MatchCount);

public sealed record TrainingMatchDto(
    int Id, int Order, string HomeName, string AwayName, int[] HomePlayerIds, int[] AwayPlayerIds,
    int TargetPoints, int HomeScore, int AwayScore, bool HomeWon);

public sealed record TrainingRowDto(
    int Rank, string Name, int[] PlayerIds, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int PointsDiff, int ClassificationPoints);

public sealed record TrainingDetailDto(
    TrainingSummaryDto Summary,
    IReadOnlyList<TrainingMatchDto> Matches,
    IReadOnlyList<TrainingRowDto> PairStandings,
    IReadOnlyList<TrainingRowDto> PlayerStandings);

public sealed record TrainingSuggestionDto(
    int[] HomePlayerIds, int[] AwayPlayerIds, string HomeName, string AwayName,
    double HomeLevel, double AwayLevel, int Balance, string Reason);
