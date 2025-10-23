// Vista: Dropdown de asignación manual para administradores

import { apiFetch } from '../api/apiClient';

type Agent = {
  userId: string;
  name: string;
  email: string;
};

let agentsCache: Agent[] | null = null;

// ===== Control de observador/inyección (para evitar loops) =====
let assignObserver: MutationObserver | null = null;
let injectScheduled = false; // Debounce de inyección
let isInjecting = false;     // Evitar reentrancia mientras modificamos el DOM

/**
 * Obtiene la lista de agentes disponibles
 */
async function fetchAgents(): Promise<Agent[]> {
  if (agentsCache) return agentsCache;
  
  try {
    const response = await apiFetch('/users?role=AGENT', { method: 'GET' });
    agentsCache = response as Agent[];
    return agentsCache;
  } catch (error) {
    console.error('[assign-dropdown] Error al obtener agentes:', error);
    return [];
  }
}

/**
 * Asigna un ticket a un agente
 */
async function assignTicket(ticketId: string, agentId: string): Promise<boolean> {
  try {
    await apiFetch(`/tickets/${ticketId}/assign`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ agentId })
    });
    return true;
  } catch (error) {
    console.error('[assign-dropdown] Error al asignar ticket:', error);
    throw error;
  }
}

/**
 * Crea el HTML del dropdown de asignación
 */
function createDropdownHTML(agents: Agent[], currentAgentId?: string): string {
  return `
    <div class="assign-dropdown">
      <select class="assign-select">
        <option value="">-- Asignar a --</option>
        ${agents.map(agent => `
          <option value="${agent.userId}" ${agent.userId === currentAgentId ? 'selected' : ''}>
            ${agent.name}
          </option>
        `).join('')}
      </select>
    </div>
  `;
}

/**
 * Inyecta el dropdown en las tarjetas del tablero
 * Solo para administradores y tickets en estados 'nuevo' o 'en-proceso'
 */
export async function setupAssignDropdown(
  onAssignSuccess: () => Promise<void>
): Promise<void> {
  const role = localStorage.getItem('ticketflow_role');
  
  // Solo para administradores
  if (role !== 'ADMIN') {
    return;
  }

  // Obtener lista de agentes
  const agents = await fetchAgents();
  if (agents.length === 0) {
    console.warn('[assign-dropdown] No hay agentes disponibles');
    return;
  }

  // Agregar estilos si no existen
  injectStyles();

  // Observar cambios en el DOM para inyectar dropdowns en nuevas tarjetas
  observeCards(agents, onAssignSuccess);
}

/**
 * Observa cambios en el DOM para agregar dropdowns a tarjetas nuevas
 */
function observeCards(agents: Agent[], onAssignSuccess: () => Promise<void>): void {
  // Desconectar observador previo si existe
  if (assignObserver) {
    try { assignObserver.disconnect(); } catch {}
  }

  assignObserver = new MutationObserver((mutations) => {
    if (isInjecting) return; // Si estamos inyectando, ignorar mutaciones propias

    // Procesar solo si se agregan tarjetas
    const hasRelevantChanges = mutations.some(m =>
      Array.from(m.addedNodes).some(node =>
        node instanceof HTMLElement && (
          node.classList?.contains('card') ||
          node.querySelector?.('.card') ||
          node.classList?.contains('col-body')
        )
      )
    );

    if (!hasRelevantChanges) return;

    // Debounce: agrupar múltiples mutaciones en un solo ciclo
    if (injectScheduled) return;
    injectScheduled = true;

    // Usar requestAnimationFrame para esperar a que termine el render
    requestAnimationFrame(() => {
      // Desconectar para que nuestras modificaciones no disparen el observer
      try { assignObserver?.disconnect(); } catch {}
      isInjecting = true;

      try {
        injectDropdownsToCards(agents, onAssignSuccess);
      } finally {
        isInjecting = false;
        injectScheduled = false;
        // Re-conectar el observador
        const boardElement = document.getElementById('app');
        if (boardElement) {
          assignObserver?.observe(boardElement, { childList: true, subtree: true });
        }
      }
    });
  });

  const boardElement = document.getElementById('app');
  if (boardElement) {
    assignObserver.observe(boardElement, { childList: true, subtree: true });
  }

  // Inyección inicial
  isInjecting = true;
  try { injectDropdownsToCards(agents, onAssignSuccess); } finally { isInjecting = false; }
}

/**
 * Calcula el tiempo transcurrido desde una fecha
 */
function getElapsedTime(updatedAt?: string): string {
  if (!updatedAt) return '';
  
  const now = new Date();
  const updated = new Date(updatedAt);
  const diffMs = now.getTime() - updated.getTime();
  const diffMins = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMins < 1) return 'Ahora mismo';
  if (diffMins < 60) return `${diffMins}m`;
  if (diffHours < 24) return `${diffHours}h`;
  return `${diffDays}d`;
}

/**
 * Inyecta dropdowns en todas las tarjetas que no los tienen
 */
function injectDropdownsToCards(agents: Agent[], onAssignSuccess: () => Promise<void>): void {
  const cards = document.querySelectorAll('.card');
  
  cards.forEach(card => {
    const ticketId = card.getAttribute('data-ticket-id');
    if (!ticketId) return;

    // Verificar estado del ticket
    const columnId = card.closest('.col')?.getAttribute('data-col-id');
    
    // Evitar reprocesar la misma tarjeta varias veces en el mismo DOM
    const alreadyProcessed = card.getAttribute('data-assign-ready') === '1';

    // Solo agregar dropdown si no existe y está en nuevo/en-proceso
    if (!alreadyProcessed && !card.querySelector('.assign-dropdown')) {
      if (columnId && ['nuevo', 'en-proceso'].includes(columnId)) {
        // Obtener agente actual si existe
        const currentAgentId = card.querySelector('.card-assignee')?.getAttribute('data-assignee-id');

        // Crear e inyectar dropdown
        const dropdownContainer = document.createElement('div');
        dropdownContainer.className = 'assign-dropdown-container';
        dropdownContainer.innerHTML = createDropdownHTML(agents, currentAgentId || undefined);

        // Insertar después del header
        const cardHeader = card.querySelector('.card-header');
        if (cardHeader) {
          cardHeader.insertAdjacentElement('afterend', dropdownContainer);
        }

        // Event listener para el select
        const selectElement = dropdownContainer.querySelector('.assign-select') as HTMLSelectElement;
        if (selectElement) {
          selectElement.addEventListener('change', async (e) => {
            const select = e.target as HTMLSelectElement;
            const agentId = select.value;

            if (!agentId) return;

            // Deshabilitar select durante la asignación
            select.disabled = true;

            try {
              await assignTicket(ticketId, agentId);
              
              // Mostrar feedback visual
              card.classList.add('assign-success');
              setTimeout(() => card.classList.remove('assign-success'), 1000);

              // Recargar el tablero
              await onAssignSuccess();
            } catch (error: any) {
              alert(`Error al asignar ticket: ${error.message || 'Error desconocido'}`);
              select.value = currentAgentId || '';
            } finally {
              select.disabled = false;
            }
          });
        }

        // Marcar tarjeta como inicializada para no reinyectar en este DOM
        card.setAttribute('data-assign-ready', '1');
      }
    }

    // Mejorar visualización del nombre del agente y tiempo transcurrido
    enhanceCardVisuals(card);
  });
}

/**
 * Mejora las visuales de la tarjeta: avatar mejorado y tiempo transcurrido
 */
function enhanceCardVisuals(card: Element): void {
  // Agregar tiempo transcurrido si no existe
  if (!card.querySelector('.card-elapsed-time') && (card as HTMLElement).getAttribute('data-elapsed-ready') !== '1') {
    const assigneeNameEl = card.querySelector('.card-assignee-name');
    if (assigneeNameEl) {
      const cardIdEl = card.querySelector('.card-id');
      if (cardIdEl) {
        const ticketId = card.getAttribute('data-ticket-id');
        // Aquí podríamos obtener el updatedAt desde un atributo data-*
        // Por ahora usamos un placeholder
        const elapsedTimeEl = document.createElement('span');
        elapsedTimeEl.className = 'card-elapsed-time';
        elapsedTimeEl.textContent = '⏱ Reciente';
        elapsedTimeEl.style.cssText = 'font-size: 11px; color: #888; margin-left: 8px;';
        cardIdEl.appendChild(elapsedTimeEl);
        (card as HTMLElement).setAttribute('data-elapsed-ready', '1');
      }
    }
  }

  // Mejorar el avatar (ya existe en board-adapter-vanilla)
  const avatarEl = card.querySelector('.avatar');
  if (avatarEl && !avatarEl.classList.contains('enhanced')) {
    avatarEl.classList.add('enhanced');
    // El adapter ya maneja las iniciales, solo agregamos clase
  }
}

/**
 * Inyecta estilos CSS para el dropdown
 */
function injectStyles(): void {
  const styleId = 'assign-dropdown-styles';
  if (document.getElementById(styleId)) return;

  const style = document.createElement('style');
  style.id = styleId;
  style.textContent = `
    .assign-dropdown-container {
      margin: 8px 0;
      padding: 0 8px;
    }

    .assign-select {
      width: 100%;
      padding: 6px 8px;
      border: 1px solid #e3e3e8;
      border-radius: 4px;
      font-size: 13px;
      font-family: inherit;
      background: white;
      cursor: pointer;
      transition: all 0.2s;
    }

    .assign-select:hover {
      border-color: #3b82f6;
    }

    .assign-select:focus {
      outline: none;
      border-color: #3b82f6;
      box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
    }

    .assign-select:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .card.assign-success {
      animation: assignSuccess 0.5s ease-out;
    }

    @keyframes assignSuccess {
      0%, 100% {
        transform: scale(1);
      }
      50% {
        transform: scale(1.02);
        box-shadow: 0 4px 12px rgba(34, 197, 94, 0.3);
      }
    }

    /* Ajustar espaciado en las tarjetas */
    .card-assignee-name {
      font-size: 12px;
      color: #666;
      margin: 4px 8px;
    }
  `;
  document.head.appendChild(style);
}
