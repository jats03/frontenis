using Frontenis.Application.Abstractions;
using Frontenis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frontenis.Infrastructure.Persistence;

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

    Task IUnitOfWork.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

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
            e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HomeEntry).WithMany().HasForeignKey(x => x.HomeEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AwayEntry).WithMany().HasForeignKey(x => x.AwayEntryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NextMatch).WithMany().HasForeignKey(x => x.NextMatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CompetitionId, x.Phase, x.GroupId, x.Round });
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
