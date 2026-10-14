using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

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

public sealed record RenameCompetitionRequest(string Name);

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

/// <summary>Grupos formados a mano: ids de inscripción (parejas) o de jugadores (liga rotatoria) por grupo.</summary>
public sealed record ManualDrawRequest(IReadOnlyList<IReadOnlyList<int>> Groups);
