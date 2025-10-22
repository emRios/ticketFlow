using TicketFlow.Domain.Events;

namespace TicketFlow.Domain.Entities;

/// <summary>
/// Interfaz para entidades que generan eventos de dominio
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
