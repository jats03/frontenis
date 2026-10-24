using Frontenis.Application.Interfaces.Services;

namespace Frontenis.Api.HostedServices;

/// <summary>
/// Cada martes y jueves, en cuanto es posible a partir de las 9:00 (hora de Madrid), crea el entreno de ese día vacío.
/// Revisa cada pocos minutos: si el servidor estaba dormido a las 9:00, lo crea al despertar ese mismo día.
/// </summary>
public sealed class TrainingAutoCreateService : BackgroundService
{
    private static readonly TimeSpan FirstRun = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(10);

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
                await Task.Delay(Every, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* apagado */ }
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
