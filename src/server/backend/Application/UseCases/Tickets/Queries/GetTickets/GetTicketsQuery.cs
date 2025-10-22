namespace TicketFlow.Application.UseCases.Tickets.Queries.GetTickets;

/// <summary>
/// Query para obtener lista de tickets con filtros opcionales
/// </summary>
public record GetTicketsQuery(
    string? Status = null,
    string? Priority = null,
    string? AssignedTo = null
);
