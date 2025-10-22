using TicketFlow.Application.Interfaces.Repositories;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Users
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly TicketFlowDbContext _context;

    public UserRepository(TicketFlowDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        // TODO: Implementar
        await Task.CompletedTask;
        return null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // TODO: Implementar
        await Task.CompletedTask;
        return null;
    }

    public async Task AddAsync(User user)
    {
        // TODO: Implementar
        await Task.CompletedTask;
    }
}
