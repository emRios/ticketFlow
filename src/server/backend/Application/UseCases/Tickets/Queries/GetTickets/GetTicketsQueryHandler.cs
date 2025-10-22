using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Application.Interfaces;

namespace TicketFlow.Application.UseCases.Tickets.Queries.GetTickets;

/// <summary>
/// Handler para obtener lista de tickets con filtros
/// </summary>
public class GetTicketsQueryHandler
{
    private readonly ITicketRepository _repository;

    public GetTicketsQueryHandler(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Ticket>> HandleAsync(
        GetTicketsQuery query, 
        CancellationToken cancellationToken = default)
    {
        // Obtener todos los tickets
        var tickets = await _repository.GetAllAsync(cancellationToken);

        // TODO: Aplicar filtros cuando se agreguen propiedades Status, Priority, AssignedTo a la entidad Ticket
        // Por ahora retornamos todos los tickets ordenados por fecha de creación

        return tickets
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }
}
