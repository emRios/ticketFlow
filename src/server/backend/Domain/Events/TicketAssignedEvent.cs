namespace TicketFlow.Domain.Events;

/// <summary>
/// Evento de dominio - Ticket asignado a un usuario
/// </summary>
public record TicketAssignedEvent : IDomainEvent
{
    public Guid TicketId { get; init; }
    public string AssigneeId { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? Reason { get; init; }
}
