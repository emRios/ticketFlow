namespace TicketFlow.Infrastructure.Outbox;

/// <summary>
/// Repositorio para gestionar mensajes del Outbox
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Obtiene mensajes pendientes de procesar
    /// </summary>
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca un mensaje como procesado exitosamente
    /// </summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca un mensaje como fallido e incrementa contador de reintentos
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
