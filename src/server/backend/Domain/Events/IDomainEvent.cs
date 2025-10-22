namespace TicketFlow.Domain.Events;

/// <summary>
/// Interfaz base para eventos de dominio
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
