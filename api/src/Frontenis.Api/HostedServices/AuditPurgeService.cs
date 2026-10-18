using Frontenis.Application.Interfaces.Repositories;

namespace Frontenis.Api.HostedServices;

/// <summary>Borra cada día los registros de actividad con más de <c>Audit:RetentionDays</c> días (30 por defecto).</summary>
public sealed class AuditPurgeService : BackgroundService
{
    public const int DefaultRetentionDays = 30;
    private static readonly TimeSpan FirstRun = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Every = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AuditPurgeService> _logger;
    private readonly int _days;

    public AuditPurgeService(IServiceScopeFactory scopes, IConfiguration config, ILogger<AuditPurgeService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        var days = config.GetValue("Audit:RetentionDays", DefaultRetentionDays);
        _days = days > 0 ? days : DefaultRetentionDays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRun, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeAsync(stoppingToken);
                await Task.Delay(Every, stoppingToken);
            }
        }
        catch (OperationCanceledException) { /* apagado */ }
    }

    private async Task PurgeAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var removed = await scope.ServiceProvider.GetRequiredService<IAuditRepository>()
                .PurgeOlderThanAsync(DateTime.UtcNow.AddDays(-_days), ct);
            _logger.LogInformation("Registro de actividad: {Removed} entradas de más de {Days} días borradas.", removed, _days);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo limpiar el registro de actividad.");
        }
    }
}
