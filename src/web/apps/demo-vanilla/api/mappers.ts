// Mappers para convertir DTOs del backend al formato del frontend

import type { TicketDTO, ColumnDTO } from './board';
import { appConfig } from '../config/app-config';
import type { CurrentUser } from './me';

// ========== Backend DTOs ==========

export interface TicketResponse {
  id: string; // UUID from backend
  title: string;
  description: string;
  status: string;
  priority: string;
  assignedTo: string | null;
  assignedToName: string | null;
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
  // Normalizamos: admitimos español (canon), inglés legacy y variantes
  const s = (status || '').toLowerCase();
  const mapping: Record<string, string> = {
    // Español canónico
    'nuevo': 'nuevo',
    'en-proceso': 'en-proceso',
    'en_proceso': 'en-proceso',
    'en espera': 'en-espera',
    'en-espera': 'en-espera',
    'resuelto': 'resuelto',
    'cerrado': 'cerrado',
    // Inglés legacy
    'open': 'nuevo',
    'in_progress': 'en-proceso',
    'in-progress': 'en-proceso',
    'waiting': 'en-espera',
    'resolved': 'resuelto',
    'closed': 'cerrado'
  };
  return mapping[s] || 'nuevo';
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
    id: ticket.id, // UUID directo desde el backend
    title: ticket.title,
    columnId: mapStatusToColumnId(ticket.status),
    order: index,
    assignee: ticket.assignedTo ? {
      id: ticket.assignedTo,
      name: ticket.assignedToName || ticket.assignedTo.replace(/\./g, ' ').replace(/\b\w/g, l => l.toUpperCase())
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
      // Valores por defecto; se ajustan en api/board según permisos y scope
      move: true,
      reorder: true,
      assign: true,
      addTag: true,
      removeTag: true,
      allowedTransitions: appConfig.fsm[mapStatusToColumnId(ticket.status)] ?? []
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
