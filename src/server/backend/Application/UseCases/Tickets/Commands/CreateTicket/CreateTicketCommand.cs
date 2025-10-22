namespace TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;

/// <summary>
/// Comando para crear un nuevo ticket
/// </summary>
public record CreateTicketCommand(
    string Title,
    string? Description,
    string? Priority,
    string CreatedBy
);
