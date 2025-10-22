namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad principal del dominio - Ticket
/// </summary>
public class Ticket
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    
    // Constructor privado para EF Core
    private Ticket() { }
    
    // Factory method
    public static Ticket Create(string title, string description)
    {
        // TODO: Implementar validaciones e invariantes
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
