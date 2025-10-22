namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad de usuario del sistema
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    
    private User() { }
    
    public static User Create(string email, string name)
    {
        // TODO: Implementar validaciones
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name
        };
    }
}
