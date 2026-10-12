using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);

public sealed record PlayerRequest(string Name, PlayerPosition Position);
public sealed record PlayerDto(int Id, string Name, PlayerPosition Position);

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
    int Rounds = 0);

public sealed record CompetitionSummaryDto(
    int Id, string Name, CompetitionType Type, CompetitionStatus Status,
    int PairCount, int GroupCount, int PairsPerGroup, int PointsPerWin, int PointsPerLoss,
    bool DoubleRound, int QualifiersPerGroup, int RegisteredPairs, DateTime CreatedAtUtc,
    int Rounds = 0, int? NextCompetitionId = null);

public sealed record GroupDto(int Id, string Name);

public sealed record MatchDto(
    int Id, MatchPhase Phase, int? GroupId, string? GroupName, int Round, int Order,
    int? HomeEntryId, string? HomeName, int? AwayEntryId, string? AwayName,
    int? HomeScore, int? AwayScore, bool IsPlayed, int? WinnerEntryId);

public sealed record CompetitionDetailDto(
    CompetitionSummaryDto Summary,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<PairDto> Pairs,
    IReadOnlyList<MatchDto> Matches);

public sealed record SetResultRequest(int HomeScore, int AwayScore);

public sealed record StandingDto(
    int Rank, int EntryId, string PairName, int Played, int Won, int Lost,
    int PointsFor, int PointsAgainst, int PointsDiff, int ClassificationPoints, bool Qualifies, int Movement = 0);
public sealed record GroupStandingsDto(int GroupId, string GroupName, IReadOnlyList<StandingDto> Rows);
