using Frontenis.Domain.Enums;

namespace Frontenis.Application.Dtos;

public sealed record PlayerRequest(string Name, PlayerPosition Position, int? GroupNumber = null);

public sealed record PlayerDto(int Id, string Name, PlayerPosition Position, int? GroupNumber = null);

public sealed record PairDto(int EntryId, int PairId, string Name, PlayerDto Player1, PlayerDto Player2, int? GroupId);

public sealed record PairingPreviewDto(IReadOnlyList<PlayerPairDto> Pairs, PlayerDto? Unpaired);

public sealed record PlayerPairDto(PlayerDto Player1, PlayerDto Player2);

public sealed record PlayersSelection(IReadOnlyList<int> PlayerIds);

public sealed record ManualPairRequest(int Player1Id, int Player2Id);
