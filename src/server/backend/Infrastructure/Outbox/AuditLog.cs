namespace TicketFlow.Infrastructure.Outbox;

/// <summary>
/// Bitácora de auditoría para trazabilidad (quién, cuándo, qué)
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // e.g., "Ticket"
    public string EntityId { get; set; } = string.Empty;    // Guid en string
    public string Action { get; set; } = string.Empty;      // e.g., TicketCreated, TicketStatusChanged, TicketAssigned
    public string? PerformedBy { get; set; }                // UserId
    public string? PerformedByName { get; set; }            // Nombre legible del usuario
    public DateTime OccurredAt { get; set; }
    public string? CorrelationId { get; set; }              // Correlación para agrupar acciones relacionadas
    public string? Data { get; set; }                       // JSON con detalle (old/new)
}
