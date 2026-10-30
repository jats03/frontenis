using Frontenis.Application.Caching;
using Frontenis.Application.Interfaces.Caching;
using Frontenis.Application.Interfaces.Repositories;
using Frontenis.Application.Interfaces.Security;
using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Models;
using Frontenis.Application.Security;
using Frontenis.Application.Services;
using Frontenis.Domain.Interfaces;
using Frontenis.Domain.Services;
using Frontenis.Infrastructure.Persistence;
using Frontenis.Infrastructure.Repositories;
using Frontenis.Infrastructure.Security;
using Frontenis.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frontenis.Infrastructure.Extensions;

/// <summary>Registro de dependencias de toda la aplicación (composition root de las capas internas).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFrontenis(this IServiceCollection services, IConfiguration config) => services
        .AddPersistence(config)
        .AddSecurity(config)
        .AddDomainServices()
        .AddBookings(config)
        .AddApplicationServices();

    /// <summary>Crea el esquema y siembra el administrador (se llama al arrancar, con reintentos).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider provider, CancellationToken ct = default)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(ct);
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        // Neon (PostgreSQL) suspende el cómputo tras unos minutos sin uso: damos margen de conexión
        // (mínimo 30 s) y reintentos automáticos ante cortes transitorios.
        var connection = PostgresConnectionString.Build(config.GetConnectionString("Default"));

        services.AddDbContext<FrontenisDbContext>(o =>
            o.UseNpgsql(connection,
                npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        // Caché de lecturas públicas en memoria; cada guardado correcto la vacía.
        // Con Cache__Enabled=false se desactiva (para medir los tiempos reales de la BD); por defecto está activa.
        if (string.Equals(config["Cache:Enabled"], "false", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IReadCache, NullReadCache>();
        else
            services.AddSingleton<IReadCache, ReadCache>();
        services.AddScoped<IUnitOfWork>(sp =>
            new CachingUnitOfWork(sp.GetRequiredService<FrontenisDbContext>(), sp.GetRequiredService<IReadCache>()));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IPairRepository, PairRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMatchHistoryRepository, MatchHistoryRepository>();
        services.AddScoped<ITrainingRepository, TrainingRepository>();
        services.AddScoped<ITrainingAutoCreator, TrainingAutoCreator>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<ISlotTemplateRepository, SlotTemplateRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IDeviceLinkRepository, DeviceLinkRepository>();
        services.AddScoped<IPlayerDelegateRepository, PlayerDelegateRepository>();
        services.AddScoped<IMatchSubmissionRepository, MatchSubmissionRepository>();

        services.AddScoped<SchemaUpdater>();
        services.AddScoped<AdminUserSeeder>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }

    private static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginThrottle, LoginThrottle>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IDeviceTokenHasher, Sha256DeviceTokenHasher>();
        return services;
    }

    /// <summary>Servicios de dominio: sin estado, por tanto singleton.</summary>
    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
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
        return services;
    }

    /// <summary>
    /// Reservas de entrenamientos: reloj de Madrid, regla de apertura y casos de uso. Las consultas de franjas no se
    /// cachean porque su estado (abierta, pocos jugadores) depende de la hora actual.
    /// </summary>
    private static IServiceCollection AddBookings(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(BookingOptions.Section).Get<BookingOptions>() ?? new BookingOptions();
        if (options.MinPlayers < 1) throw new InvalidOperationException("Bookings:MinPlayers debe ser al menos 1.");

        services.AddSingleton(options);
        services.AddSingleton<IClock, MadridClock>();
        services.AddSingleton<IOpeningPolicy>(new WeeklyOpeningPolicy(options.OpenDay, options.OpenTime));
        services.AddSingleton<ISlotGenerator, WeeklySlotGenerator>();

        services.AddScoped<ISlotDtoBuilder, SlotDtoBuilder>();
        services.AddScoped<ISlotQueries, SlotQueries>();
        services.AddScoped<ISlotAdminCommands, SlotAdminCommands>();
        services.AddScoped<ISlotScheduleService, SlotScheduleService>();
        services.AddScoped<IAttendanceCommands, AttendanceCommands>();
        services.AddScoped<ISlotTemplateQueries, SlotTemplateQueries>();
        services.AddScoped<ISlotTemplateCommands, SlotTemplateCommands>();

        services.AddScoped<IDeviceAccessResolver, DeviceAccessResolver>();
        services.AddScoped<IAuditQueries, AuditQueries>();
        services.AddScoped<IDeviceLinkService, DeviceLinkService>();
        services.AddScoped<IDeviceLinkAdminCommands, DeviceLinkAdminCommands>();
        services.AddScoped<IMatchSubmissionService, MatchSubmissionService>();
        services.AddScoped<IDeviceAttendanceCommands, DeviceAttendanceCommands>();
        services.AddScoped<IClothingSizeService, ClothingSizeService>();
        services.AddScoped<PlayerDelegateService>();
        services.AddScoped<IPlayerDelegateQueries>(sp => sp.GetRequiredService<PlayerDelegateService>());
        services.AddScoped<IPlayerDelegateCommands>(sp => sp.GetRequiredService<PlayerDelegateService>());
        return services;
    }

    /// <summary>Casos de uso. Las consultas públicas se decoran con la caché de lecturas.</summary>
    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPairingService, PairingService>();
        services.AddScoped<IMatchService, MatchService>();

        services.AddScoped<IPlayerCommands, PlayerCommands>();
        services.AddScoped<PlayerQueries>();
        services.AddCached<IPlayerQueries, PlayerQueries, CachedPlayerQueries>();

        services.AddScoped<CompetitionCommands>();
        services.AddScoped<ICompetitionCommands>(sp => new CacheInvalidatingCompetitionCommands(
            sp.GetRequiredService<CompetitionCommands>(), sp.GetRequiredService<IReadCache>()));
        services.AddScoped<ICompetitionDrawService, CompetitionDrawService>();
        services.AddScoped<IRotatingLeagueCommands, RotatingLeagueCommands>();
        services.AddScoped<CompetitionQueries>();
        services.AddCached<ICompetitionQueries, CompetitionQueries, CachedCompetitionQueries>();

        services.AddScoped<ITrainingDetailBuilder, TrainingDetailBuilder>();
        services.AddScoped<ITrainingCommands, TrainingCommands>();
        services.AddScoped<ITrainingSuggestionService, TrainingSuggestionService>();
        services.AddScoped<TrainingQueries>();
        services.AddCached<ITrainingQueries, TrainingQueries, CachedTrainingQueries>();

        services.AddScoped<PlayerHistoryService>();
        services.AddCached<IPlayerHistoryService, PlayerHistoryService, CachedPlayerHistoryService>();
        return services;
    }

    /// <summary>Registra <typeparamref name="TService"/> envuelto por su decorador de caché.</summary>
    private static IServiceCollection AddCached<TService, TInner, TDecorator>(this IServiceCollection services)
        where TService : class
        where TInner : class, TService
        where TDecorator : class, TService
    {
        return services.AddScoped<TService>(sp => ActivatorUtilities.CreateInstance<TDecorator>(
            sp, sp.GetRequiredService<TInner>()));
    }
}
