using TicketFlow.Application.Interfaces.Repositories;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Tickets
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly TicketFlowDbContext _context;

    public TicketRepository(TicketFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        // TODO: Implementar con EF Core
        await Task.CompletedTask;
        return null;
    }

    public async Task<IEnumerable<Ticket>> GetAllAsync()
    {
        // TODO: Implementar
        await Task.CompletedTask;
        return Array.Empty<Ticket>();
    }

    public async Task AddAsync(Ticket ticket)
    {
        // TODO: Implementar
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        // TODO: Implementar
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        // TODO: Implementar
        await Task.CompletedTask;
    }
}
