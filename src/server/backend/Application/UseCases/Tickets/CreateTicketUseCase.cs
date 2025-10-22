using TicketFlow.Domain.Entities;

namespace TicketFlow.Application.UseCases.Tickets;

/// <summary>
/// Caso de uso - Crear ticket
/// </summary>
public class CreateTicketUseCase
{
    // TODO: Inyectar ITicketRepository, IUnitOfWork, etc.
    
    public async Task<Guid> ExecuteAsync(string title, string description)
    {
        // TODO: Implementar lógica
        // 1. Validar input
        // 2. Crear entidad
        // 3. Guardar en repositorio
        // 4. Publicar evento de dominio
        
        await Task.CompletedTask;
        return Guid.NewGuid();
    }
}
