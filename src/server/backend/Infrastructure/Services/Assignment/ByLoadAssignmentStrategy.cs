using Microsoft.EntityFrameworkCore;
using Npgsql;
using TicketFlow.Domain.Services;
using TicketFlow.Infrastructure.Persistence;

namespace TicketFlow.Infrastructure.Services.Assignment;

/// <summary>
/// Estrategia de asignación por carga de trabajo (load balancing).
/// Calcula load_score = open_count + 1.5 * in_progress_count por agente
/// y asigna al de menor carga con control optimista de concurrencia.
/// </summary>
public class ByLoadAssignmentStrategy : IAssignmentStrategy
{
    private readonly TicketFlowDbContext _context;
    private const decimal IN_PROGRESS_WEIGHT = 1.5m;
    private const int MAX_RETRIES = 1; // Reintenta una vez en caso de conflicto

    public ByLoadAssignmentStrategy(TicketFlowDbContext context)
    {
        _context = context;
    }

    public async Task<AssignmentResult> TryAssignAsync(Guid ticketId, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt <= MAX_RETRIES; attempt++)
        {
            // 1. Obtener agente con menor carga
            var selectedAgent = await GetAgentWithLowestLoadAsync(ct);
            
            if (selectedAgent == null)
            {
                return AssignmentResult.FailureResult("no_active_agents");
            }

            // 2. Intentar asignación con control optimista
            var assigned = await TryAssignWithOptimisticLockAsync(
                ticketId, 
                selectedAgent.AgentId, 
                ct);

            if (assigned)
            {
                // 3. Actualizar LastAssignedAt del agente
                await UpdateAgentLastAssignedAsync(selectedAgent.AgentId, ct);

                // 4. Agregar evento a Outbox
                await AddAssignmentEventToOutboxAsync(ticketId, selectedAgent.AgentId, ct);

                // 5. Commit transacción
                await _context.SaveChangesAsync(ct);

                return AssignmentResult.SuccessResult(selectedAgent.AgentId);
            }

            // Conflicto detectado, reintentar si quedan intentos
            if (attempt < MAX_RETRIES)
            {
                await Task.Delay(50, ct); // Pequeño backoff
                continue;
            }

            return AssignmentResult.FailureResult("conflict");
        }

        return AssignmentResult.FailureResult("max_retries_exceeded");
    }

    /// <summary>
    /// Ejecuta query SQL optimizada para obtener el agente con menor carga.
    /// load_score = open_count + 1.5 * in_progress_count
    /// Desempates: load_score ASC → LastAssignedAt NULLS FIRST → AgentId ASC
    /// </summary>
    private async Task<AgentLoadInfo?> GetAgentWithLowestLoadAsync(CancellationToken ct)
    {
        // Query SQL (basada en AgentLoadQuery.sql)
        // IMPORTANTE: Usar minúsculas para nombres de tabla en PostgreSQL
        // IMPORTANTE: assignedto es VARCHAR, necesitamos convertir GUID a string
        var sql = @"
WITH agent_base AS (
  SELECT u.id::text AS agent_id, u.lastassignedat
  FROM users u
  WHERE u.role = 'AGENT' AND u.isactive = TRUE
),
counts AS (
  SELECT
    t.assignedto AS agent_id,
    SUM(CASE WHEN t.status IN ('nuevo','en-proceso','en-espera') THEN 1 ELSE 0 END) AS open_count,
    SUM(CASE WHEN t.status = 'en-proceso' THEN 1 ELSE 0 END) AS in_progress_count
  FROM tickets t
  WHERE t.assignedto IS NOT NULL
  GROUP BY t.assignedto
)
SELECT 
  a.agent_id::uuid,
  COALESCE(c.open_count, 0)::int AS open_count,
  COALESCE(c.in_progress_count, 0)::int AS in_progress_count,
  (COALESCE(c.open_count, 0) + 1.5 * COALESCE(c.in_progress_count, 0))::numeric AS load_score,
  a.lastassignedat
FROM agent_base a
LEFT JOIN counts c ON c.agent_id = a.agent_id
ORDER BY load_score ASC, a.lastassignedat NULLS FIRST, a.agent_id ASC
LIMIT 1";

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            await command.Connection!.OpenAsync(ct);
        }

        using var reader = await command.ExecuteReaderAsync(ct);
        
        if (await reader.ReadAsync(ct))
        {
            return new AgentLoadInfo
            {
                AgentId = reader.GetGuid(0),
                OpenCount = reader.GetInt32(1),
                InProgressCount = reader.GetInt32(2),
                LoadScore = reader.GetDecimal(3)
            };
        }

        return null;
    }

    /// <summary>
    /// Intenta actualizar el ticket con control optimista usando la columna Version.
    /// UPDATE ... WHERE Version=@ExpectedVersion
    /// Retorna true si asignó (1 fila afectada), false si hubo conflicto (0 filas).
    /// </summary>
    private async Task<bool> TryAssignWithOptimisticLockAsync(
        Guid ticketId, 
        Guid agentId, 
        CancellationToken ct)
    {
        // Obtener versión actual del ticket
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == ticketId)
            .Select(t => new { t.Status, t.Version })
            .FirstOrDefaultAsync(ct);

        if (ticket == null)
        {
            return false; // Ticket no existe
        }

        // Solo asignar si está en estados asignables
        var assignableStatuses = new[] { "nuevo", "en-espera" };
        if (!assignableStatuses.Contains(ticket.Status.ToLowerInvariant()))
        {
            return false; // Estado no permite asignación
        }

        // UPDATE con control optimista
        // IMPORTANTE: assignedto es VARCHAR, convertir GUID a string
        var sql = @"
UPDATE tickets
SET assignedto = @AgentId::text,
    version = version + 1,
    updatedat = NOW()
WHERE id = @TicketId 
  AND status IN ('nuevo', 'en-espera')
  AND version = @ExpectedVersion";

        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            sql,
            new NpgsqlParameter("@AgentId", agentId),
            new NpgsqlParameter("@TicketId", ticketId),
            new NpgsqlParameter("@ExpectedVersion", ticket.Version));

        return rowsAffected > 0;
    }

    /// <summary>
    /// Actualiza LastAssignedAt del agente para desempates futuros
    /// </summary>
    private async Task UpdateAgentLastAssignedAsync(Guid agentId, CancellationToken ct)
    {
        var sql = @"
UPDATE users
SET lastassignedat = NOW()
WHERE id = @AgentId";

        await _context.Database.ExecuteSqlRawAsync(
            sql,
            new NpgsqlParameter("@AgentId", agentId));
    }

    /// <summary>
    /// Agrega evento ticket.assigned a la tabla Outbox para procesamiento asíncrono
    /// </summary>
    private async Task AddAssignmentEventToOutboxAsync(
        Guid ticketId, 
        Guid agentId, 
        CancellationToken ct)
    {
        var outboxMessage = new
        {
            Id = Guid.NewGuid(),
            Type = "ticket.assigned",
            OccurredAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ticketId = ticketId.ToString(),
                agentId = agentId.ToString(),
                assignedAt = DateTime.UtcNow,
                source = "auto_assignment"
            }),
            CorrelationId = Guid.NewGuid().ToString()
        };

        var sql = @"
INSERT INTO ""OutboxMessages"" (""Id"", ""Type"", ""OccurredAt"", ""PayloadJson"", ""CorrelationId"")
VALUES (@Id, @Type, @OccurredAt, CAST(@PayloadJson AS text), @CorrelationId)";

        await _context.Database.ExecuteSqlRawAsync(
            sql,
            new NpgsqlParameter("@Id", outboxMessage.Id),
            new NpgsqlParameter("@Type", outboxMessage.Type),
            new NpgsqlParameter("@OccurredAt", outboxMessage.OccurredAt),
            new NpgsqlParameter("@PayloadJson", outboxMessage.PayloadJson),
            new NpgsqlParameter("@CorrelationId", outboxMessage.CorrelationId));
    }

    private class AgentLoadInfo
    {
        public Guid AgentId { get; init; }
        public int OpenCount { get; init; }
        public int InProgressCount { get; init; }
        public decimal LoadScore { get; init; }
    }
}
