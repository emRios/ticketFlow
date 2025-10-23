using TicketFlow.Application.Interfaces;
using TicketFlow.Application.Interfaces.Repositories;
using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.UseCases.Tickets.Commands;

/// <summary>
/// Handler para asignar un ticket a un agente
/// Valida que el ticket esté en estado válido (nuevo o en-proceso)
/// </summary>
public class AssignTicketHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTicketHandler(
        ITicketRepository ticketRepository, 
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(AssignTicketCommand command, CancellationToken ct = default)
    {
        // 0. Verificar permisos del actor (ADMIN: todos; AGENT: si el ticket está asignado a él)
        var actor = await _userRepository.GetByIdAsync(command.PerformedBy);
        if (actor == null)
        {
            return Result.Failure("No tienes permisos para reasignar este ticket");
        }

        // 1. Obtener ticket
        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);
        if (ticket == null)
        {
            return Result.Failure("Ticket no encontrado");
        }

        // 2. Validar estado: solo reasignable si está en nuevo o en-proceso
        var allowedStatuses = new[] { "nuevo", "en-proceso" };
        if (!allowedStatuses.Contains(ticket.Status.ToLowerInvariant()))
        {
            return Result.Failure($"No se puede reasignar un ticket en estado '{ticket.Status}'. Solo se permite en: nuevo, en-proceso.");
        }

        // 2.1. Validar permisos por rol
        var isAdmin = actor.Role == "ADMIN";
        var isAgent = actor.Role == "AGENT";
        if (!isAdmin)
        {
            // Agente solo puede reasignar si el ticket está asignado a él
            if (!(isAgent && string.Equals(ticket.AssignedTo, actor.Id.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure("No tienes permisos para reasignar este ticket");
            }
        }

        // 3. Verificar que el assignee exista y sea un agente
        var assignee = await _userRepository.GetByIdAsync(command.AssigneeId);
        if (assignee == null)
        {
            return Result.Failure("El usuario asignado no existe");
        }

        if (assignee.Role != "AGENT")
        {
            return Result.Failure("Solo se puede asignar tickets a usuarios con rol AGENT");
        }

    // 4. Asignar (emite TicketAssignedEvent)
    ticket.AssignTo(command.AssigneeId.ToString(), command.PerformedBy.ToString(), command.Reason);

        // 5. Persistir
        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        return Result.Success();
    }
}

/// <summary>
/// Resultado de la operación
/// </summary>
public record Result
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}
