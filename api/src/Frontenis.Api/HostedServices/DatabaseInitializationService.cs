using Frontenis.Infrastructure.Extensions;

namespace Frontenis.Api.HostedServices;

/// <summary>
/// Inicializa la BD en segundo plano y con reintentos: así la API empieza a responder (/health) enseguida
/// aunque la base de datos esté suspendida y tarde en reanudarse.
/// </summary>
public sealed class DatabaseInitializationService : BackgroundService
{
    private const int MaxAttempts = 8;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public DatabaseInitializationService(IServiceProvider services, IHostApplicationLifetime lifetime,
        ILogger<DatabaseInitializationService> logger)
    {
        _services = services;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera a que el servidor ya esté atendiendo peticiones antes de tocar la BD.
        var started = new TaskCompletionSource();
        using var registration = _lifetime.ApplicationStarted.Register(() => started.TrySetResult());
        await started.Task.WaitAsync(stoppingToken);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await _services.InitializeDatabaseAsync(stoppingToken);
                _logger.LogInformation("Base de datos lista.");
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Base de datos no disponible todavía (intento {Attempt}/{Max}).", attempt, MaxAttempts);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
        _logger.LogError("No se pudo inicializar la base de datos tras {Max} intentos.", MaxAttempts);
    }
}
