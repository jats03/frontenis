using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

// ---------- Resultados enviados por los jugadores (pendientes de aprobación) ----------
public sealed record SubmitResultRequest(IReadOnlyList<SetScoreDto> Sets, BallType? Ball = null);

public sealed record MatchSubmissionDto(
    int Id, int MatchId, int CompetitionId, string CompetitionName, string? HomeName, string? AwayName,
    int PlayerId, string PlayerName, IReadOnlyList<SetScoreDto> Sets, DateTime SubmittedAtUtc,
    BallType Ball = BallTypes.Default);

public sealed record RejectedResultDto(
    int Id, int MatchId, int CompetitionId, string? HomeName, string? AwayName,
    int PlayerId, string PlayerName, IReadOnlyList<SetScoreDto> Sets, DateTime RejectedAtUtc,
    BallType Ball = BallTypes.Default);
