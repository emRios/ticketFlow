using System.Text.Json;
using TicketFlow.Domain.Events;
using TicketFlow.Infrastructure.Outbox;

namespace TicketFlow.Infrastructure.Persistence.EfCore.OutboxStore;

/// <summary>
/// Helper para mapear eventos de dominio a mensajes del Outbox
/// </summary>
public static class OutboxMapper
{
    /// <summary>
    /// Opciones de serialización JSON optimizadas para eventos de dominio
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false, // Compacto para ahorrar espacio
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        // Incluir campos privados si es necesario para reconstruir el evento
        IncludeFields = true
    };

    /// <summary>
    /// Mapea un evento de dominio a un mensaje del Outbox
    /// </summary>
    /// <param name="domainEvent">Evento de dominio a mapear</param>
    /// <param name="correlationId">ID de correlación opcional para rastreo distribuido</param>
    /// <returns>OutboxMessage listo para persistir</returns>
    public static OutboxMessage ToOutboxMessage(IDomainEvent domainEvent, string? correlationId = null)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        var eventType = domainEvent.GetType();
        
        // Serializar el evento completo, incluyendo el tipo para deserialización posterior
        var payloadJson = JsonSerializer.Serialize(domainEvent, eventType, SerializerOptions);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = GetEventTypeName(eventType),
            PayloadJson = payloadJson,
            OccurredAt = domainEvent.OccurredAt,
            CorrelationId = correlationId,
            DispatchedAt = null, // Pendiente de despacho
            Attempts = 0,
            Error = null
        };
    }

    /// <summary>
    /// Mapea múltiples eventos de dominio a mensajes del Outbox
    /// </summary>
    /// <param name="domainEvents">Colección de eventos de dominio</param>
    /// <param name="correlationId">ID de correlación común para todos los eventos</param>
    /// <returns>Lista de OutboxMessages</returns>
    public static IEnumerable<OutboxMessage> ToOutboxMessages(
        IEnumerable<IDomainEvent> domainEvents, 
        string? correlationId = null)
    {
        return domainEvents.Select(e => ToOutboxMessage(e, correlationId));
    }

    /// <summary>
    /// Deserializa un OutboxMessage de vuelta a un evento de dominio
    /// </summary>
    /// <typeparam name="TEvent">Tipo del evento esperado</typeparam>
    /// <param name="outboxMessage">Mensaje del Outbox</param>
    /// <returns>Evento de dominio deserializado</returns>
    public static TEvent? FromOutboxMessage<TEvent>(OutboxMessage outboxMessage) 
        where TEvent : IDomainEvent
    {
        if (outboxMessage == null)
            throw new ArgumentNullException(nameof(outboxMessage));

        return JsonSerializer.Deserialize<TEvent>(outboxMessage.PayloadJson, SerializerOptions);
    }

    /// <summary>
    /// Deserializa un OutboxMessage a un evento de dominio usando el tipo almacenado
    /// </summary>
    /// <param name="outboxMessage">Mensaje del Outbox</param>
    /// <param name="eventType">Tipo del evento a deserializar</param>
    /// <returns>Evento de dominio deserializado</returns>
    public static IDomainEvent? FromOutboxMessage(OutboxMessage outboxMessage, Type eventType)
    {
        if (outboxMessage == null)
            throw new ArgumentNullException(nameof(outboxMessage));

        if (!typeof(IDomainEvent).IsAssignableFrom(eventType))
            throw new ArgumentException($"El tipo {eventType.Name} no implementa IDomainEvent", nameof(eventType));

        return JsonSerializer.Deserialize(outboxMessage.PayloadJson, eventType, SerializerOptions) as IDomainEvent;
    }

    /// <summary>
    /// Obtiene el nombre del tipo de evento en formato legible
    /// Ejemplo: "TicketCreatedEvent" o "Domain.Events.TicketCreatedEvent"
    /// </summary>
    private static string GetEventTypeName(Type eventType)
    {
        // Opción 1: Usar solo el nombre de la clase (simple)
        // return eventType.Name;

        // Opción 2: Usar el nombre completo con namespace (recomendado para evitar colisiones)
        return eventType.FullName ?? eventType.Name;
    }

    /// <summary>
    /// Genera un ID de correlación único para rastreo
    /// </summary>
    public static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N"); // Sin guiones, más compacto
    }
}
