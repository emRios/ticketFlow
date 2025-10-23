using Microsoft.EntityFrameworkCore;
using TicketFlow.Domain.Entities;
using TicketFlow.Infrastructure.Outbox;
using TicketFlow.Domain.Events;
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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Aplicar configuraciones del Outbox
    modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
    modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        
        // Aplicar configuraciones de entidades
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.TicketConfiguration());
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.UserConfiguration());
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.TicketActivityConfiguration());
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

        // 6. Generar registros de auditoría
    var auditLogs = await MapEventsToAuditLogsAsync(domainEvents, correlationId, cancellationToken);

        // 7. Generar registros de actividad de tickets
        var ticketActivities = await MapEventsToTicketActivitiesAsync(domainEvents, correlationId, cancellationToken);

        // 8. Agregar OutboxMessages, AuditLogs y TicketActivities al contexto en la misma transacción
        await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        await AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        await TicketActivities.AddRangeAsync(ticketActivities, cancellationToken);

        // 9. Guardar todo (entidades + outbox + auditoría + actividad) en una sola transacción
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

        // 5. Generar registros de auditoría
    var auditLogs = MapEventsToAuditLogs(domainEvents, correlationId);

        // 6. Generar registros de actividad de tickets
        var ticketActivities = MapEventsToTicketActivities(domainEvents, correlationId);

        // 7. Agregar OutboxMessages, AuditLogs y TicketActivities al contexto
        OutboxMessages.AddRange(outboxMessages);
        AuditLogs.AddRange(auditLogs);
        TicketActivities.AddRange(ticketActivities);

        // 8. Guardar todo en una transacción
        return base.SaveChanges();
    }

    private IEnumerable<AuditLog> MapEventsToAuditLogs(IEnumerable<IDomainEvent> domainEvents, string correlationId)
    {
        var performedByIds = domainEvents
            .Select(e => e switch
            {
                TicketFlow.Domain.Events.TicketCreatedEvent ce => ce.PerformedBy,
                TicketFlow.Domain.Events.TicketAssignedEvent ae => ae.PerformedBy,
                TicketFlow.Domain.Events.TicketStatusChangedEvent se => se.PerformedBy,
                _ => null
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList();

        var guidIds = new List<Guid>();
        foreach (var s in performedByIds)
        {
            if (Guid.TryParse(s, out var g)) guidIds.Add(g);
        }

        var namesById = Users
            .Where(u => guidIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToList()
            .ToDictionary(x => x.Id.ToString(), x => x.Name);

        var logs = new List<AuditLog>();
        foreach (var ev in domainEvents)
        {
            switch (ev)
            {
                case TicketFlow.Domain.Events.TicketCreatedEvent created:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = created.TicketId.ToString(),
                        Action = "TicketCreated",
                        PerformedBy = created.PerformedBy,
                        PerformedByName = created.PerformedBy != null && namesById.TryGetValue(created.PerformedBy, out var name1) ? name1 : null,
                        OccurredAt = created.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { created.Title })
                    });
                    break;
                case TicketFlow.Domain.Events.TicketStatusChangedEvent status:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = status.TicketId.ToString(),
                        Action = "TicketStatusChanged",
                        PerformedBy = status.PerformedBy,
                        PerformedByName = status.PerformedBy != null && namesById.TryGetValue(status.PerformedBy, out var name2) ? name2 : null,
                        OccurredAt = status.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { status.OldStatus, status.NewStatus, status.Comment })
                    });
                    break;
                case TicketFlow.Domain.Events.TicketAssignedEvent assigned:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = assigned.TicketId.ToString(),
                        Action = "TicketAssigned",
                        PerformedBy = assigned.PerformedBy,
                        PerformedByName = assigned.PerformedBy != null && namesById.TryGetValue(assigned.PerformedBy, out var name3) ? name3 : null,
                        OccurredAt = assigned.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { assigned.AssigneeId, assigned.Reason })
                    });
                    break;
            }
        }
        return logs;
    }

    private async Task<IEnumerable<AuditLog>> MapEventsToAuditLogsAsync(IEnumerable<IDomainEvent> domainEvents, string correlationId, CancellationToken cancellationToken)
    {
        var performedByIds = domainEvents
            .Select(e => e switch
            {
                TicketFlow.Domain.Events.TicketCreatedEvent ce => ce.PerformedBy,
                TicketFlow.Domain.Events.TicketAssignedEvent ae => ae.PerformedBy,
                TicketFlow.Domain.Events.TicketStatusChangedEvent se => se.PerformedBy,
                _ => null
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList();

        var guidIds = new List<Guid>();
        foreach (var s in performedByIds)
        {
            if (Guid.TryParse(s, out var g)) guidIds.Add(g);
        }

        var namePairs = await Users
            .Where(u => guidIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToListAsync(cancellationToken);
        var namesById = namePairs.ToDictionary(x => x.Id.ToString(), x => x.Name);

        var logs = new List<AuditLog>();
        foreach (var ev in domainEvents)
        {
            switch (ev)
            {
                case TicketFlow.Domain.Events.TicketCreatedEvent created:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = created.TicketId.ToString(),
                        Action = "TicketCreated",
                        PerformedBy = created.PerformedBy,
                        PerformedByName = created.PerformedBy != null && namesById.TryGetValue(created.PerformedBy, out var name1) ? name1 : null,
                        OccurredAt = created.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { created.Title })
                    });
                    break;
                case TicketFlow.Domain.Events.TicketStatusChangedEvent status:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = status.TicketId.ToString(),
                        Action = "TicketStatusChanged",
                        PerformedBy = status.PerformedBy,
                        PerformedByName = status.PerformedBy != null && namesById.TryGetValue(status.PerformedBy, out var name2) ? name2 : null,
                        OccurredAt = status.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { status.OldStatus, status.NewStatus, status.Comment })
                    });
                    break;
                case TicketFlow.Domain.Events.TicketAssignedEvent assigned:
                    logs.Add(new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityType = "Ticket",
                        EntityId = assigned.TicketId.ToString(),
                        Action = "TicketAssigned",
                        PerformedBy = assigned.PerformedBy,
                        PerformedByName = assigned.PerformedBy != null && namesById.TryGetValue(assigned.PerformedBy, out var name3) ? name3 : null,
                        OccurredAt = assigned.OccurredAt,
                        CorrelationId = correlationId,
                        Data = System.Text.Json.JsonSerializer.Serialize(new { assigned.AssigneeId, assigned.Reason })
                    });
                    break;
            }
        }
        return logs;
    }

    private IEnumerable<TicketActivity> MapEventsToTicketActivities(IEnumerable<IDomainEvent> domainEvents, string correlationId)
    {
        var performedByIds = domainEvents
            .Select(e => e switch
            {
                TicketFlow.Domain.Events.TicketCreatedEvent ce => ce.PerformedBy,
                TicketFlow.Domain.Events.TicketAssignedEvent ae => ae.PerformedBy,
                TicketFlow.Domain.Events.TicketStatusChangedEvent se => se.PerformedBy,
                _ => null
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList();

        var assigneeIds = domainEvents
            .OfType<TicketFlow.Domain.Events.TicketAssignedEvent>()
            .Select(e => e.AssigneeId)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        var allUserIdStrings = new HashSet<string>(performedByIds);
        foreach (var aid in assigneeIds)
        {
            allUserIdStrings.Add(aid);
        }

        var allUserIds = new List<Guid>();
        foreach (var s in allUserIdStrings)
        {
            if (Guid.TryParse(s, out var g)) allUserIds.Add(g);
        }

        var namesById = Users
            .Where(u => allUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToList()
            .ToDictionary(x => x.Id.ToString(), x => x.Name);

        var activities = new List<TicketActivity>();
        foreach (var ev in domainEvents)
        {
            switch (ev)
            {
                case TicketFlow.Domain.Events.TicketCreatedEvent created:
                    activities.Add(TicketActivity.ForCreated(
                        ticketId: created.TicketId,
                        title: created.Title,
                        performedBy: created.PerformedBy,
                        performedByName: created.PerformedBy != null && namesById.TryGetValue(created.PerformedBy, out var name1) ? name1 : null,
                        correlationId: correlationId,
                        occurredAt: created.OccurredAt
                    ));
                    break;
                case TicketFlow.Domain.Events.TicketAssignedEvent assigned:
                    var assigneeName = namesById.TryGetValue(assigned.AssigneeId, out var aName) ? aName : null;
                    activities.Add(TicketActivity.ForAssigned(
                        ticketId: assigned.TicketId,
                        assigneeId: assigned.AssigneeId,
                        assigneeName: assigneeName,
                        reason: assigned.Reason,
                        performedBy: assigned.PerformedBy,
                        performedByName: assigned.PerformedBy != null && namesById.TryGetValue(assigned.PerformedBy, out var name2) ? name2 : null,
                        correlationId: correlationId,
                        occurredAt: assigned.OccurredAt
                    ));
                    break;
                case TicketFlow.Domain.Events.TicketStatusChangedEvent status:
                    activities.Add(TicketActivity.ForStatusChanged(
                        ticketId: status.TicketId,
                        oldStatus: status.OldStatus,
                        newStatus: status.NewStatus,
                        comment: status.Comment,
                        performedBy: status.PerformedBy,
                        performedByName: status.PerformedBy != null && namesById.TryGetValue(status.PerformedBy, out var name3) ? name3 : null,
                        correlationId: correlationId,
                        occurredAt: status.OccurredAt
                    ));
                    break;
            }
        }
        return activities;
    }

    private async Task<IEnumerable<TicketActivity>> MapEventsToTicketActivitiesAsync(IEnumerable<IDomainEvent> domainEvents, string correlationId, CancellationToken cancellationToken)
    {
        var performedByIds = domainEvents
            .Select(e => e switch
            {
                TicketFlow.Domain.Events.TicketCreatedEvent ce => ce.PerformedBy,
                TicketFlow.Domain.Events.TicketAssignedEvent ae => ae.PerformedBy,
                TicketFlow.Domain.Events.TicketStatusChangedEvent se => se.PerformedBy,
                _ => null
            })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct()
            .ToList();

        var assigneeIds = domainEvents
            .OfType<TicketFlow.Domain.Events.TicketAssignedEvent>()
            .Select(e => e.AssigneeId)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct();
        
        var allUserIdStrings = new HashSet<string>(performedByIds);
        foreach (var aid in assigneeIds)
        {
            allUserIdStrings.Add(aid);
        }

        var allUserIds = new List<Guid>();
        foreach (var s in allUserIdStrings)
        {
            if (Guid.TryParse(s, out var g)) allUserIds.Add(g);
        }

        var namePairs = await Users
            .Where(u => allUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToListAsync(cancellationToken);
        var namesById = namePairs.ToDictionary(x => x.Id.ToString(), x => x.Name);

        var activities = new List<TicketActivity>();
        foreach (var ev in domainEvents)
        {
            switch (ev)
            {
                case TicketFlow.Domain.Events.TicketCreatedEvent created:
                    activities.Add(TicketActivity.ForCreated(
                        ticketId: created.TicketId,
                        title: created.Title,
                        performedBy: created.PerformedBy,
                        performedByName: created.PerformedBy != null && namesById.TryGetValue(created.PerformedBy, out var name1) ? name1 : null,
                        correlationId: correlationId,
                        occurredAt: created.OccurredAt
                    ));
                    break;
                case TicketFlow.Domain.Events.TicketAssignedEvent assigned:
                    var assigneeName = namesById.TryGetValue(assigned.AssigneeId, out var aName) ? aName : null;
                    activities.Add(TicketActivity.ForAssigned(
                        ticketId: assigned.TicketId,
                        assigneeId: assigned.AssigneeId,
                        assigneeName: assigneeName,
                        reason: assigned.Reason,
                        performedBy: assigned.PerformedBy,
                        performedByName: assigned.PerformedBy != null && namesById.TryGetValue(assigned.PerformedBy, out var name2) ? name2 : null,
                        correlationId: correlationId,
                        occurredAt: assigned.OccurredAt
                    ));
                    break;
                case TicketFlow.Domain.Events.TicketStatusChangedEvent status:
                    activities.Add(TicketActivity.ForStatusChanged(
                        ticketId: status.TicketId,
                        oldStatus: status.OldStatus,
                        newStatus: status.NewStatus,
                        comment: status.Comment,
                        performedBy: status.PerformedBy,
                        performedByName: status.PerformedBy != null && namesById.TryGetValue(status.PerformedBy, out var name3) ? name3 : null,
                        correlationId: correlationId,
                        occurredAt: status.OccurredAt
                    ));
                    break;
            }
        }
        return activities;
    }
}
