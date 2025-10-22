namespace TicketFlow.Domain.ValueObjects;

/// <summary>
/// Value Object - Estado del ticket
/// </summary>
public record TicketStatus
{
    public string Value { get; init; }

    private TicketStatus(string value) => Value = value;

    public static TicketStatus Todo => new("todo");
    public static TicketStatus InProgress => new("in-progress");
    public static TicketStatus Done => new("done");

    // TODO: Implementar validaciones y transiciones FSM
}
