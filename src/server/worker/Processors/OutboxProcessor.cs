using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketFlow.Infrastructure.Outbox;
using TicketFlow.Infrastructure.Persistence;
using TicketFlow.Worker.Messaging;

namespace TicketFlow.Worker.Processors;

/// <summary>
/// Procesador de mensajes Outbox con soporte para PostgreSQL advisory locks,
/// verificación de idempotencia y publicación a RabbitMQ
/// </summary>
public class OutboxProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IMessagePublisher _messagePublisher;
    
    // Lock ID 42 como especificado (pg_try_advisory_lock(42))
    private const int LockId = 42;
    
    // Número de mensajes a procesar por lote
    private const int BatchSize = 50;
    
    // Máximo número de reintentos antes de marcar como fallido permanente
    private const int MaxAttempts = 5;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        IMessagePublisher messagePublisher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    /// <summary>
    /// Bucle principal de procesamiento del Outbox
    /// </summary>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TicketFlowDbContext>();

        // 1. Intentar adquirir lock advisory de PostgreSQL (pg_try_advisory_lock(42))
        var lockAcquired = await TryAcquireAdvisoryLockAsync(context, cancellationToken);
        
        if (!lockAcquired)
        {
            _logger.LogDebug("No se pudo adquirir el lock (42). Otro worker está procesando.");
            return;
        }

        try
        {
            _logger.LogDebug("Lock advisory (42) adquirido. Procesando mensajes Outbox...");

            // 2. Leer mensajes pendientes con FOR UPDATE SKIP LOCKED
            var pendingMessages = await GetPendingMessagesWithLockAsync(context, cancellationToken);

            if (!pendingMessages.Any())
            {
                _logger.LogDebug("No hay mensajes pendientes en el Outbox.");
                return;
            }

            _logger.LogInformation(
                "Procesando {Count} mensajes del Outbox (Lock ID: {LockId})", 
                pendingMessages.Count, 
                LockId);

            var successCount = 0;
            var failureCount = 0;
            var skippedCount = 0;

            // 3. Procesar cada mensaje
            foreach (var message in pendingMessages)
            {
                try
                {
                    // 4. Verificar idempotencia - ¿Ya se procesó este evento?
                    var alreadyProcessed = await IsAlreadyProcessedAsync(context, message.Id, cancellationToken);
                    
                    if (alreadyProcessed)
                    {
                        _logger.LogWarning(
                            "Mensaje {MessageId} ya fue procesado (idempotencia). Marcando como despachado.",
                            message.Id);
                        
                        await MarkAsDispatchedAsync(context, message.Id, cancellationToken);
                        skippedCount++;
                        continue;
                    }

                    // 5. Publicar a RabbitMQ
                    await _messagePublisher.PublishAsync(
                        message.Type,
                        message.PayloadJson,
                        message.CorrelationId,
                        cancellationToken);

                    _logger.LogInformation(
                        "✅ Mensaje publicado: {Type} (ID: {Id}, CorrelationId: {CorrelationId})",
                        message.Type,
                        message.Id,
                        message.CorrelationId ?? "N/A");

                    // 6. Marcar como despachado y registrar en ProcessedEvents
                    await MarkAsDispatchedAsync(context, message.Id, cancellationToken);
                    await RecordProcessedEventAsync(context, message.Id, cancellationToken);

                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Error al procesar mensaje {Id} del tipo {Type}. Intento {Attempts}/{Max}",
                        message.Id,
                        message.Type,
                        message.Attempts + 1,
                        MaxAttempts);

                    // 7. Incrementar intentos y registrar error (sin marcar DispatchedAt)
                    await IncrementAttemptsAsync(
                        context, 
                        message.Id, 
                        ex.Message, 
                        cancellationToken);

                    failureCount++;
                }
            }

            _logger.LogInformation(
                "Procesamiento completado. Exitosos: {Success}, Fallidos: {Failed}, Omitidos: {Skipped}",
                successCount,
                failureCount,
                skippedCount);
        }
        finally
        {
            // 8. Siempre liberar el lock advisory
            await ReleaseAdvisoryLockAsync(context, cancellationToken);
            _logger.LogDebug("Lock advisory (42) liberado.");
        }
    }

    /// <summary>
    /// Lee mensajes pendientes usando:
    /// WHERE DispatchedAt IS NULL AND Attempts < MaxAttempts
    /// ORDER BY OccurredAt
    /// LIMIT BatchSize
    /// FOR UPDATE SKIP LOCKED
    /// </summary>
    private async Task<List<OutboxMessage>> GetPendingMessagesWithLockAsync(
        TicketFlowDbContext context,
        CancellationToken cancellationToken)
    {
        // Usar SQL raw para FOR UPDATE SKIP LOCKED (no soportado directamente en LINQ)
        var messages = await context.OutboxMessages
            .FromSqlRaw(@"
                SELECT * FROM ""OutboxMessages""
                WHERE ""DispatchedAt"" IS NULL 
                  AND ""Attempts"" < {0}
                ORDER BY ""OccurredAt""
                LIMIT {1}
                FOR UPDATE SKIP LOCKED",
                MaxAttempts,
                BatchSize)
            .ToListAsync(cancellationToken);

        return messages;
    }

    /// <summary>
    /// Verifica si el evento ya fue procesado (tabla ProcessedEvents)
    /// </summary>
    private async Task<bool> IsAlreadyProcessedAsync(
        TicketFlowDbContext context,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await context.ProcessedEvents
            .AnyAsync(pe => pe.EventId == eventId, cancellationToken);
    }

    /// <summary>
    /// Marca el mensaje como despachado (DispatchedAt = NOW)
    /// </summary>
    private async Task MarkAsDispatchedAsync(
        TicketFlowDbContext context,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE ""OutboxMessages""
            SET ""DispatchedAt"" = {0}
            WHERE ""Id"" = {1}",
            DateTime.UtcNow,
            messageId,
            cancellationToken);
    }

    /// <summary>
    /// Registra el evento en ProcessedEvents para idempotencia
    /// </summary>
    private async Task RecordProcessedEventAsync(
        TicketFlowDbContext context,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var processedEvent = new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAt = DateTime.UtcNow
        };

        context.ProcessedEvents.Add(processedEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Incrementa el contador de intentos y registra el error
    /// NO marca DispatchedAt para que se reintente después
    /// </summary>
    private async Task IncrementAttemptsAsync(
        TicketFlowDbContext context,
        Guid messageId,
        string error,
        CancellationToken cancellationToken)
    {
        // Truncar error a 2000 caracteres (límite de la columna)
        var truncatedError = error.Length > 2000 ? error[..2000] : error;

        await context.Database.ExecuteSqlRawAsync(@"
            UPDATE ""OutboxMessages""
            SET ""Attempts"" = ""Attempts"" + 1,
                ""Error"" = {0}
            WHERE ""Id"" = {1}",
            truncatedError,
            messageId,
            cancellationToken);
    }

    /// <summary>
    /// Intenta adquirir un advisory lock de PostgreSQL
    /// pg_try_advisory_lock(42) retorna true si se adquiere, false si ya está tomado
    /// </summary>
    private async Task<bool> TryAcquireAdvisoryLockAsync(
        TicketFlowDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Usar ADO.NET directamente para evitar problemas con EF Core's SqlQuery
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT pg_try_advisory_lock({LockId})";
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is bool lockAcquired && lockAcquired;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, 
                "Error al intentar adquirir advisory lock (ID: {LockId}). Asumiendo lock no disponible.", 
                LockId);
            return false;
        }
    }

    /// <summary>
    /// Libera el advisory lock de PostgreSQL
    /// </summary>
    private async Task ReleaseAdvisoryLockAsync(
        TicketFlowDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.Database
                .ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({LockId})", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al liberar advisory lock (ID: {LockId}).", LockId);
        }
    }
}
