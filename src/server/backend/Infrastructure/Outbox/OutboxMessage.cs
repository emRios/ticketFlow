namespace TicketFlow.Infrastructure.Outbox;

/// <summary>
/// Entidad para el patrón Outbox - garantiza entrega eventual de eventos de dominio
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Identificador único del mensaje
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tipo del evento (ej: "TicketCreated", "TicketStatusChanged")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del mensaje serializado en JSON
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora cuando ocurrió el evento original
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// ID de correlación para rastreo de flujos distribuidos
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Fecha y hora cuando el mensaje fue despachado exitosamente (null = pendiente)
    /// </summary>
    public DateTime? DispatchedAt { get; set; }

    /// <summary>
    /// Número de intentos de procesamiento
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Mensaje de error del último intento fallido
    /// </summary>
    public string? Error { get; set; }
}
