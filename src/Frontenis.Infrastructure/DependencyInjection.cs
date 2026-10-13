using Frontenis.Application.Abstractions;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Services;
using Frontenis.Infrastructure.Persistence;
using Frontenis.Infrastructure.Repositories;
using Frontenis.Infrastructure.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frontenis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFrontenis(this IServiceCollection services, IConfiguration config)
    {
        // Azure SQL gratuito se pausa por inactividad y tarda en reanudarse: damos más margen de conexión
        // (mínimo 60 s) y más reintentos antes de fallar.
        var connection = new SqlConnectionStringBuilder(config.GetConnectionString("Default") ?? string.Empty);
        if (connection.ConnectTimeout < 60) connection.ConnectTimeout = 60;
        services.AddDbContext<FrontenisDbContext>(o =>
            o.UseSqlServer(connection.ConnectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));
        // Caché de lecturas públicas en memoria; cada guardado correcto la vacía.
        services.AddSingleton<IReadCache, ReadCache>();
        services.AddScoped<IUnitOfWork>(sp =>
            new CachingUnitOfWork(sp.GetRequiredService<FrontenisDbContext>(), sp.GetRequiredService<IReadCache>()));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IPairRepository, PairRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMatchHistoryRepository, MatchHistoryRepository>();
        services.AddScoped<ITrainingRepository, TrainingRepository>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginThrottle, LoginThrottle>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Dominio (sin estado -> singleton)
        services.AddSingleton<IRandomizer, SystemRandomizer>();
        services.AddSingleton<IPairingStrategy, RandomPairingStrategy>();
        services.AddSingleton<IGroupDistributor, ShuffleGroupDistributor>();
        services.AddSingleton<IScheduleGenerator, RoundRobinScheduleGenerator>();
        services.AddSingleton<IStandingsCalculator, StandingsCalculator>();
        services.AddSingleton<IBracketGenerator, SingleEliminationBracketGenerator>();
        services.AddSingleton<IRotationPlanner, RotationPlanner>();
        services.AddSingleton<IIndividualStandingsCalculator, IndividualStandingsCalculator>();
        services.AddSingleton<IPlayerStatsCalculator, PlayerStatsCalculator>();
        services.AddSingleton<ITrainingPlanner, TrainingPlanner>();

        // Casos de uso
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IPairingService, PairingService>();
        services.AddScoped<ICompetitionService, CompetitionService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IPlayerHistoryService, PlayerHistoryService>();
        services.AddScoped<ITrainingService, TrainingService>();
        return services;
    }

    /// <summary>Crea la BD si no existe y siembra el usuario administrador.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, IConfiguration config)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FrontenisDbContext>();
        await db.Database.EnsureCreatedAsync();

        // EnsureCreated no modifica tablas existentes: añadimos de forma idempotente las columnas de la liga rotatoria.
        await db.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('Competitions','Rounds') IS NULL ALTER TABLE Competitions ADD Rounds INT NOT NULL CONSTRAINT DF_Competitions_Rounds DEFAULT 0;");
        await db.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('Players','GroupNumber') IS NULL ALTER TABLE Players ADD GroupNumber INT NULL;");
        await db.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('Competitions','NextCompetitionId') IS NULL ALTER TABLE Competitions ADD NextCompetitionId INT NULL;");

        await db.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('Competitions','SetsPerMatch') IS NULL ALTER TABLE Competitions ADD SetsPerMatch INT NOT NULL CONSTRAINT DF_Competitions_SetsPerMatch DEFAULT 1;");
        foreach (var col in new[] { "Set1Home", "Set1Away", "Set2Home", "Set2Away", "Set3Home", "Set3Away" })
            await db.Database.ExecuteSqlRawAsync(
                $"IF COL_LENGTH('Matches','{col}') IS NULL ALTER TABLE Matches ADD {col} INT NULL;");

        // Tablas de entrenamientos (partidos sueltos): EnsureCreated no añade tablas a una BD existente.
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('TrainingSessions') IS NULL
CREATE TABLE TrainingSessions (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    SessionDate DATETIME2 NOT NULL,
    PointsPerWin INT NOT NULL,
    PointsPerLoss INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);");
        await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('TrainingMatches') IS NULL
CREATE TABLE TrainingMatches (
    Id INT IDENTITY PRIMARY KEY,
    SessionId INT NOT NULL REFERENCES TrainingSessions(Id) ON DELETE CASCADE,
    [Order] INT NOT NULL,
    Home1Id INT NOT NULL REFERENCES Players(Id),
    Home2Id INT NOT NULL REFERENCES Players(Id),
    Away1Id INT NOT NULL REFERENCES Players(Id),
    Away2Id INT NOT NULL REFERENCES Players(Id),
    TargetPoints INT NOT NULL,
    HomeScore INT NOT NULL,
    AwayScore INT NOT NULL,
    PlayedAtUtc DATETIME2 NOT NULL
);");
        await db.Database.ExecuteSqlRawAsync(
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingMatches_SessionId_Order') " +
            "CREATE INDEX IX_TrainingMatches_SessionId_Order ON TrainingMatches(SessionId, [Order]);");

        // Los tantos totales (suma de sets) pueden empatar: el ganador se decide por sets.
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('CK_Matches_NoTie') IS NOT NULL ALTER TABLE Matches DROP CONSTRAINT CK_Matches_NoTie;");

        var username = config["Admin:Username"] ?? "admin";
        if (await db.Users.AnyAsync(u => u.Username == username)) return;
        var password = config["Admin:Password"] ?? throw new InvalidOperationException("Falta Admin:Password en la configuración.");
        var weak = PasswordPolicy.Validate(password, username);
        if (weak is not null)
            throw new InvalidOperationException($"Admin:Password no es segura ({weak}) Cámbiala en la configuración antes del primer arranque.");
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new User(username, hasher.Hash(password), Roles.Admin));
        await db.SaveChangesAsync();
    }
}
