/**
 * apiClient.ts
 * 
 * Cliente HTTP centralizado para consumir la API backend.
 * Maneja automáticamente:
 * - Autenticación JWT desde localStorage
 * - Headers (Authorization, Content-Type)
 * - Manejo de errores HTTP
 * - Logging de errores
 * 
 * Este cliente funciona como un "axios minimalista" y puede
 * extenderse con interceptores, retries o tracing más adelante.
 */

// Configuración de la API Backend
// @ts-ignore - Vite env variables
const API_BASE_URL = import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5076';

/**
 * Función para obtener el token JWT
 * Puede ser reemplazada por una función desde session.ts si es necesario
 */
export function getToken(): string | null {
  return localStorage.getItem('jwt_token');
}

/**
 * Guarda el token JWT en localStorage
 */
export function setAuthToken(token: string): void {
  localStorage.setItem('jwt_token', token);
}

/**
 * Elimina el token JWT del localStorage
 */
export function clearAuthToken(): void {
  localStorage.removeItem('jwt_token');
}

/**
 * Obtiene el userId persistido (dev) para enviar como X-UserId
 * Se establece después de la primera llamada a /api/me
 */
export function getUserId(): string | null {
  return localStorage.getItem('user_id');
}

/**
 * Clase de error personalizada para respuestas HTTP no exitosas
 */
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public body: any
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Cliente HTTP centralizado con JWT automático
 * 
 * @param path - Ruta del endpoint (ej: '/api/tickets')
 * @param options - Opciones de fetch (método, headers, body, etc.)
 * @returns Promise con la respuesta parseada como tipo T
 * 
 * @example
 * // GET request
 * const tickets = await apiFetch<Ticket[]>('/api/tickets');
 * 
 * @example
 * // POST request
 * const newTicket = await apiFetch<Ticket>('/api/tickets', {
 *   method: 'POST',
 *   body: JSON.stringify({ title: 'New ticket' })
 * });
 * 
 * @example
 * // PATCH request
 * await apiFetch<void>('/api/tickets/1/status', {
 *   method: 'PATCH',
 *   body: JSON.stringify({ status: 'RESOLVED' })
 * });
 */
export async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  // Construir URL completa
  const url = `${API_BASE_URL}${path}`;
  
  // Obtener token JWT
  const token = getToken();
  const userId = getUserId();
  
  // Preparar headers
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>)
  };
  
  // Agregar Authorization header si el token está disponible
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  // Agregar X-UserId si está disponible (flujo dev sin JWT)
  if (userId) {
    headers['X-UserId'] = userId;
  }
  
  // Agregar Content-Type para POST y PATCH si no existe
  const method = options.method?.toUpperCase();
  if ((method === 'POST' || method === 'PATCH' || method === 'PUT') && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }
  
  try {
    // Hacer la petición HTTP
    const response = await fetch(url, {
      ...options,
      headers
    });
    
    // Verificar si la respuesta es exitosa
    if (!response.ok) {
      // Caso especial: 401 Unauthorized - limpiar token
      if (response.status === 401) {
        console.error('[apiClient] 401 Unauthorized - Token inválido o expirado');
        clearAuthToken();
      }
      
      // Intentar parsear el body del error
      let errorBody: any;
      try {
        errorBody = await response.json();
      } catch {
        errorBody = { error: 'Error desconocido', message: response.statusText };
      }
      
      // Logging del error
      console.error('[apiClient] HTTP Error:', {
        status: response.status,
        statusText: response.statusText,
        url,
        method: options.method || 'GET',
        body: errorBody
      });
      
      // Lanzar error personalizado con status y body
      const errorMessage = errorBody.error || errorBody.message || `HTTP ${response.status}`;
      throw new ApiError(errorMessage, response.status, errorBody);
    }
    
    // Caso especial: 204 No Content - no hay body
    if (response.status === 204) {
      return undefined as T;
    }
    
    // Parsear y retornar el JSON como tipo T
    return await response.json() as T;
    
  } catch (error) {
    // Si ya es un ApiError, re-lanzarlo
    if (error instanceof ApiError) {
      throw error;
    }
    
    // Error de red u otro error (ej: no se pudo conectar al servidor)
    console.error('[apiClient] Network or unexpected error:', {
      url,
      method: options.method || 'GET',
      error
    });
    
    throw error;
  }
}
