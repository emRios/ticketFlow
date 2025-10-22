namespace TicketFlow.Application.UseCases.Tickets.Queries.GetTicketById;

/// <summary>
/// Query para obtener un ticket por ID
/// </summary>
public record GetTicketByIdQuery(Guid TicketId);
