using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.Interfaces.Repositories;

/// <summary>
/// Interfaz de repositorio para Users
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}
