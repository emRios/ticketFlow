namespace TicketFlow.Worker.Messaging;

/// <summary>
/// Interfaz para publicación de mensajes a sistemas externos (RabbitMQ, Kafka, etc.)
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publica un mensaje al sistema de mensajería
    /// </summary>
    /// <param name="eventType">Tipo del evento (ej: "TicketCreatedEvent")</param>
    /// <param name="payloadJson">Contenido del mensaje en JSON</param>
    /// <param name="correlationId">ID de correlación para rastreo</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task PublishAsync(
        string eventType, 
        string payloadJson, 
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
