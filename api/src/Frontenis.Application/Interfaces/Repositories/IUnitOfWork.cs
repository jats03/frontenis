namespace Frontenis.Application.Interfaces.Repositories;

/// <summary>Confirma como una sola operación los cambios hechos a través de los repositorios.</summary>
public interface IUnitOfWork
{
    /// <summary>Guarda en una sola transacción todos los cambios pendientes de los repositorios.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
