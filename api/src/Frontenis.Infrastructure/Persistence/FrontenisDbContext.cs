using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Persistence;

/// <summary>Contexto de Entity Framework Core; actúa también como unidad de trabajo.</summary>
public sealed class FrontenisDbContext : DbContext, IUnitOfWork
{
    public FrontenisDbContext(DbContextOptions<FrontenisDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Pair> Pairs => Set<Pair>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<CompetitionEntry> Entries => Set<CompetitionEntry>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<TrainingMatch> TrainingMatches => Set<TrainingMatch>();
    public DbSet<SlotTemplate> SlotTemplates => Set<SlotTemplate>();
    public DbSet<TrainingSlot> TrainingSlots => Set<TrainingSlot>();
    public DbSet<SlotAttendance> SlotAttendances => Set<SlotAttendance>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<DeviceLink> DeviceLinks => Set<DeviceLink>();
    public DbSet<PlayerDelegate> PlayerDelegates => Set<PlayerDelegate>();
    public DbSet<MatchSubmission> MatchSubmissions => Set<MatchSubmission>();
    public DbSet<RejectedSubmission> RejectedSubmissions => Set<RejectedSubmission>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void ConfigureConventions(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Player>(e =>
        {
            e.ToTable("Players");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Position).HasConversion<byte>();
        });

        b.Entity<Pair>(e =>
        {
            e.ToTable("Pairs");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.DisplayName);
            e.HasOne(x => x.Player1).WithMany().HasForeignKey(x => x.Player1Id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Player2).WithMany().HasForeignKey(x => x.Player2Id).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Competition>(e =>
        {
            e.ToTable("Competitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Type).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasMany(x => x.Groups).WithOne().HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Entries).WithOne().HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Matches).WithOne().HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Groups).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Matches).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<Group>(e =>
        {
            e.ToTable("Groups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
        });

        b.Entity<CompetitionEntry>(e =>
        {
            e.ToTable("CompetitionEntries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompetitionId, x.PairId }).IsUnique();
            e.HasOne(x => x.Pair).WithMany().HasForeignKey(x => x.PairId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Match>(e =>
        {
            e.ToTable("Matches");
            e.HasKey(x => x.Id);
            e.Property(x => x.Phase).HasConversion<byte>();
            e.Property(x => x.NextSlot).HasConversion<byte?>();
            e.Ignore(x => x.IsPlayed);
            e.Ignore(x => x.IsReady);
            e.Ignore(x => x.Winner);
            e.Ignore(x => x.Sets);
            e.Ignore(x => x.HomeSets);
            e.Ignore(x => x.AwaySets);
            e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HomeEntry).WithMany().HasForeignKey(x => x.HomeEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AwayEntry).WithMany().HasForeignKey(x => x.AwayEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NextMatch).WithMany().HasForeignKey(x => x.NextMatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CompetitionId, x.Phase, x.GroupId, x.Round });
        });

        b.Entity<TrainingSession>(e =>
        {
            e.ToTable("TrainingSessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasMany(x => x.Matches).WithOne().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Matches).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<TrainingMatch>(e =>
        {
            e.ToTable("TrainingMatches");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.HomeWon);
            e.HasIndex(x => new { x.SessionId, x.Order });
        });

        b.Entity<SlotTemplate>(e =>
        {
            e.ToTable("SlotTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Day).HasConversion<byte>();
        });

        // Fecha, horas y apertura son hora local de Madrid: se guardan tal cual, sin convertir zonas horarias.
        b.Entity<TrainingSlot>(e =>
        {
            e.ToTable("TrainingSlots");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.StartsAt);
            e.HasIndex(x => new { x.Date, x.Start });
            e.HasOne<SlotTemplate>().WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Attendees).WithOne().HasForeignKey(x => x.SlotId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Attendees).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<SlotAttendance>(e =>
        {
            e.ToTable("SlotAttendances");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.SlotId, x.PlayerId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.RegisteredByPlayerId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<DeviceLink>(e =>
        {
            e.ToTable("DeviceLinks");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeviceHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.DeviceHash).IsUnique();   // un dispositivo, un jugador
            e.HasIndex(x => x.PlayerId).IsUnique();     // un jugador, un dispositivo (el administrador lo libera)
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MatchSubmission>(e =>
        {
            e.ToTable("MatchSubmissions");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Sets);
            e.HasIndex(x => x.MatchId).IsUnique();      // un único envío pendiente por partido
            e.HasOne<Match>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RejectedSubmission>(e =>
        {
            e.ToTable("RejectedSubmissions");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.Sets);
            e.HasIndex(x => x.CompetitionId);
            // Sin claves foráneas: es una copia de seguridad que debe sobrevivir aunque cambie el partido o el jugador.
        });

        b.Entity<PlayerDelegate>(e =>
        {
            e.ToTable("PlayerDelegates");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OwnerPlayerId, x.DelegatePlayerId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.OwnerPlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.DelegatePlayerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AuditEntry>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Actor).HasConversion<byte>();
            e.Property(x => x.ActorName).HasMaxLength(AuditEntry.MaxName).IsRequired();
            e.Property(x => x.Method).HasMaxLength(10).IsRequired();
            e.Property(x => x.Path).HasMaxLength(AuditEntry.MaxPath).IsRequired();
            e.Property(x => x.Device).HasMaxLength(AuditEntry.MaxDevice);
            e.HasIndex(x => x.AtUtc);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            e.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });
    }
}
