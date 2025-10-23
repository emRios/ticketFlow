// Gestión de sesión del usuario actual
import { getMe, type CurrentUser } from '../api/me';

/**
 * Usuario actual autenticado
 * null = no inicializado o no autenticado
 */
export let currentUser: CurrentUser | null = null;

/**
 * Obtiene el token JWT del localStorage
 */
export function getToken(): string | null {
  return localStorage.getItem('jwt_token');
}

/**
 * Guarda el token JWT en localStorage
 */
export function setToken(token: string): void {
  localStorage.setItem('jwt_token', token);
}

/**
 * Elimina el token JWT del localStorage
 */
export function clearToken(): void {
  localStorage.removeItem('jwt_token');
}

/**
 * Dev helpers: persistencia de userId para X-UserId
 * Sincronizado con ticketflow_userId del login
 */
export function getUserId(): string | null {
  return localStorage.getItem('ticketflow_userId') || localStorage.getItem('user_id');
}

export function setUserId(userId: string): void {
  localStorage.setItem('user_id', userId);
  localStorage.setItem('ticketflow_userId', userId);
}

export function clearUserId(): void {
  localStorage.removeItem('user_id');
  localStorage.removeItem('ticketflow_userId');
  localStorage.removeItem('ticketflow_role');
  localStorage.removeItem('ticketflow_username');
  localStorage.removeItem('ticketflow_email');
}

/**
 * Inicializa la sesión obteniendo datos del localStorage
 * Debe llamarse antes de hidratar el tablero
 */
export async function initSession(): Promise<void> {
  try {
    // Obtener datos del localStorage (guardados en login)
    const userId = localStorage.getItem('ticketflow_userId');
    const role = localStorage.getItem('ticketflow_role');
    
    if (!userId || !role) {
      throw new Error('Usuario no autenticado');
    }

    // Mapear rol a formato esperado
    const roleNormalized = role.toLowerCase() as CurrentUser['role'];
    
    // Derivar teamIds según el rol
    const teamIds = roleNormalized === 'agent' ? ['t-ops'] : 
                    roleNormalized === 'admin' ? ['t-admin'] : 
                    [];
    
    // Derivar scopes según el rol
    const scopes = roleNormalized === 'agent' || roleNormalized === 'admin' ? [
      'tickets:read',
      'tickets:create',
      'tickets:update',
      'tickets:assign',
      'tickets:comment'
    ] : [
      'tickets:read',
      'tickets:create'
    ];
    
    currentUser = {
      userId,
      role: roleNormalized,
      teamIds,
      scopes
    };

    // Guardar userId en formato legacy para compatibilidad
    setUserId(userId);
    
    console.log('[session] Usuario autenticado:', currentUser);
  } catch (error) {
    console.error('[session] Error al inicializar sesión:', error);
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
