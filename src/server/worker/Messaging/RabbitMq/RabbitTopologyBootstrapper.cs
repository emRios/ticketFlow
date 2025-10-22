using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace TicketFlow.Worker.Messaging.RabbitMq;

/// <summary>
/// Bootstrapper para inicializar la topología de RabbitMQ
/// Crea exchanges, colas y bindings necesarios para el sistema
/// </summary>
public class RabbitTopologyBootstrapper
{
    private readonly ILogger<RabbitTopologyBootstrapper> _logger;
    private readonly string _hostname;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public RabbitTopologyBootstrapper(
        ILogger<RabbitTopologyBootstrapper> logger,
        string hostname = "localhost",
        int port = 5672,
        string username = "guest",
        string password = "guest")
    {
        _logger = logger;
        _hostname = hostname;
        _port = port;
        _username = username;
        _password = password;
    }

    /// <summary>
    /// Inicializa la topología completa de RabbitMQ
    /// </summary>
    public void InitializeTopology()
    {
        _logger.LogInformation("🚀 Inicializando topología de RabbitMQ en {Host}:{Port}...", _hostname, _port);

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostname,
                Port = _port,
                UserName = _username,
                Password = _password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // 1. Declarar Exchange principal de tipo Topic
            DeclareExchange(channel, "tickets", ExchangeType.Topic);

            // 2. Declarar Colas
            DeclareQueue(channel, "notifications");
            DeclareQueue(channel, "metrics");

            // 3. Crear Bindings (ticket.* a ambas colas)
            BindQueue(channel, "notifications", "tickets", "ticket.*");
            BindQueue(channel, "metrics", "tickets", "ticket.*");

            _logger.LogInformation("✅ Topología de RabbitMQ inicializada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al inicializar topología de RabbitMQ");
            throw;
        }
    }

    /// <summary>
    /// Declara un exchange
    /// </summary>
    private void DeclareExchange(IModel channel, string exchangeName, string exchangeType)
    {
        channel.ExchangeDeclare(
            exchange: exchangeName,
            type: exchangeType,
            durable: true,       // Sobrevive a reinicios del broker
            autoDelete: false,   // No se elimina automáticamente
            arguments: null
        );

        _logger.LogInformation(
            "📢 Exchange declarado: '{Exchange}' (Type: {Type}, Durable: true)",
            exchangeName,
            exchangeType);
    }

    /// <summary>
    /// Declara una cola durable
    /// </summary>
    private void DeclareQueue(IModel channel, string queueName)
    {
        channel.QueueDeclare(
            queue: queueName,
            durable: true,       // Sobrevive a reinicios del broker
            exclusive: false,    // Puede ser accedida por múltiples conexiones
            autoDelete: false,   // No se elimina automáticamente
            arguments: null
        );

        _logger.LogInformation(
            "📥 Cola declarada: '{Queue}' (Durable: true, Exclusive: false)",
            queueName);
    }

    /// <summary>
    /// Crea un binding entre una cola y un exchange con routing key
    /// </summary>
    private void BindQueue(IModel channel, string queueName, string exchangeName, string routingKey)
    {
        channel.QueueBind(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey,
            arguments: null
        );

        _logger.LogInformation(
            "🔗 Binding creado: Cola '{Queue}' ← Exchange '{Exchange}' (RoutingKey: '{RoutingKey}')",
            queueName,
            exchangeName,
            routingKey);
    }
}
