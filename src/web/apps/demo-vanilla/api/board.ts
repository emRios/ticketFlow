// API - Tablero de tickets

import { currentUser } from '../state/session';
import { apiFetch } from './apiClient';
import { mapTicketResponseToDTO, getDefaultColumns, type TicketResponse } from './mappers';
import { appConfig, type BoardScope as CfgScope } from '../config/app-config';

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
    const mappedTickets = tickets.map((ticket, index) => mapTicketResponseToDTO(ticket, index));
    
    // Filtrar por scope si es necesario
    const filteredTickets = filterTicketsByScope(mappedTickets, scope);
    
    // Aplicar permisos por scope y usuario + FSM desde config
    const withCaps = filteredTickets.map(t => ({
      ...t,
      capabilities: {
        ...t.capabilities,
        allowedTransitions: appConfig.fsm[t.columnId] ?? [],
          move: computeMoveAllowed(t, scope),
          assign: computeAssignAllowed(t, scope)
      }
    }));
    
    return {
      columns: getDefaultColumns(),
      tickets: withCaps
    };
  } catch (error) {
    console.error('Error obteniendo tablero:', error);
    throw error; // Propagar el error sin fallback
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
      // Sin información explícita de equipos en el ticket, aproximamos "mi equipo"
      // como "tickets asignados a otros agentes (no yo)" para visibilidad colaborativa.
      return tickets.filter(t => t.assignee && t.assignee.id !== currentUser!.userId);
    case 'all':
    default:
      return tickets;
  }
}

/** Decide si el usuario actual puede mover la tarjeta en el scope actual */
function computeMoveAllowed(ticket: TicketDTO, scope: BoardScope): boolean {
  // Si no hay usuario cargado aún, no bloqueamos (modo dev)
  if (!currentUser) return true;

  const role = currentUser.role;
  // ADMIN bypass: puede mover en cualquier scope si está activo en config
  if (role === 'admin' && appConfig.permissions.adminBypass) return true;

  const isMovableScope = appConfig.permissions.movableScopes.includes(scope as CfgScope);
  if (!isMovableScope) return false;
  if (role === 'agent') {
    if (appConfig.permissions.agentCanMoveUnassigned) return true;
    return ticket.assignee?.id === currentUser.userId; // solo propios
  }
  // clientes/no rol: no mover
  return false;
}

/** Decide si el usuario actual puede asignar/reasignar */
function computeAssignAllowed(ticket: TicketDTO, scope: BoardScope): boolean {
  if (!currentUser) return false;
  const isAdmin = currentUser.role === 'admin';
  const inAssignableStatus = ticket.columnId === 'nuevo' || ticket.columnId === 'en-proceso';
  if (!inAssignableStatus) return false;

  // Admin puede reasignar todos
  if (isAdmin) return true;

  // Agente: puede reasignar sus propios tickets y, en vista de equipo, permitir edición colaborativa
  if (currentUser.role === 'agent') {
    const isMine = ticket.assignee?.id === currentUser.userId;
    const inTeamView = scope === 'team';
    return isMine || inTeamView;
  }

  return false;
}
