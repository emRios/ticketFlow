import { initBoard } from '@ticketflow/board-adapter-vanilla';
import { initSession } from '../state/session';
import { loadBoard, BoardData } from '../state/board-loader';
import { createBoardHandlers } from '../handlers/board-handlers';
import { setupScopeButtons } from '../views/scope-buttons';
import { BoardScope } from '../api/board';
import { setupCreateTicket } from '../views/create-ticket';
// Eliminado assign-dropdown basado en MutationObserver en favor de control inline del adapter
// import { setupAssignDropdown } from '../views/assign-dropdown';
import { listAgents } from '../api/users';
import { openAssignModal } from '../views/assign-modal';

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
  const handlers = createBoardHandlers(() => ticketsCache, async () => {
    const newData = await loadBoard(currentScope);
    ticketsCache = newData.rawTickets;
    board.rerender({ columns: newData.columns, tickets: newData.tickets });
  });
  
  // 4. Hidratar tablero
  const root = document.getElementById('app')!;
  board = initBoard(root, {
    columns: data.columns,
    tickets: data.tickets,
    ...handlers, // Spread de todos los handlers (onMove, onReorder, etc.)
    // Click en tarjeta -> modal de reasignación (solo si puede asignar)
    onCardClick: ({ ticketId }) => {
      // Navegar a la pantalla de detalle del ticket
      window.location.href = `ticket.html?id=${ticketId}`;
    }
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

  // 6. Botón: Nuevo ticket
  setupCreateTicket(
    () => currentScope,
    () => board,
    (raw) => { ticketsCache = raw; }
  );

  // 7. Setup usuario y logout
  setupUserInfo();

  // Nota: el dropdown de asignación ahora es inline en el adapter (sin observers)
}

// ===== Usuario y logout =====
function setupUserInfo() {
  const username = localStorage.getItem('ticketflow_username');
  const role = localStorage.getItem('ticketflow_role');
  const userInfoEl = document.getElementById('user-info');
  const logoutBtn = document.getElementById('btn-logout');
  const adminBtn = document.getElementById('btn-admin');

  if (userInfoEl && username) {
    const roleLabel = role === 'ADMIN' ? 'Administrador' : 'Agente';
    userInfoEl.textContent = `${username} (${roleLabel})`;
  }

  // Mostrar botón de administración solo para admins
  if (adminBtn && role === 'ADMIN') {
    adminBtn.style.display = 'inline-block';
    adminBtn.addEventListener('click', () => {
      window.location.href = 'admin.html';
    });
  }

  if (logoutBtn) {
    logoutBtn.addEventListener('click', () => {
      if (confirm('¿Seguro que deseas cerrar sesión?')) {
        localStorage.removeItem('ticketflow_userId');
        localStorage.removeItem('ticketflow_role');
        localStorage.removeItem('ticketflow_username');
        localStorage.removeItem('ticketflow_email');
        window.location.href = 'login.html';
      }
    });
  }
}

// Iniciar aplicación
init().then(() => {
  // Ocultar boot-status si existe
  const boot = document.getElementById('boot-status');
  if (boot) boot.classList.remove('show');
}).catch(error => {
  console.error('[main] Error al inicializar aplicación:', error);
  // Fallback: si no autenticado, redirigir a login
  const msg = (error && (error.message || error.toString())) || '';
  if (msg.toLowerCase().includes('no autenticado')) {
    sessionStorage.setItem('ticketflow_redirecting', '1');
    window.location.replace('login.html');
    return;
  }
  const boot = document.getElementById('boot-status');
  if (boot) {
    boot.classList.add('show');
    boot.textContent = 'Error al inicializar el tablero. Revisa la consola.';
  }
});
