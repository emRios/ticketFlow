namespace TicketFlow.Domain.Events;

/// <summary>
/// Evento de dominio - Cambio de estado del ticket
/// </summary>
public record TicketStatusChangedEvent : IDomainEvent
{
    public Guid TicketId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? Comment { get; init; }
}
