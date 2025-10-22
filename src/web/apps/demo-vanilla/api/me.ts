// API - Usuario actual

import { apiFetch } from './apiClient';
import { mapUserResponseToCurrentUser, type UserResponse } from './mappers';

export type UserRole = 'agent' | 'admin' | 'client';

export type CurrentUser = {
  userId: string;
  role: UserRole;
  teamIds: string[];
  scopes: string[];
};

/**
 * Obtiene la información del usuario actual desde el backend
 */
export async function getMe(): Promise<CurrentUser> {
  try {
    const userResponse = await apiFetch<UserResponse>('/api/me');
    return mapUserResponseToCurrentUser(userResponse);
  } catch (error) {
    console.error('Error obteniendo usuario actual:', error);
    
    // Fallback a usuario mock en caso de error
    console.warn('Usando datos mock del usuario');
    return {
      userId: 'u123',
      role: 'agent',
      teamIds: ['t-ops'],
      scopes: ['tickets:move', 'tickets:reorder', 'tickets:assign', 'tickets:tag']
    };
  }
}

