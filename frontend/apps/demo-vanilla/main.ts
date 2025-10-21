import { initBoard } from '../../packages/board-adapter-vanilla/index';
import { initSession, currentUser } from './session';
import { getBoard } from './api/board';

// ===== Inicialización =====
async function init() {
  // 1. Inicializar sesión antes de hidratar el tablero
  await initSession();
  console.log('[session]', currentUser);
  
  // 2. Obtener datos del tablero desde API
  const { columns, tickets } = await getBoard('assigned');
  console.log('[board]', { columns, tickets });
  
  // 3. Mapear tickets a DTOs del adapter (sin capabilities, solo UI)
  const ticketsForAdapter = tickets.map(t => ({
    ticketId: t.id,
    title: t.title,
    columnId: t.columnId,
    order: t.order,
    tags: t.tags,
    assignee: t.assignee
  }));
  
  // 4. Hidratar tablero
  const root = document.getElementById('app')!;
  const board = initBoard(root, {
    columns, 
    tickets: ticketsForAdapter,
  
  // ===== Drag & Drop Handlers =====
  async onMove(cmd) {
    console.log('[onMove]', cmd);
    
    // TODO: Validar capabilities antes de ejecutar
    // TODO: POST /api/tickets/${cmd.ticketId}/move con { to, newIndex }
    // TODO: Recargar board: const newData = await getBoard('assigned'); board.rerender(newData);
    
    console.warn('[onMove] No implementado aún - requiere backend');
  },
  
  async onReorder(cmd) {
    console.log('[onReorder]', cmd);
    
    // TODO: Validar capabilities antes de ejecutar
    // TODO: POST /api/tickets/${cmd.ticketId}/reorder con { newIndex }
    // TODO: Recargar board
    
    console.warn('[onReorder] No implementado aún - requiere backend');
  },
  
  // ===== Assignee Handler =====
  async onAssignTicket(cmd) {
    console.log('[onAssignTicket]', cmd);
    
    // TODO: POST /api/tickets/${cmd.ticketId}/assign con { assigneeId }
    // TODO: Recargar board
    
    console.warn('[onAssignTicket] No implementado aún - requiere backend');
  },
  
  // ===== Tags Handlers =====
  async onAddTagToTicket(cmd) {
    console.log('[onAddTagToTicket]', cmd);
    
    // TODO: POST /api/tickets/${cmd.ticketId}/tags con { tagId }
    // TODO: Recargar board
    
    console.warn('[onAddTagToTicket] No implementado aún - requiere backend');
  },
  
  async onRemoveTagFromTicket(cmd) {
    console.log('[onRemoveTagFromTicket]', cmd);
    
    // TODO: DELETE /api/tickets/${cmd.ticketId}/tags/${cmd.tagId}
    // TODO: Recargar board
    
    console.warn('[onRemoveTagFromTicket] No implementado aún - requiere backend');
  }
  });
  
  return board;
}

// Iniciar aplicación
init().catch(error => {
  console.error('[main] Error al inicializar aplicación:', error);
});