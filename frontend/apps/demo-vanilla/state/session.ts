// Gestión de sesión del usuario actual
import { getMe, type CurrentUser } from '../api/me';

/**
 * Usuario actual autenticado
 * null = no inicializado o no autenticado
 */
export let currentUser: CurrentUser | null = null;

/**
 * Inicializa la sesión obteniendo datos del usuario actual
 * Debe llamarse antes de hidratar el tablero
 */
export async function initSession(): Promise<void> {
  try {
    currentUser = await getMe();
    console.log('[session] Usuario autenticado:', currentUser);
  } catch (error) {
    console.error('[session] Error al obtener usuario:', error);
    throw error;
  }
}

/**
 * Verifica si el usuario tiene un permiso específico
 */
export function hasScope(scope: string): boolean {
  return currentUser?.scopes.includes(scope) ?? false;
}

/**
 * Verifica si el usuario tiene un rol específico
 */
export function hasRole(role: CurrentUser['role']): boolean {
  return currentUser?.role === role;
}
