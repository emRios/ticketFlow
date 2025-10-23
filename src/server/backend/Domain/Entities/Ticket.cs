using System.Text.RegularExpressions;
using TicketFlow.Domain.Events;

namespace TicketFlow.Domain.Entities;

/// <summary>
/// Entidad principal del dominio - Ticket
/// </summary>
public partial class Ticket : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Campos simples para soportar UI actual (sin enums por simplicidad en EF/dev)
    public string Status { get; private set; } = "nuevo";
    public string Priority { get; private set; } = "MEDIUM";
    public string? AssignedTo { get; private set; }
    
    /// <summary>
    /// Columna para control optimista de concurrencia en asignación automática.
    /// Se incrementa en cada UPDATE para detectar colisiones.
    /// </summary>
    public int Version { get; private set; } = 0;
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructor privado para EF Core
    private Ticket() { }
    
    // Factory method
    public static Ticket Create(string title, string description, string? priority = null, string? createdBy = null)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Status = "nuevo",
            Priority = string.IsNullOrWhiteSpace(priority) ? "MEDIUM" : priority.Trim().ToUpperInvariant()
        };
        
        // Generar evento de dominio
        ticket._domainEvents.Add(new TicketCreatedEvent
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            PerformedBy = createdBy ?? string.Empty,
            OccurredAt = ticket.CreatedAt
        });
        
        return ticket;
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void ChangeStatus(string newStatus, string? performedBy = null, string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(newStatus)) return;
        var old = Status;
        Status = CanonicalizeStatus(newStatus);
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new TicketStatusChangedEvent
        {
            TicketId = Id,
            OldStatus = old,
            NewStatus = Status,
            PerformedBy = performedBy ?? string.Empty,
            OccurredAt = UpdatedAt ?? DateTime.UtcNow,
            Comment = comment
        });
    }

    public void AssignTo(string assigneeId, string? performedBy = null, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(assigneeId)) return;
        AssignedTo = assigneeId;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new TicketAssignedEvent
        {
            TicketId = Id,
            AssigneeId = assigneeId,
            PerformedBy = performedBy ?? string.Empty,
            OccurredAt = UpdatedAt ?? DateTime.UtcNow,
            Reason = reason
        });
    }
}

// Helpers de normalización de estado
partial class Ticket
{
    private static readonly Dictionary<string, string> StatusMap = new()
    {
        // Español (canónico)
        ["nuevo"] = "nuevo",
        ["enproceso"] = "en-proceso",
        ["en-espera"] = "en-espera",
        ["enespera"] = "en-espera",
        ["resuelto"] = "resuelto",

        // Variantes con guiones/underscores
        ["en-proceso"] = "en-proceso",
        ["en_proceso"] = "en-proceso",
        ["on_hold"] = "en-espera",
        ["on-hold"] = "en-espera",

        // Inglés comunes → Español
        ["open"] = "nuevo",
        ["new"] = "nuevo",
        ["inprogress"] = "en-proceso",
        ["in-progress"] = "en-proceso",
        ["in_progress"] = "en-proceso",
        ["onhold"] = "en-espera",
        ["resolved"] = "resuelto",
        ["closed"] = "resuelto"
    };

    private static string CanonicalizeStatus(string input)
    {
        var raw = input.Trim().ToLowerInvariant();
        // Normalizar quitando caracteres no alfanuméricos para buscar en el mapa
        var key = Regex.Replace(raw, "[^a-z0-9]", "");
        if (StatusMap.TryGetValue(key, out var canonical))
        {
            return canonical;
        }

        // Si la clave exacta (incluyendo guiones/underscores) existe, usarla
        if (StatusMap.TryGetValue(raw, out canonical))
        {
            return canonical;
        }

        // Fallback: devolver minúsculas y reemplazar '_' por '-' para consistencia visual
        return raw.Replace('_', '-');
    }
}
