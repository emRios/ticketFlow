// Mappers para convertir DTOs del backend al formato del frontend

import type { TicketDTO, ColumnDTO } from './board';
import type { CurrentUser } from './me';

// ========== Backend DTOs ==========

export interface TicketResponse {
  id: number;
  title: string;
  description: string;
  status: string;
  priority: string;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UserResponse {
  userId: string;
  username: string;
  role: string;
  email: string;
}

// ========== Mappers ==========

/**
 * Mapea el status del backend al columnId del frontend
 */
function mapStatusToColumnId(status: string): string {
  const mapping: Record<string, string> = {
    'OPEN': 'nuevo',
    'IN_PROGRESS': 'en-proceso',
    'WAITING': 'en-espera',
    'RESOLVED': 'resuelto',
    'CLOSED': 'cerrado'
  };
  return mapping[status] || 'nuevo';
}

/**
 * Mapea un ticket del backend al formato del frontend
 */
export function mapTicketResponseToDTO(ticket: TicketResponse, index: number): TicketDTO {
  // Mapear priority a color de tag
  const priorityColors: Record<string, string> = {
    'HIGH': '#ef4444',
    'MEDIUM': '#f59e0b',
    'LOW': '#10b981'
  };
  
  return {
    id: `TF-${ticket.id}`,
    title: ticket.title,
    columnId: mapStatusToColumnId(ticket.status),
    order: index,
    assignee: ticket.assignedTo ? {
      id: ticket.assignedTo,
      name: ticket.assignedTo.replace(/\./g, ' ').replace(/\b\w/g, l => l.toUpperCase())
    } : undefined,
    requester: {
      id: 'unknown',
      name: 'Cliente'
    },
    tags: ticket.priority ? [{
      id: ticket.priority.toLowerCase(),
      label: ticket.priority,
      color: priorityColors[ticket.priority] || '#6b7280'
    }] : [],
    updatedAt: ticket.updatedAt || ticket.createdAt,
    capabilities: {
      move: true,
      reorder: true,
      assign: true,
      addTag: true,
      removeTag: true,
      allowedTransitions: ['nuevo', 'en-proceso', 'en-espera', 'resuelto']
    }
  };
}

/**
 * Mapea el usuario del backend al formato del frontend
 */
export function mapUserResponseToCurrentUser(user: UserResponse): CurrentUser {
  const role = user.role.toLowerCase() as 'agent' | 'admin' | 'client';
  
  // Derivar teamIds según el rol
  const teamIds = role === 'agent' ? ['t-ops'] : 
                  role === 'admin' ? ['t-admin'] : 
                  [];
  
  // Derivar scopes según el rol
  const scopes = role === 'agent' || role === 'admin' ? [
    'tickets:read',
    'tickets:create',
    'tickets:update',
    'tickets:assign',
    'tickets:comment'
  ] : [
    'tickets:read',
    'tickets:create'
  ];
  
  return {
    userId: user.userId,
    role,
    teamIds,
    scopes
  };
}

/**
 * Genera las columnas estándar del tablero
 */
export function getDefaultColumns(): ColumnDTO[] {
  return [
    { id: 'nuevo', name: 'Nuevo' },
    { id: 'en-proceso', name: 'En Proceso' },
    { id: 'en-espera', name: 'En Espera' },
    { id: 'resuelto', name: 'Resuelto' }
  ];
}
