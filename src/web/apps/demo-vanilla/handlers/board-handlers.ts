/**
 * Tipo para el cache de tickets con capabilities
 */
export type TicketCache = {
  id: string;
  capabilities?: {
    move?: boolean;
    reorder?: boolean;
    allowedTransitions?: string[];
  };
};

/**
 * Tipos de los handlers del tablero
 */
export type BoardHandlers = {
  onMove: (cmd: { ticketId: string; from: string; to: string; newIndex: number }) => Promise<void>;
  onReorder: (cmd: { ticketId: string; columnId: string; newIndex: number }) => Promise<void>;
  onAssignTicket?: (cmd: { ticketId: string; assigneeId: string; note?: string }) => Promise<void>;
  onAddTagToTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void>;
  onRemoveTagFromTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void>;
};

/**
 * Crea los handlers del tablero con validaciones de capabilities
 * 
 * @param ticketsCache - Cache de tickets con capabilities para validación
 * @param onReload - Callback para recargar el tablero después de una operación
 */
import { updateTicketStatus } from '../api/tickets';
import { apiFetch } from '../api/apiClient';

export function createBoardHandlers(
  ticketsCacheGetter: () => TicketCache[],
  onReload: () => Promise<void>
): BoardHandlers {
  
  /**
   * Helper: Encuentra ticket y valida capabilities
   */
  function getTicketWithCapabilities(
    ticketId: string, 
    capability: 'move' | 'reorder'
  ): TicketCache | null {
    const cache = ticketsCacheGetter();
    const ticket = cache.find(t => t.id === ticketId);
    
    if (!ticket) {
      console.error(`[handlers] Ticket ${ticketId} no encontrado en cache`);
      return null;
    }
    
    if (ticket.capabilities?.[capability] === false) {
      const action = capability === 'move' ? 'mover' : 'reordenar';
      alert(`No tienes permisos para ${action} este ticket`);
      return null;
    }
    
    return ticket;
  }
  
  return {
    /**
     * Handler: Mover ticket entre columnas
     */
    async onMove(cmd) {
      console.log('[onMove]', cmd);
      
      const ticket = getTicketWithCapabilities(cmd.ticketId, 'move');
      if (!ticket) return;
      
      // Validar transiciones permitidas según FSM
      if (ticket.capabilities?.allowedTransitions 
          && !ticket.capabilities.allowedTransitions.includes(cmd.to)) {
        alert('Transición no permitida según las reglas de estado del ticket');
        return;
      }

      // REGLA: Verificar si el estado actual tiene comentarios
      try {
        // Obtener actividades del ticket
        const activities = await apiFetch<any[]>(`/api/tickets/${cmd.ticketId}/activity`, { method: 'GET' });
        
        // Encontrar la última actividad de cambio de estado (estado actual)
        const lastStatusChange = [...activities]
          .reverse()
          .find(a => a.action === 'TicketStatusChanged' || a.action === 'TicketCreated');
        
        // Si existe un estado actual y NO tiene comentarios, redirigir a detalle
        if (lastStatusChange && (!lastStatusChange.comment || lastStatusChange.comment.trim() === '')) {
          const shouldNavigate = confirm(
            `⚠️ El estado actual no tiene comentarios.\n\n` +
            `Para mantener la trazabilidad completa, debes agregar un comentario antes de cambiar de estado.\n\n` +
            `¿Deseas ir a la página de detalle del ticket para agregar comentarios?`
          );
          
          if (shouldNavigate) {
            window.location.href = `./ticket.html?id=${cmd.ticketId}`;
          }
          return;
        }

        // Si hay comentarios, permitir el cambio de estado
        await updateTicketStatus(cmd.ticketId, cmd.to);
        await onReload();
      } catch (error) {
        console.error('[onMove] Error al actualizar estado:', error);
        alert('No se pudo mover el ticket. Revisa la consola.');
      }
    },
    
    /**
     * Handler: Reordenar ticket dentro de la misma columna
     */
    async onReorder(cmd) {
      console.log('[onReorder]', cmd);
      
      const ticket = getTicketWithCapabilities(cmd.ticketId, 'reorder');
      if (!ticket) return;
      
      // Backend aún no soporta orden; solo recargamos para reflejar posición
      await onReload();
    },
    
    /**
     * Handler: Asignar ticket a un usuario
     */
    async onAssignTicket(cmd) {
      console.log('[onAssignTicket]', cmd);
      try {
        await apiFetch(`/api/tickets/${cmd.ticketId}/assign`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ assigneeId: cmd.assigneeId, reason: cmd.note })
        });
        await onReload();
      } catch (error) {
        console.error('[onAssignTicket] Error:', error);
        alert('No se pudo asignar el ticket. Revisa la consola.');
      }
    },
    
    /**
     * Handler: Agregar tag a un ticket
     */
    async onAddTagToTicket(cmd) {
      console.log('[onAddTagToTicket]', cmd);
      
      // TODO: Implementar cuando el backend esté disponible
      // await fetch(`/api/tickets/${cmd.ticketId}/tags`, {
      //   method: 'POST',
      //   body: JSON.stringify({ tagId: cmd.tagId })
      // });
      // await onReload();
      
      console.warn('[onAddTagToTicket] No implementado aún - requiere backend');
    },
    
    /**
     * Handler: Remover tag de un ticket
     */
    async onRemoveTagFromTicket(cmd) {
      console.log('[onRemoveTagFromTicket]', cmd);
      
      // TODO: Implementar cuando el backend esté disponible
      // await fetch(`/api/tickets/${cmd.ticketId}/tags/${cmd.tagId}`, {
      //   method: 'DELETE'
      // });
      // await onReload();
      
      console.warn('[onRemoveTagFromTicket] No implementado aún - requiere backend');
    }
  };
}
