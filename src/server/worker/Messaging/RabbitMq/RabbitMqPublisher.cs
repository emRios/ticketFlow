using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;

namespace TicketFlow.Worker.Messaging.RabbitMq;

/// <summary>
/// Implementación de publisher para RabbitMQ
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName;

    public RabbitMqPublisher(
        ILogger<RabbitMqPublisher> logger,
        string hostname = "localhost",
        string exchangeName = "ticketflow.events",
        string username = "guest",
        string password = "guest",
        int port = 5672)
    {
        _logger = logger;
        _exchangeName = exchangeName;

        // Crear conexión a RabbitMQ
        var factory = new ConnectionFactory
        {
            HostName = hostname,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declarar exchange de tipo topic
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation(
            "RabbitMQ Publisher conectado a {Host}. Exchange: {Exchange}",
            hostname,
            _exchangeName);
    }

    /// <summary>
    /// Publica un mensaje a RabbitMQ (versión genérica de IMessagePublisher)
    /// </summary>
    public Task PublishAsync(
        string eventType,
        string payloadJson,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        // Routing key basado en el tipo de evento
        var routingKey = GetRoutingKey(eventType);
        
        // Delegar a método Publish específico
        Publish(_exchangeName, routingKey, payloadJson, correlationId, eventType);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publica un mensaje a un exchange específico con routing key personalizada
    /// </summary>
    /// <param name="exchange">Nombre del exchange</param>
    /// <param name="routingKey">Routing key (ej: "ticket.created")</param>
    /// <param name="jsonPayload">Contenido del mensaje en JSON</param>
    /// <param name="correlationId">ID de correlación opcional</param>
    /// <param name="messageType">Tipo del mensaje (opcional)</param>
    public void Publish(
        string exchange, 
        string routingKey, 
        string jsonPayload,
        string? correlationId = null,
        string? messageType = null)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(jsonPayload);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Mensaje durable
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                properties.CorrelationId = correlationId;
            }

            if (!string.IsNullOrEmpty(messageType))
            {
                properties.Type = messageType;
            }

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug(
                "📤 Mensaje publicado → Exchange: {Exchange}, RoutingKey: {RoutingKey}, CorrelationId: {CorrelationId}",
                exchange,
                routingKey,
                correlationId ?? "N/A");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "❌ Error al publicar mensaje a RabbitMQ. Exchange: {Exchange}, RoutingKey: {RoutingKey}", 
                exchange, 
                routingKey);
            throw;
        }
    }

    /// <summary>
    /// Convierte el tipo de evento a routing key
    /// Ejemplo: "TicketFlow.Domain.Events.TicketCreatedEvent" → "ticket.created"
    /// </summary>
    private string GetRoutingKey(string eventType)
    {
        // Extraer el nombre del evento sin namespace
        var eventName = eventType.Split('.').Last();

        // Remover sufijo "Event" si existe
        if (eventName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            eventName = eventName[..^5]; // Remover últimos 5 caracteres
        }

        // Convertir de PascalCase a snake_case
        // Ejemplo: "TicketCreated" → "ticket.created"
        var parts = System.Text.RegularExpressions.Regex
            .Replace(eventName, "([a-z])([A-Z])", "$1.$2")
            .ToLowerInvariant();

        return parts;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        _logger.LogInformation("RabbitMQ Publisher desconectado");
    }
}
