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
  onAssignTicket?: (cmd: { ticketId: string; assigneeId: string }) => Promise<void>;
  onAddTagToTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void>;
  onRemoveTagFromTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void>;
};

/**
 * Crea los handlers del tablero con validaciones de capabilities
 * 
 * @param ticketsCache - Cache de tickets con capabilities para validación
 * @param onReload - Callback para recargar el tablero después de una operación
 */
export function createBoardHandlers(
  ticketsCache: TicketCache[],
  onReload: () => Promise<void>
): BoardHandlers {
  
  /**
   * Helper: Encuentra ticket y valida capabilities
   */
  function getTicketWithCapabilities(
    ticketId: string, 
    capability: 'move' | 'reorder'
  ): TicketCache | null {
    const ticket = ticketsCache.find(t => t.id === ticketId);
    
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
      
      // TODO: Implementar cuando el backend esté disponible
      // await fetch(`/api/tickets/${cmd.ticketId}/move`, {
      //   method: 'POST',
      //   body: JSON.stringify({ to: cmd.to, newIndex: cmd.newIndex })
      // });
      // await onReload();
      
      console.warn('[onMove] No implementado aún - requiere backend');
    },
    
    /**
     * Handler: Reordenar ticket dentro de la misma columna
     */
    async onReorder(cmd) {
      console.log('[onReorder]', cmd);
      
      const ticket = getTicketWithCapabilities(cmd.ticketId, 'reorder');
      if (!ticket) return;
      
      // TODO: Implementar cuando el backend esté disponible
      // await fetch(`/api/tickets/${cmd.ticketId}/reorder`, {
      //   method: 'POST',
      //   body: JSON.stringify({ newIndex: cmd.newIndex })
      // });
      // await onReload();
      
      console.warn('[onReorder] No implementado aún - requiere backend');
    },
    
    /**
     * Handler: Asignar ticket a un usuario
     */
    async onAssignTicket(cmd) {
      console.log('[onAssignTicket]', cmd);
      
      // TODO: Implementar cuando el backend esté disponible
      // await fetch(`/api/tickets/${cmd.ticketId}/assign`, {
      //   method: 'POST',
      //   body: JSON.stringify({ assigneeId: cmd.assigneeId })
      // });
      // await onReload();
      
      console.warn('[onAssignTicket] No implementado aún - requiere backend');
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
