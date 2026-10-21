namespace Frontenis.Application.Dtos;

// ---------- Resultados enviados por los jugadores (pendientes de aprobación) ----------
public sealed record SubmitResultRequest(IReadOnlyList<SetScoreDto> Sets);

public sealed record MatchSubmissionDto(
    int Id, int MatchId, int CompetitionId, string CompetitionName, string? HomeName, string? AwayName,
    int PlayerId, string PlayerName, IReadOnlyList<SetScoreDto> Sets, DateTime SubmittedAtUtc);
