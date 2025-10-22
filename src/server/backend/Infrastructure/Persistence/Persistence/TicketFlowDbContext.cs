using Microsoft.EntityFrameworkCore;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Outbox;
using TicketFlow.Infrastructure.Persistence.EfCore.OutboxStore;

namespace TicketFlow.Infrastructure.Persistence;

/// <summary>
/// DbContext principal con soporte para patrón Outbox
/// </summary>
public class TicketFlowDbContext : DbContext
{
    public TicketFlowDbContext(DbContextOptions<TicketFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar configuraciones del Outbox
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
        
        // Aplicar configuraciones de entidades
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.TicketConfiguration());
    }

    /// <summary>
    /// Intercepta el guardado para convertir eventos de dominio en OutboxMessages
    /// Garantiza que los eventos se persistan en la misma transacción que los cambios de entidades
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Detectar entidades que tienen eventos de dominio pendientes
        var entitiesWithEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .Select(entry => entry.Entity)
            .ToList();

        // 2. Extraer todos los eventos de dominio
        var domainEvents = entitiesWithEvents
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents(); // Limpiar eventos después de extraerlos
                return events;
            })
            .ToList();

        // 3. Si no hay eventos, guardar normalmente
        if (!domainEvents.Any())
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        // 4. Generar un CorrelationId único para este batch de eventos
        var correlationId = OutboxMapper.GenerateCorrelationId();

        // 5. Convertir eventos de dominio a OutboxMessages usando el mapper
        var outboxMessages = OutboxMapper.ToOutboxMessages(domainEvents, correlationId);

        // 6. Agregar OutboxMessages al contexto en la misma transacción
        await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);

        // 7. Guardar todo (entidades + outbox messages) en una sola transacción
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Versión sincrónica del interceptor de eventos
    /// </summary>
    public override int SaveChanges()
    {
        // 1. Detectar entidades que tienen eventos de dominio pendientes
        var entitiesWithEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .Select(entry => entry.Entity)
            .ToList();

        // 2. Extraer todos los eventos de dominio
        var domainEvents = entitiesWithEvents
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        // 3. Si no hay eventos, guardar normalmente
        if (!domainEvents.Any())
        {
            return base.SaveChanges();
        }

        // 4. Generar CorrelationId y convertir a OutboxMessages
        var correlationId = OutboxMapper.GenerateCorrelationId();
        var outboxMessages = OutboxMapper.ToOutboxMessages(domainEvents, correlationId);

        // 5. Agregar OutboxMessages al contexto
        OutboxMessages.AddRange(outboxMessages);

        // 6. Guardar todo en una transacción
        return base.SaveChanges();
    }
}
