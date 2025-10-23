namespace TicketFlow.Domain.Services;

/// <summary>
/// Estrategia de asignación de tickets a agentes.
/// NO conoce detalles de SQL, EF Core ni infraestructura.
/// Contrato puro de dominio.
/// </summary>
public interface IAssignmentStrategy
{
    /// <summary>
    /// Intenta asignar un ticket a un agente según la estrategia implementada.
    /// </summary>
    /// <param name="ticketId">ID del ticket a asignar</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Resultado de la asignación con éxito, agente asignado y razón si falló</returns>
    Task<AssignmentResult> TryAssignAsync(Guid ticketId, CancellationToken ct = default);
}

/// <summary>
/// Resultado de un intento de asignación automática.
/// </summary>
public record AssignmentResult
{
    /// <summary>
    /// Indica si la asignación fue exitosa
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// ID del agente al que se asignó el ticket (null si no se asignó)
    /// </summary>
    public Guid? AgentId { get; init; }

    /// <summary>
    /// Razón del fallo si Success=false.
    /// Valores esperados: "no_active_agents", "conflict", "ticket_not_found", etc.
    /// </summary>
    public string? Reason { get; init; }

    public static AssignmentResult SuccessResult(Guid agentId) => 
        new() { Success = true, AgentId = agentId };

    public static AssignmentResult FailureResult(string reason) => 
        new() { Success = false, Reason = reason };
}
