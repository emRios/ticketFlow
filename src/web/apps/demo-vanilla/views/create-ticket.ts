import { createTicket } from '../api/tickets';
import { BoardScope } from '../api/board';
import { loadBoard } from '../state/board-loader';

/**
 * Inicia el botón "Nuevo ticket" y maneja el flujo de creación
 */
export function setupCreateTicket(
  getScope: () => BoardScope,
  getBoard: () => { rerender: (data: { columns: any[]; tickets: any[] }) => void },
  setCache: (rawTickets: any[]) => void
) {
  const btn = document.getElementById('create-ticket');
  if (!btn) return;

  btn.addEventListener('click', async () => {
    const title = prompt('Título del ticket:', 'Card esp');
    if (!title || !title.trim()) return;
    const description = prompt('Descripción (opcional):', '');
    const priorityInput = prompt("Prioridad (HIGH, MEDIUM, LOW):", 'LOW') || 'LOW';
    const priority = ['HIGH', 'MEDIUM', 'LOW'].includes(priorityInput.toUpperCase())
      ? priorityInput.toUpperCase()
      : 'LOW';

    try {
      await createTicket({ title: title.trim(), description: description || '', priority });
      // Recargar tablero con el scope actual
      const data = await loadBoard(getScope());
      setCache(data.rawTickets);
      getBoard().rerender({ columns: data.columns, tickets: data.tickets });
    } catch (err) {
      console.error('[create-ticket] Error creando ticket', err);
      alert('No se pudo crear el ticket. Revisa la consola.');
    }
  });
}
