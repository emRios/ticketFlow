namespace TicketFlow.Application.Interfaces;

/// <summary>
/// Interfaz del Unit of Work para transacciones
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Guarda todos los cambios pendientes
    /// Dispara el interceptor de Outbox automáticamente
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
