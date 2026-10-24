namespace Frontenis.Application.Dtos;

// ---------- Cambios en una liga rotatoria ya sorteada ----------
public sealed record AddRotatingPlayerRequest(int PlayerId, int? GroupId = null);

public sealed record MoveMatchRequest(int Round);

public sealed record SwapRoundsRequest(int RoundA, int RoundB);

public sealed record AddMatchRequest(int Round, int Home1Id, int Home2Id, int Away1Id, int Away2Id);

public sealed record SwapMatchRequest(int OtherMatchId);
