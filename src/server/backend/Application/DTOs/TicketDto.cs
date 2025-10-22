namespace TicketFlow.Application.DTOs;

/// <summary>
/// DTO para respuesta de ticket
/// </summary>
public record TicketDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
