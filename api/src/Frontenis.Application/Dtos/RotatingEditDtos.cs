namespace Frontenis.Application.Dtos;

// ---------- Cambios en una liga rotatoria ya sorteada ----------
public sealed record AddRotatingPlayerRequest(int PlayerId);

public sealed record MoveMatchRequest(int Round);

public sealed record SwapRoundsRequest(int RoundA, int RoundB);

public sealed record SwapMatchRequest(int OtherMatchId);
