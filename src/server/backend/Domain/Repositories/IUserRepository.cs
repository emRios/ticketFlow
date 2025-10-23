using TicketFlow.Domain.Entities;

namespace TicketFlow.Domain.Repositories;

/// <summary>
/// Repositorio de usuarios
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<User>> GetByRoleAsync(string role, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
}
