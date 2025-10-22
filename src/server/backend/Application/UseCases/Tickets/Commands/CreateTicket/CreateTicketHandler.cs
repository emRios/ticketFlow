using TicketFlow.Domain.Entities;
using TicketFlow.Domain.Enums;
using TicketFlow.Application.Interfaces;

namespace TicketFlow.Application.UseCases.Tickets.Commands.CreateTicket;

/// <summary>
/// Handler para crear un nuevo ticket
/// Genera evento de dominio TicketCreatedEvent
/// </summary>
public class CreateTicketHandler
{
    private readonly ITicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketHandler(
        ITicketRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Ticket> HandleAsync(CreateTicketCommand command, CancellationToken cancellationToken = default)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ArgumentException("El título no puede estar vacío", nameof(command.Title));

        // Crear ticket (solo con Title y Description que son las propiedades que existen)
        var ticket = Ticket.Create(
            title: command.Title,
            description: command.Description ?? string.Empty
        );

        // Agregar a repositorio
        await _repository.AddAsync(ticket, cancellationToken);

        // Guardar cambios (esto dispara el interceptor de Outbox)
        await _unitOfWork.CommitAsync(cancellationToken);

        return ticket;
    }
}
