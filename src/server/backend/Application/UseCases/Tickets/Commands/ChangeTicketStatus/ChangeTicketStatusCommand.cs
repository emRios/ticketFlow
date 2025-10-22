namespace TicketFlow.Application.UseCases.Tickets.Commands.ChangeTicketStatus;

/// <summary>
/// Comando para cambiar el estado de un ticket
/// </summary>
public record ChangeTicketStatusCommand(
    Guid TicketId,
    string NewStatus,
    string ChangedBy
);
