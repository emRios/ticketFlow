// API Mock - Tablero de tickets
// TODO: Reemplazar con llamada real al backend cuando esté disponible

import { currentUser } from '../state/session';

export type BoardScope = 'assigned' | 'team' | 'all';

export type TicketCapabilities = {
  move: boolean;
  reorder: boolean;
  assign: boolean;
  addTag: boolean;
  removeTag: boolean;
  allowedTransitions: string[];
};

export type TicketDTO = {
  id: string;
  title: string;
  columnId: string;
  order: number;
  assignee?: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  requester?: {
    id: string;
    name?: string;
  };
  tags: Array<{
    id: string;
    label: string;
    color: string;
  }>;
  updatedAt: string;
  capabilities: TicketCapabilities;
};

export type ColumnDTO = {
  id: string;
  name: string;
};

export type BoardData = {
  columns: ColumnDTO[];
  tickets: TicketDTO[];
};

/**
 * Obtiene el tablero con columnas y tickets según el scope
 * @param scope - 'assigned' (mis tickets), 'team' (del equipo), 'all' (todos)
 */
export async function getBoard(scope: BoardScope = 'assigned'): Promise<BoardData> {
  try {
    // TODO: Implementar fetch real cuando backend esté listo
    // const response = await fetch(`/api/board?scope=${scope}`);
    // if (!response.ok) throw new Error('Failed to fetch board');
    // return await response.json();
    
    throw new Error('NO_BACKEND'); // temporal - forzar fallback a mock
  } catch (e) {
    console.warn('[getBoard:mock]', (e as Error)?.message ?? e);
    
    // Fallback: siempre devolver 4 columnas y al menos 1 ticket
    return {
      columns: [
        { id: 'nuevo', name: 'Nuevo' },
        { id: 'en-proceso', name: 'En Proceso' },
        { id: 'en-espera', name: 'En Espera' },
        { id: 'resuelto', name: 'Resuelto' }
      ],
      tickets: [
        {
          id: 'TF-1024',
          title: 'Fallo en pagos',
          columnId: 'en-proceso',
          order: 0,
          assignee: { id: 'u123', name: 'Ana' },
          requester: { id: 'c88', name: 'Cliente SA' },
          tags: [{ id: 't1', label: 'Urgente', color: '#ef4444' }],
          updatedAt: new Date().toISOString(),
          capabilities: {
            move: true,
            reorder: true,
            assign: false,
            addTag: true,
            removeTag: true,
            allowedTransitions: ['en-espera', 'verificacion', 'resuelto']
          }
        }
      ]
    };
  }
}
