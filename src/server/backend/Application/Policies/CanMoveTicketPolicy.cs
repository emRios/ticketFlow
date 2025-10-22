namespace TicketFlow.Application.Policies;

/// <summary>
/// Política de autorización - Solo el asignado puede mover el ticket
/// </summary>
public class CanMoveTicketPolicy
{
    // TODO: Implementar lógica de autorización
    public bool CanMove(Guid userId, Guid ticketId)
    {
        // Stub
        return true;
    }
}
