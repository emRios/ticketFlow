namespace TicketFlow.Application.UseCases.Tickets.Commands;

/// <summary>
/// Comando para asignar un ticket a un agente
/// </summary>
public record AssignTicketCommand
{
    public Guid TicketId { get; init; }
    public Guid AssigneeId { get; init; }
    public Guid PerformedBy { get; init; } // Quién hace la asignación (admin)
    public string? Reason { get; init; }
}
