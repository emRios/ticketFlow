namespace TicketFlow.Infrastructure.Messaging;

/// <summary>
/// Publicador de mensajes (RabbitMQ, Azure Service Bus, etc.)
/// </summary>
public class MessagePublisher
{
    // TODO: Implementar publicación de mensajes
    public async Task PublishAsync<T>(T message) where T : class
    {
        // Stub
        await Task.CompletedTask;
    }
}
