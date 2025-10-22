// API - Tablero de tickets

import { currentUser } from '../state/session';
import { apiFetch } from './apiClient';
import { mapTicketResponseToDTO, getDefaultColumns, type TicketResponse } from './mappers';

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
 * Obtiene el tablero con columnas y tickets desde el backend
 * @param scope - 'assigned' (mis tickets), 'team' (del equipo), 'all' (todos)
 */
export async function getBoard(scope: BoardScope = 'assigned'): Promise<BoardData> {
  try {
    // Obtener tickets del backend
    const tickets = await apiFetch<TicketResponse[]>('/api/tickets');
    
    // Mapear tickets al formato del frontend
    const mappedTickets = tickets.map((ticket, index) => 
      mapTicketResponseToDTO(ticket, index)
    );
    
    // Filtrar por scope si es necesario
    const filteredTickets = filterTicketsByScope(mappedTickets, scope);
    
    return {
      columns: getDefaultColumns(),
      tickets: filteredTickets
    };
  } catch (error) {
    console.error('Error obteniendo tablero:', error);
    console.warn('[getBoard:mock] Usando datos de fallback');
    
    // Fallback: devolver mock data en caso de error
    return {
      columns: getDefaultColumns(),
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

/**
 * Filtra tickets según el scope y el usuario actual
 */
function filterTicketsByScope(tickets: TicketDTO[], scope: BoardScope): TicketDTO[] {
  if (!currentUser) return tickets;
  
  switch (scope) {
    case 'assigned':
      return tickets.filter(t => t.assignee?.id === currentUser!.userId);
    case 'team':
      return tickets.filter(t => 
        t.assignee && currentUser!.teamIds.includes(t.assignee.id)
      );
    case 'all':
    default:
      return tickets;
  }
}
