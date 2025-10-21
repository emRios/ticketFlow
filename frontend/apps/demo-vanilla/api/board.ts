// API Mock - Tablero de tickets
// TODO: Reemplazar con llamada real al backend cuando esté disponible

import { currentUser } from '../session';

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
  // Simular latencia de red
  await new Promise(resolve => setTimeout(resolve, 150));
  
  // TODO: Reemplazar con fetch(`/api/board?scope=${scope}`) cuando backend esté listo
  
  // Columnas fijas
  const columns: ColumnDTO[] = [
    { id: 'nuevo', name: 'Nuevo' },
    { id: 'en-proceso', name: 'En Proceso' },
    { id: 'en-espera', name: 'En Espera' },
    { id: 'resuelto', name: 'Resuelto' }
  ];
  
  // Tickets mock - asignados al usuario actual
  const userId = currentUser?.userId || 'u123';
  
  const mockTickets: TicketDTO[] = [
    {
      id: 't1',
      title: 'Llamar a cliente para seguimiento',
      columnId: 'nuevo',
      order: 0,
      assignee: {
        id: userId,
        name: 'Juan Pérez',
        avatarUrl: 'https://i.pravatar.cc/150?img=12'
      },
      requester: {
        id: 'c1',
        name: 'Carlos Gómez'
      },
      tags: [
        { id: 't1', label: 'Urgente', color: '#ef4444' }
      ],
      updatedAt: new Date().toISOString(),
      capabilities: {
        move: true,
        reorder: true,
        assign: true,
        addTag: true,
        removeTag: true,
        allowedTransitions: ['en-proceso', 'en-espera']
      }
    },
    {
      id: 't2',
      title: 'Enviar propuesta comercial',
      columnId: 'en-proceso',
      order: 0,
      assignee: {
        id: userId,
        name: 'Juan Pérez',
        avatarUrl: 'https://i.pravatar.cc/150?img=12'
      },
      requester: {
        id: 'c2',
        name: 'Ana Martínez'
      },
      tags: [
        { id: 't3', label: 'Feature', color: '#3b82f6' }
      ],
      updatedAt: new Date(Date.now() - 3600000).toISOString(), // 1 hora atrás
      capabilities: {
        move: true,
        reorder: true,
        assign: true,
        addTag: true,
        removeTag: true,
        allowedTransitions: ['nuevo', 'en-espera', 'resuelto']
      }
    },
    {
      id: 't3',
      title: 'Revisión legal del contrato',
      columnId: 'en-espera',
      order: 0,
      assignee: {
        id: 'u3',
        name: 'Carlos López',
        avatarUrl: 'https://i.pravatar.cc/150?img=33'
      },
      requester: {
        id: 'c1',
        name: 'Carlos Gómez'
      },
      tags: [
        { id: 't2', label: 'Bug', color: '#f97316' },
        { id: 't4', label: 'Documentación', color: '#8b5cf6' }
      ],
      updatedAt: new Date(Date.now() - 7200000).toISOString(), // 2 horas atrás
      capabilities: {
        move: false, // No puede mover (asignado a otro)
        reorder: false,
        assign: true,
        addTag: false,
        removeTag: false,
        allowedTransitions: []
      }
    }
  ];
  
  // Filtrar según scope
  let filteredTickets = mockTickets;
  if (scope === 'assigned') {
    filteredTickets = mockTickets.filter(t => t.assignee?.id === userId);
  } else if (scope === 'team') {
    // TODO: Filtrar por teamIds del usuario
    filteredTickets = mockTickets;
  }
  
  return {
    columns,
    tickets: filteredTickets
  };
}
