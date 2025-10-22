using TicketFlow.Domain.Events;

namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad principal del dominio - Ticket
/// </summary>
public class Ticket : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructor privado para EF Core
    private Ticket() { }
    
    // Factory method
    public static Ticket Create(string title, string description)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        
        // Generar evento de dominio
        ticket._domainEvents.Add(new TicketCreatedEvent
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            OccurredAt = ticket.CreatedAt
        });
        
        return ticket;
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
