import { initBoard } from '@ticketflow/board-adapter-vanilla';
import { initSession } from './state/session';
import { loadBoard, BoardData } from './state/board-loader';
import { createBoardHandlers } from './handlers/board-handlers';
import { setupScopeButtons } from './views/scope-buttons';
import { BoardScope } from './api/board';

// ===== Variables globales =====
let board: ReturnType<typeof initBoard>;
let currentScope: BoardScope = 'assigned';
let ticketsCache: any[] = []; // Cache para validaciones en handlers

// ===== Inicialización =====
async function init() {
  // 1. Inicializar sesión antes de hidratar el tablero
  await initSession();
  
  // 2. Cargar datos iniciales
  const data = await loadBoard(currentScope);
  ticketsCache = data.rawTickets;
  
  // 3. Crear handlers con lógica de recarga
  const handlers = createBoardHandlers(ticketsCache, async () => {
    const newData = await loadBoard(currentScope);
    ticketsCache = newData.rawTickets;
    board.rerender({ columns: newData.columns, tickets: newData.tickets });
  });
  
  // 4. Hidratar tablero
  const root = document.getElementById('app')!;
  board = initBoard(root, {
    columns: data.columns,
    tickets: data.tickets,
    ...handlers // Spread de todos los handlers (onMove, onReorder, etc.)
  });
  
  // 5. Setup de botones de filtro por scope
  setupScopeButtons(
    currentScope,
    (scope, newData) => {
      currentScope = scope;
      ticketsCache = newData.rawTickets;
      board.rerender({ columns: newData.columns, tickets: newData.tickets });
    },
    loadBoard
  );
}

// Iniciar aplicación
init().catch(error => {
  console.error('[main] Error al inicializar aplicación:', error);
});