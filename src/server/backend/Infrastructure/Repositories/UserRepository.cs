using Microsoft.EntityFrameworkCore;
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
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<List<User>> GetByRoleAsync(string role)
    {
        return await _context.Users
            .Where(u => u.Role == role.ToUpper())
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}

