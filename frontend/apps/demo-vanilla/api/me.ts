// API Mock - Usuario actual
// TODO: Reemplazar con llamada real al backend cuando esté disponible

export type UserRole = 'agent' | 'admin' | 'client';

export type CurrentUser = {
  userId: string;
  role: UserRole;
  teamIds: string[];
  scopes: string[];
};

/**
 * Obtiene la información del usuario actual
 * MOCK: Retorna usuario hardcodeado mientras no hay backend
 */
export async function getMe(): Promise<CurrentUser> {
  // Simular latencia de red
  await new Promise(resolve => setTimeout(resolve, 100));
  
  // TODO: Reemplazar con fetch('/api/auth/me') cuando backend esté listo
  const mockUser: CurrentUser = {
    userId: 'u123',
    role: 'agent',
    teamIds: ['t-ops'],
    scopes: ['tickets:move', 'tickets:reorder', 'tickets:assign', 'tickets:tag']
  };
  
  return mockUser;
}
