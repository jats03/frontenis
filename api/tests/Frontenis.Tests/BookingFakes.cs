using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Security;
using Frontenis.Application.Models;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Enums;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;

namespace Frontenis.Tests;

// Dobles de prueba en memoria compartidos por las pruebas de reservas.


internal sealed class FakeClock : IClock { public DateTime Now { get; set; } }

internal sealed class FakeUow : IUnitOfWork
{
    public int Saves { get; private set; }
    public Task SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
}

internal sealed class FakeSlots : ISlotRepository
{
    private int _next = 1;
    public List<TrainingSlot> Items { get; } = new();

    public Task<IReadOnlyList<TrainingSlot>> ListAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<TrainingSlot>>(Items.Where(s => s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date).ThenBy(s => s.Start).ToList());
    public Task<IReadOnlyList<TrainingSlot>> ListForUpdateAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ListAsync(from, to, ct);
    public Task<TrainingSlot?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));
    public Task<IReadOnlyCollection<GeneratedSlotKey>> GeneratedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyCollection<GeneratedSlotKey>>(Items
            .Where(s => s.TemplateId != null && s.Date >= Weeks.MondayOf(from) && s.Date <= to)
            .Select(s => new GeneratedSlotKey(s.TemplateId!.Value, Weeks.MondayOf(s.Date))).ToList());
    public Task AddAsync(TrainingSlot slot, CancellationToken ct = default) { slot.WithId(_next++); Items.Add(slot); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<TrainingSlot> slots, CancellationToken ct = default)
    {
        foreach (var s in slots) { s.WithId(_next++); Items.Add(s); }
        return Task.CompletedTask;
    }
    public void Remove(TrainingSlot slot) => Items.Remove(slot);
}

internal sealed class FakeTemplates : ISlotTemplateRepository
{
    public List<SlotTemplate> Items { get; } = new();
    public Task<IReadOnlyList<SlotTemplate>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SlotTemplate>>(Items.ToList());
    public Task<SlotTemplate?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));
    public Task AddAsync(SlotTemplate template, CancellationToken ct = default) { template.WithId(Items.Count + 1); Items.Add(template); return Task.CompletedTask; }
    public void Remove(SlotTemplate template) => Items.Remove(template);
}

internal sealed class FakePlayers : IPlayerRepository
{
    public List<Player> Items { get; } = new();
    public Task<IReadOnlyList<Player>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Player>>(Items.ToList());
    public Task<IReadOnlyList<Player>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Player>>(Items.Where(p => ids.Contains(p.Id)).ToList());
    public Task<Player?> GetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
    public Task AddAsync(Player player, CancellationToken ct = default) { Items.Add(player); return Task.CompletedTask; }
    public void Remove(Player player) => Items.Remove(player);
    public Task<bool> IsInUseAsync(int playerId, CancellationToken ct = default) => Task.FromResult(false);
}


internal sealed class FakeDeviceLinks : IDeviceLinkRepository
{
    private int _next = 1;
    public List<DeviceLink> Items { get; } = new();
    public Task<DeviceLink?> GetByHashAsync(string deviceHash, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(l => l.DeviceHash == deviceHash));
    public Task<DeviceLink?> GetByPlayerAsync(int playerId, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(l => l.PlayerId == playerId));
    public Task<IReadOnlyList<DeviceLink>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<DeviceLink>>(Items.ToList());
    public Task AddAsync(DeviceLink link, CancellationToken ct = default) { link.WithId(_next++); Items.Add(link); return Task.CompletedTask; }
    public void Remove(DeviceLink link) => Items.Remove(link);
}

internal sealed class FakeDelegates : IPlayerDelegateRepository
{
    private int _next = 1;
    public List<PlayerDelegate> Items { get; } = new();
    public Task<IReadOnlyList<PlayerDelegate>> ListByOwnerAsync(int ownerPlayerId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PlayerDelegate>>(Items.Where(d => d.OwnerPlayerId == ownerPlayerId).ToList());
    public Task<IReadOnlyList<PlayerDelegate>> ListAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PlayerDelegate>>(Items.ToList());
    public Task<PlayerDelegate?> GetAsync(int ownerPlayerId, int delegatePlayerId, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(d => d.OwnerPlayerId == ownerPlayerId && d.DelegatePlayerId == delegatePlayerId));
    public Task AddAsync(PlayerDelegate playerDelegate, CancellationToken ct = default) { playerDelegate.WithId(_next++); Items.Add(playerDelegate); return Task.CompletedTask; }
    public void Remove(PlayerDelegate playerDelegate) => Items.Remove(playerDelegate);
}

/// <summary>Identificadores válidos: 8 o más caracteres alfanuméricos. Hash reconocible para las pruebas.</summary>
internal sealed class FakeHasher : IDeviceTokenHasher
{
    public bool IsValid(string? token) => token is { Length: >= 8 } && token.All(char.IsLetterOrDigit);
    public string Hash(string token) => "hash:" + token;
}

internal sealed class BookingWorld
{
    public FakeClock Clock { get; } = new() { Now = new DateTime(2026, 10, 15, 10, 0, 0) };
    public FakeUow Uow { get; } = new();
    public FakeSlots Slots { get; } = new();
    public FakeTemplates Templates { get; } = new();
    public FakePlayers Players { get; } = new();
    public FakeDeviceLinks Devices { get; } = new();
    public FakeDelegates Delegates { get; } = new();
    public FakeHasher Hasher { get; } = new();
    public BookingOptions Options { get; } = new();
    public IOpeningPolicy Opening { get; } = new WeeklyOpeningPolicy(DayOfWeek.Friday, new TimeOnly(19, 0));

    public BookingWorld()
    {
        Templates.AddAsync(SlotTemplate.Create(DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(20, 0)));
        Templates.AddAsync(SlotTemplate.Create(DayOfWeek.Friday, new TimeOnly(19, 0), new TimeOnly(22, 0)));
        for (var i = 1; i <= 5; i++) Players.Items.Add(TestHelpers.Player(i, $"Jugador {i}", PlayerPosition.Delantero));
    }

    public SlotScheduleService Schedule() =>
        new(Slots, Templates, new WeeklySlotGenerator(), Opening, Clock, Uow);
    public SlotDtoBuilder Builder() => new(Players, Clock, Options, Opening);
    public AttendanceCommands Attendance() => new(Slots, Players, Builder(), Clock, Uow);
    public SlotAdminCommands Admin() => new(Slots, Builder(), Opening, Clock, Uow);
    public SlotQueries Queries() => new(Slots, Builder(), Clock);

    public DeviceAccessResolver Access() => new(Hasher, Devices, Delegates);
    public DeviceLinkService Links() => new(Hasher, Access(), Devices, Players, Uow);
    public DeviceLinkAdminCommands LinkAdmin() => new(Devices, Players, Uow);
    public PlayerDelegateService DelegateService() => new(Delegates, Players, Uow);
    public DeviceAttendanceCommands DeviceAttendance() => new(Access(), Attendance());
}

