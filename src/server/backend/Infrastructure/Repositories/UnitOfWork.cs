using TicketFlow.Application.Interfaces;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Infrastructure.Repositories;

/// <summary>
/// Implementación del Unit of Work usando EF Core DbContext
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TicketFlowDbContext _context;

    public UnitOfWork(TicketFlowDbContext context)
    {
        _context = context;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        // SaveChangesAsync dispara automáticamente el interceptor de Outbox
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
