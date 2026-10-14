namespace Frontenis.Application.Dtos;

public sealed record RenameTrainingRequest(string Name);

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
