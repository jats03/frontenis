using Frontenis.Application.Interfaces.Services;
using Frontenis.Application.Services;
using Frontenis.Domain.Interfaces;

namespace Frontenis.Api.HostedServices;

/// <summary>
/// Cada martes y jueves, a partir de las 8:00 (hora de Madrid), crea el entreno de ese día vacío. Revisa al arrancar y
/// luego los martes y jueves a las 8:00, 13:00, 18:00 y 23:00 (cada 5 horas): si el servidor estaba dormido a las 8:00,
/// lo crea al despertar ese mismo día.
/// </summary>
public sealed class TrainingAutoCreateService : BackgroundService
{
    private static readonly TimeSpan FirstRun = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<TrainingAutoCreateService> _logger;

    public TrainingAutoCreateService(IServiceScopeFactory scopes, ILogger<TrainingAutoCreateService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRun, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunAsync(stoppingToken);
                await Task.Delay(TimeUntilNextCheck(), stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* apagado */ }
    }

    private TimeSpan TimeUntilNextCheck()
    {
        using var scope = _scopes.CreateScope();
        var now = scope.ServiceProvider.GetRequiredService<IClock>().Now;
        var wait = TrainingAutoCreator.NextCheck(now) - now;
        return wait < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : wait;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            if (await scope.ServiceProvider.GetRequiredService<ITrainingAutoCreator>().EnsureTodayAsync(ct))
                _logger.LogInformation("Entreno de hoy creado automáticamente.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo crear el entreno automático.");
        }
    }
}
