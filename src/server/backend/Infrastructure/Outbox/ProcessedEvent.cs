namespace TicketFlow.Infrastructure.Outbox;

/// <summary>
/// Registro de eventos ya procesados para prevenir duplicados (idempotencia)
/// </summary>
public class ProcessedEvent
{
    /// <summary>
    /// ID único del evento procesado (debe coincidir con el ID del evento de dominio)
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Fecha y hora cuando se procesó el evento
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}
