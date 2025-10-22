using TicketFlow.Domain.Entities;
using TicketFlow.Application.Interfaces;

namespace TicketFlow.Application.UseCases.Tickets.Queries.GetTicketById;

/// <summary>
/// Handler para obtener un ticket por ID
/// </summary>
public class GetTicketByIdQueryHandler
{
    private readonly ITicketRepository _repository;

    public GetTicketByIdQueryHandler(ITicketRepository repository)
    {
        _repository = repository;
    }

    public async Task<Ticket?> HandleAsync(
        GetTicketByIdQuery query, 
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(query.TicketId, cancellationToken);
    }
}
