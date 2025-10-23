namespace TicketFlow.Domain.Events;

/// <summary>
/// Evento de dominio - Ticket creado
/// </summary>
public record TicketCreatedEvent : IDomainEvent
{
    public Guid TicketId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
