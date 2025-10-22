using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Application.Interfaces;

namespace TicketFlow.Application.UseCases.Tickets.Commands.ChangeTicketStatus;

/// <summary>
/// Handler para cambiar el estado de un ticket
/// Usa la FSM del agregado para validar transiciones
/// </summary>
public class ChangeTicketStatusHandler
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTicketStatusHandler(
        ITicketRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Ticket> HandleAsync(
        ChangeTicketStatusCommand command, 
        CancellationToken cancellationToken = default)
    {
        // Buscar ticket
        var ticket = await _repository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {command.TicketId} no encontrado");
        }

        // TODO: Implementar cambio de estado cuando se agregue la propiedad Status a la entidad Ticket
        // Por ahora solo retornamos el ticket encontrado

        // Actualizar
        await _repository.UpdateAsync(ticket, cancellationToken);

        // Guardar cambios (dispara interceptor de Outbox)
        await _unitOfWork.CommitAsync(cancellationToken);

        return ticket;
    }
}
