using Microsoft.EntityFrameworkCore;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Infrastructure.Outbox;

/// <summary>
/// Implementación del repositorio Outbox con soporte para PostgreSQL advisory locks
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly TicketFlowDbContext _context;

    public OutboxRepository(TicketFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        // Obtener mensajes no procesados, ordenados por fecha
        // Limitar reintentos a 5 para evitar loops infinitos
        return await _context.OutboxMessages
            .Where(m => m.DispatchedAt == null && m.Attempts < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.DispatchedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.Attempts++;
            message.Error = error;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
