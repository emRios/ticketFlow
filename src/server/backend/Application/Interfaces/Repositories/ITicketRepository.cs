using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Interfaces.Repositories;

/// <summary>
/// Interfaz de repositorio para Tickets
/// </summary>
public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id);
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task DeleteAsync(Guid id);
}
