namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad de usuario del sistema
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = "AGENT"; // ADMIN, AGENT, CLIENT
    
    /// <summary>
    /// Indica si el usuario está activo para recibir asignaciones automáticas.
    /// Solo aplica para agentes.
    /// </summary>
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// Última vez que se le asignó un ticket (para desempates en load balancing).
    /// NULLS FIRST = agentes sin asignaciones previas tienen prioridad.
    /// </summary>
    public DateTime? LastAssignedAt { get; private set; }
    
    private User() { }
    
    public static User Create(string email, string name, string role = "AGENT")
    {
        // TODO: Implementar validaciones
        var validRoles = new[] { "ADMIN", "AGENT", "CLIENT" };
        if (!validRoles.Contains(role.ToUpperInvariant()))
        {
            throw new ArgumentException($"Rol inválido: {role}. Permitidos: ADMIN, AGENT, CLIENT");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            Role = role.ToUpperInvariant()
        };
    }

    public void Update(string? name = null, string? email = null, string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Name = name;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            Email = email;
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var validRoles = new[] { "ADMIN", "AGENT", "CLIENT" };
            var roleUpper = role.ToUpperInvariant();
            if (!validRoles.Contains(roleUpper))
            {
                throw new ArgumentException($"Rol inválido: {role}. Permitidos: ADMIN, AGENT, CLIENT");
            }
            Role = roleUpper;
        }
    }
}
