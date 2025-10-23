namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad dedicada para el seguimiento de actividad de tickets
/// Proporciona trazabilidad completa: quién, cuándo, qué cambió
/// </summary>
public class TicketActivity
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string Action { get; private set; } = string.Empty; // TicketCreated, TicketAssigned, TicketStatusChanged
    public DateTime OccurredAt { get; private set; }
    
    // Trazabilidad de usuario
    public string? PerformedBy { get; private set; } // UserId
    public string? PerformedByName { get; private set; } // Nombre legible
    
    // Correlación para agrupar acciones relacionadas
    public string? CorrelationId { get; private set; }
    
    // Campos específicos por tipo de acción (normalizados)
    public string? Title { get; private set; } // Para TicketCreated
    public string? AssigneeId { get; private set; } // Para TicketAssigned
    public string? AssigneeName { get; private set; } // Para TicketAssigned
    public string? Reason { get; private set; } // Para TicketAssigned
    public string? OldStatus { get; private set; } // Para TicketStatusChanged
    public string? NewStatus { get; private set; } // Para TicketStatusChanged
    public string? Comment { get; private set; } // Para TicketStatusChanged
    
    private TicketActivity() { }
    
    public static TicketActivity ForCreated(
        Guid ticketId,
        string title,
        string? performedBy,
        string? performedByName,
        string? correlationId,
        DateTime occurredAt)
    {
        return new TicketActivity
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Action = "TicketCreated",
            OccurredAt = occurredAt,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            CorrelationId = correlationId,
            Title = title
        };
    }
    
    public static TicketActivity ForAssigned(
        Guid ticketId,
        string assigneeId,
        string? assigneeName,
        string? reason,
        string? performedBy,
        string? performedByName,
        string? correlationId,
        DateTime occurredAt)
    {
        return new TicketActivity
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Action = "TicketAssigned",
            OccurredAt = occurredAt,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            CorrelationId = correlationId,
            AssigneeId = assigneeId,
            AssigneeName = assigneeName,
            Reason = reason
        };
    }
    
    public static TicketActivity ForStatusChanged(
        Guid ticketId,
        string oldStatus,
        string newStatus,
        string? comment,
        string? performedBy,
        string? performedByName,
        string? correlationId,
        DateTime occurredAt)
    {
        return new TicketActivity
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Action = "TicketStatusChanged",
            OccurredAt = occurredAt,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            CorrelationId = correlationId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comment = comment
        };
    }
}
