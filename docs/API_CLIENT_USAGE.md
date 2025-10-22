# apiClient.ts - Cliente HTTP Centralizado

## 📋 Descripción

Cliente HTTP minimalista para consumir la API backend desde el frontend. Funciona como un "axios simplificado" con manejo automático de JWT, headers, y errores.

## ✨ Características

- ✅ **JWT Automático**: Lee el token desde `localStorage` y agrega el header `Authorization`
- ✅ **Headers Inteligentes**: Agrega `Content-Type: application/json` automáticamente para POST/PATCH/PUT
- ✅ **Manejo de Errores**: Lanza excepciones con status y body incluidos
- ✅ **Logging**: Console.error automático para debugging
- ✅ **TypeScript Genérico**: Retorna tipos específicos con `<T>`
- ✅ **401 Handling**: Limpia el token automáticamente si es inválido
- ✅ **204 No Content**: Maneja respuestas sin body correctamente

## 📦 Exportaciones

```typescript
// Función principal
export function apiFetch<T>(path: string, options?: RequestInit): Promise<T>

// Utilidades
export function getToken(): string | null
export function setAuthToken(token: string): void
export function clearAuthToken(): void

// Error personalizado
export class ApiError extends Error {
  status: number
  body: any
}
```

## 🚀 Ejemplos de Uso

### 1. GET Request Simple

```typescript
// En board.ts
import { apiFetch } from './apiClient';
import type { TicketResponse } from './mappers';

export async function getBoard(scope: BoardScope = 'assigned'): Promise<BoardData> {
  try {
    // GET /api/tickets - automáticamente agrega JWT si existe
    const tickets = await apiFetch<TicketResponse[]>('/api/tickets');
    
    const mappedTickets = tickets.map((ticket, index) => 
      mapTicketResponseToDTO(ticket, index)
    );
    
    return {
      columns: getDefaultColumns(),
      tickets: filterTicketsByScope(mappedTickets, scope)
    };
  } catch (error) {
    console.error('Error obteniendo tablero:', error);
    // Fallback a mock data
    return getMockBoardData();
  }
}
```

### 2. GET Request con Query Params

```typescript
// En board.ts (versión mejorada con filtros)
export async function getBoard(scope: BoardScope = 'assigned'): Promise<BoardData> {
  // Construir query params según el scope
  const params = new URLSearchParams();
  if (scope === 'assigned') {
    params.append('assignedTo', currentUser?.userId || '');
  }
  
  // GET /api/tickets?assignedTo=u123
  const tickets = await apiFetch<TicketResponse[]>(
    `/api/tickets?${params.toString()}`
  );
  
  return processTickets(tickets);
}
```

### 3. POST Request

```typescript
// En tickets.ts
import { apiFetch } from './apiClient';

export async function createTicket(data: {
  title: string;
  description: string;
  priority?: string;
}): Promise<{ id: number }> {
  try {
    // POST /api/tickets
    // apiFetch automáticamente agrega Content-Type: application/json
    return await apiFetch<{ id: number }>('/api/tickets', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  } catch (error) {
    console.error('Error creando ticket:', error);
    throw error;
  }
}

// Uso:
const newTicket = await createTicket({
  title: 'Bug en login',
  description: 'Usuario no puede iniciar sesión',
  priority: 'HIGH'
});
console.log('Ticket creado con ID:', newTicket.id);
```

### 4. PATCH Request

```typescript
// En tickets.ts
export async function updateTicketStatus(
  ticketId: string,
  newStatus: string
): Promise<void> {
  // Extraer ID numérico
  const id = ticketId.replace('TF-', '');
  
  // Mapear columnId a status del backend
  const statusMapping: Record<string, string> = {
    'nuevo': 'OPEN',
    'en-proceso': 'IN_PROGRESS',
    'en-espera': 'WAITING',
    'resuelto': 'RESOLVED'
  };
  
  const backendStatus = statusMapping[newStatus] || 'OPEN';
  
  // PATCH /api/tickets/123/status
  // Retorna void porque el endpoint responde 204 No Content
  await apiFetch<void>(`/api/tickets/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status: backendStatus })
  });
  
  console.log(`✅ Ticket ${ticketId} actualizado a ${newStatus}`);
}
```

### 5. GET Request con Autenticación (me.ts)

```typescript
// En me.ts
import { apiFetch } from './apiClient';
import { mapUserResponseToCurrentUser, type UserResponse } from './mappers';

export async function getMe(): Promise<CurrentUser> {
  try {
    // GET /api/me
    // apiFetch automáticamente agrega: Authorization: Bearer <token>
    const userResponse = await apiFetch<UserResponse>('/api/me');
    return mapUserResponseToCurrentUser(userResponse);
  } catch (error) {
    console.error('Error obteniendo usuario actual:', error);
    
    // Fallback a mock si hay error
    return getMockUser();
  }
}
```

### 6. Manejo Avanzado de Errores

```typescript
import { apiFetch, ApiError } from './apiClient';

async function fetchTickets() {
  try {
    const tickets = await apiFetch<Ticket[]>('/api/tickets');
    return tickets;
  } catch (error) {
    // Verificar si es un ApiError
    if (error instanceof ApiError) {
      // Acceso a status y body del error
      console.error('Status:', error.status);
      console.error('Body:', error.body);
      
      // Manejo específico según el status
      switch (error.status) {
        case 401:
          // Token inválido - redirigir a login
          window.location.href = '/login';
          break;
        case 403:
          // No autorizado - mostrar mensaje
          alert('No tienes permisos para esta acción');
          break;
        case 404:
          // No encontrado
          alert('Recurso no encontrado');
          break;
        case 500:
          // Error del servidor
          alert('Error del servidor. Intenta más tarde.');
          break;
        default:
          alert(`Error: ${error.message}`);
      }
    } else {
      // Error de red u otro error
      console.error('Network error:', error);
      alert('No se pudo conectar al servidor');
    }
    
    // Re-lanzar el error si es necesario
    throw error;
  }
}
```

### 7. PUT Request (Actualización Completa)

```typescript
export async function updateTicket(
  ticketId: string,
  data: Partial<TicketRequest>
): Promise<Ticket> {
  const id = ticketId.replace('TF-', '');
  
  return await apiFetch<Ticket>(`/api/tickets/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}
```

### 8. DELETE Request

```typescript
export async function deleteTicket(ticketId: string): Promise<void> {
  const id = ticketId.replace('TF-', '');
  
  await apiFetch<void>(`/api/tickets/${id}`, {
    method: 'DELETE'
  });
  
  console.log(`🗑️ Ticket ${ticketId} eliminado`);
}
```

### 9. Headers Personalizados

```typescript
// Agregar headers adicionales
const data = await apiFetch<Response>('/api/special-endpoint', {
  method: 'POST',
  headers: {
    'X-Custom-Header': 'value',
    'Accept-Language': 'es-ES'
  },
  body: JSON.stringify({ data: 'test' })
});

// Note: Authorization y Content-Type se agregan automáticamente
// Resultado: 
// Authorization: Bearer <token>
// Content-Type: application/json
// X-Custom-Header: value
// Accept-Language: es-ES
```

### 10. Request sin JWT (público)

```typescript
// Si no hay token en localStorage, la petición se hace sin Authorization
// Útil para endpoints públicos como /api/health o /api/version

export async function getHealthStatus(): Promise<{ status: string }> {
  // No requiere JWT - funciona sin token
  return await apiFetch<{ status: string }>('/api/health');
}
```

## 🔍 Logging Automático

El cliente logea automáticamente los errores en consola:

```javascript
// Ejemplo de error 401:
[apiClient] 401 Unauthorized - Token inválido o expirado
[apiClient] HTTP Error: {
  status: 401,
  statusText: "Unauthorized",
  url: "http://localhost:5000/api/me",
  method: "GET",
  body: { error: "Token expired" }
}

// Ejemplo de error de red:
[apiClient] Network or unexpected error: {
  url: "http://localhost:5000/api/tickets",
  method: "GET",
  error: TypeError: Failed to fetch
}
```

## ⚙️ Configuración

### Cambiar el Base URL

Crea un archivo `.env.local` en `src/web/`:

```bash
VITE_API_BASE_URL=http://localhost:5000
```

O para producción:

```bash
VITE_API_BASE_URL=https://api.ticketflow.com
```

### Usar una función custom para obtener el token

Si quieres obtener el token desde otro lugar (ej: session.ts):

```typescript
// En apiClient.ts, reemplaza getToken():
import { getSessionToken } from '../state/session';

export function getToken(): string | null {
  return getSessionToken(); // Tu función custom
}
```

## 🎯 Ventajas sobre fetch() directo

| Aspecto | fetch() directo | apiFetch() |
|---------|----------------|-----------|
| JWT | ❌ Manual en cada llamada | ✅ Automático |
| Content-Type | ❌ Manual | ✅ Automático para POST/PATCH |
| Error handling | ❌ Manual con if (!res.ok) | ✅ Automático con throw |
| Logging | ❌ Manual | ✅ Automático |
| 401 handling | ❌ Manual | ✅ Limpia token automáticamente |
| Type safety | ⚠️ Requiere casting | ✅ Genéricos <T> |
| Base URL | ❌ Repetir en cada llamada | ✅ Centralizado |

## 🚀 Extensiones Futuras

Este cliente puede extenderse fácilmente con:

1. **Interceptores**: Ejecutar código antes/después de cada request
2. **Retries**: Reintentar automáticamente si falla
3. **Caching**: Cachear respuestas GET
4. **Request Queue**: Encolar requests cuando no hay conexión
5. **Progress Events**: Para uploads grandes
6. **Tracing**: Integración con herramientas de APM
7. **Mock Mode**: Retornar datos mock si API no está disponible

Ejemplo de interceptor simple:

```typescript
// Agregar timestamp a cada request
const originalApiFetch = apiFetch;
export const apiFetch = async <T>(path: string, options: RequestInit = {}): Promise<T> => {
  const startTime = Date.now();
  
  try {
    const result = await originalApiFetch<T>(path, options);
    console.log(`Request to ${path} took ${Date.now() - startTime}ms`);
    return result;
  } catch (error) {
    console.error(`Request to ${path} failed after ${Date.now() - startTime}ms`);
    throw error;
  }
};
```

## 📚 Archivos Relacionados

- `src/web/apps/demo-vanilla/api/apiClient.ts` - El cliente (este archivo)
- `src/web/apps/demo-vanilla/api/board.ts` - Ejemplo de uso con GET
- `src/web/apps/demo-vanilla/api/me.ts` - Ejemplo de uso con autenticación
- `src/web/apps/demo-vanilla/api/tickets.ts` - Ejemplo de uso con POST/PATCH
- `src/web/apps/demo-vanilla/state/session.ts` - Manejo de token (opcional)

---

**Última actualización:** 22 de octubre de 2025
