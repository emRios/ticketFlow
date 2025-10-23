namespace TicketFlow.Worker.Messaging;

/// <summary>
/// Stub histórico movido desde Infrastructure. No se usa en ejecución.
/// Se mantiene aquí solo como referencia de futura abstracción.
/// Preferir inyectar IMessagePublisher (RabbitMqPublisher) en los servicios.
/// </summary>
public class MessagePublisher
{
    // Método ejemplo; no se usa. Mantener para compatibilidad temporal.
    public async Task PublishAsync<T>(T message) where T : class
    {
        await Task.CompletedTask;
    }
}
