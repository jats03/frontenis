using Frontenis.Application.Abstractions;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Services;
using Frontenis.Infrastructure.Persistence;
using Frontenis.Infrastructure.Repositories;
using Frontenis.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frontenis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFrontenis(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<FrontenisDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default"), sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FrontenisDbContext>());

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IPairRepository, PairRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Dominio (sin estado -> singleton)
        services.AddSingleton<IRandomizer, SystemRandomizer>();
        services.AddSingleton<IPairingStrategy, RandomPairingStrategy>();
        services.AddSingleton<IGroupDistributor, ShuffleGroupDistributor>();
        services.AddSingleton<IScheduleGenerator, RoundRobinScheduleGenerator>();
        services.AddSingleton<IStandingsCalculator, StandingsCalculator>();
        services.AddSingleton<IBracketGenerator, SingleEliminationBracketGenerator>();

        // Casos de uso
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IPairingService, PairingService>();
        services.AddScoped<ICompetitionService, CompetitionService>();
        services.AddScoped<IMatchService, MatchService>();
        return services;
    }

    /// <summary>Crea la BD si no existe y siembra el usuario administrador.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, IConfiguration config)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FrontenisDbContext>();
        await db.Database.EnsureCreatedAsync();

        var username = config["Admin:Username"] ?? "admin";
        if (await db.Users.AnyAsync(u => u.Username == username)) return;
        var password = config["Admin:Password"] ?? throw new InvalidOperationException("Falta Admin:Password en la configuración.");
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new User(username, hasher.Hash(password), Roles.Admin));
        await db.SaveChangesAsync();
    }
}
