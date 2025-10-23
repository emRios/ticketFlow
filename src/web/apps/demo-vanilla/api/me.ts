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
  const userResponse = await apiFetch<UserResponse>('/api/me');
  return mapUserResponseToCurrentUser(userResponse);
}

