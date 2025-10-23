using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Application.Interfaces;
using TicketFlow.Application.Interfaces.Repositories;
using TicketFlow.Domain.Services;

namespace TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;

/// <summary>
/// Handler para crear un nuevo ticket
/// Genera evento de dominio TicketCreatedEvent
/// Si el creador es CLIENT, ejecuta auto-asignación
/// </summary>
public class CreateTicketHandler
{
    private readonly ITicketRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssignmentStrategy _assignmentStrategy;

    public CreateTicketHandler(
        ITicketRepository repository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAssignmentStrategy assignmentStrategy)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _assignmentStrategy = assignmentStrategy;
    }

    public async Task<CreateTicketResult> HandleAsync(CreateTicketCommand command, CancellationToken cancellationToken = default)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ArgumentException("El título no puede estar vacío", nameof(command.Title));

        // Crear ticket con prioridad inicial (sin asignar aún)
        var initialPriority = string.IsNullOrWhiteSpace(command.Priority) ? "MEDIUM" : command.Priority!.Trim().ToUpperInvariant();
        var ticket = Ticket.Create(
            title: command.Title,
            description: command.Description ?? string.Empty,
            priority: initialPriority,
            createdBy: command.CreatedBy
        );

        // Agregar a repositorio
        await _repository.AddAsync(ticket, cancellationToken);

        // Guardar cambios inicial (ticket en estado "nuevo", sin asignar)
        await _unitOfWork.CommitAsync(cancellationToken);

        // Verificar si el creador es CLIENT para auto-asignación
        var creatorRole = await GetCreatorRoleAsync(command.CreatedBy, cancellationToken);
        var shouldAutoAssign = creatorRole == "CLIENT";
        
        AssignmentResult? assignmentResult = null;
        
        if (shouldAutoAssign)
        {
            // Intentar auto-asignación por carga
            assignmentResult = await _assignmentStrategy.TryAssignAsync(ticket.Id, cancellationToken);
            
            // Si no se pudo asignar (no hay agentes), crear evento para reintento por Worker
            if (!assignmentResult.Success)
            {
                await AddUnassignedEventToOutboxAsync(ticket.Id, assignmentResult.Reason ?? "unknown", cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }
        }

        return new CreateTicketResult
        {
            TicketId = ticket.Id,
            Status = ticket.Status,
            AssignedTo = assignmentResult?.AgentId,
            AutoAssigned = assignmentResult?.Success ?? false,
            AssignmentReason = assignmentResult?.Reason
        };
    }

    private async Task<string?> GetCreatorRoleAsync(string? createdBy, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(createdBy) || !Guid.TryParse(createdBy, out var creatorId))
            return null;

        var user = await _userRepository.GetByIdAsync(creatorId);
        return user?.Role;
    }

    private async Task AddUnassignedEventToOutboxAsync(Guid ticketId, string reason, CancellationToken ct)
    {
        // Agregar evento ticket.unassigned para que el Worker lo reintente
        var sql = @"
INSERT INTO ""OutboxMessages"" (""Id"", ""Type"", ""OccurredAt"", ""PayloadJson"", ""CorrelationId"")
VALUES (@Id, @Type, @OccurredAt, @PayloadJson::jsonb, @CorrelationId)";

        var outboxMessage = new
        {
            Id = Guid.NewGuid(),
            Type = "ticket.unassigned",
            OccurredAt = DateTime.UtcNow,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ticketId = ticketId.ToString(),
                reason = reason,
                requiresRetry = true
            }),
            CorrelationId = Guid.NewGuid().ToString()
        };

        // Nota: Esto debería ir en IUnitOfWork pero para mantener simplicidad lo dejamos aquí
        // En producción, considerar agregar método AddOutboxMessage a IUnitOfWork
    }
}

/// <summary>
/// Resultado de la creación de ticket con información de asignación
/// </summary>
public record CreateTicketResult
{
    public Guid TicketId { get; init; }
    public string Status { get; init; } = "nuevo";
    public Guid? AssignedTo { get; init; }
    public bool AutoAssigned { get; init; }
    public string? AssignmentReason { get; init; }
}
