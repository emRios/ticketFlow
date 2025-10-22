# Contracts - TicketFlow

Definiciones compartidas de DTOs, eventos y contratos de API entre frontend, backend y worker.

---

## Event Envelope (Eventos de Mensajería)

Todos los eventos publicados a RabbitMQ siguen este formato común.

### Estructura del Envelope

```json
{
  "eventId": "evt-20251021153000-abc123def456",
  "eventType": "ticket.created | ticket.status.changed | ticket.assigned",
  "occurredAt": "2025-10-21T15:30:00.123Z",
  "correlationId": "api-20251021153000-abc123",
  "actor": {
    "id": "u123",
    "role": "AGENT | ADMIN | CLIENT"
  },
  "ticket": {
    "id": "TF-1024",
    "code": "TF-1024",
    "title": "Fallo en pagos",
    "status": "nuevo | en-proceso | en-espera | resuelto",
    "priority": "low | medium | high | urgent",
    "assignedTo": "u123"
  },
  "changes": {
    "from": "en-proceso",
    "to": "en-espera"
  }
}
```

### Campos del Envelope

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `eventId` | string | ✅ | ID único del evento (idempotencia) |
| `eventType` | string | ✅ | Tipo de evento (routing key) |
| `occurredAt` | ISO 8601 | ✅ | Timestamp UTC de cuando ocurrió el evento |
| `correlationId` | string | ✅ | ID para rastreo distribuido |
| `actor` | object | ✅ | Usuario que ejecutó la acción |
| `actor.id` | string | ✅ | ID del usuario |
| `actor.role` | enum | ✅ | Rol del usuario |
| `ticket` | object | ✅ | Estado actual del ticket después del evento |
| `ticket.id` | string | ✅ | ID interno del ticket |
| `ticket.code` | string | ✅ | Código visible del ticket (ej: TF-1024) |
| `ticket.title` | string | ✅ | Título del ticket |
| `ticket.status` | enum | ✅ | Estado actual |
| `ticket.priority` | enum | ✅ | Prioridad actual |
| `ticket.assignedTo` | string | ❌ | ID del agente asignado (null si no está asignado) |
| `changes` | object | ❌ | Cambios realizados (solo para eventos de actualización) |
| `changes.from` | string | ❌ | Valor anterior |
| `changes.to` | string | ❌ | Valor nuevo |

---

## Ejemplos de Eventos

### 1. Evento: `ticket.created`

**Routing Key**: `ticket.created`

**Cuándo se dispara**: Al crear un nuevo ticket en el sistema.

```json
{
  "eventId": "evt-20251021153045-abc123def456",
  "eventType": "ticket.created",
  "occurredAt": "2025-10-21T15:30:45.123Z",
  "correlationId": "api-20251021153045-abc123",
  "actor": {
    "id": "u123",
    "role": "AGENT"
  },
  "ticket": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "code": "TF-1024",
    "title": "Fallo en pagos con tarjeta Visa",
    "status": "nuevo",
    "priority": "urgent",
    "assignedTo": "u123"
  }
}
```

**Consumidores**:
- `notifications` → Enviar email/push al agente asignado
- `metrics` → Incrementar contador de tickets creados

---

### 2. Evento: `ticket.status.changed`

**Routing Key**: `ticket.status.changed`

**Cuándo se dispara**: Al mover un ticket entre columnas/estados.

```json
{
  "eventId": "evt-20251021154230-def456ghi789",
  "eventType": "ticket.status.changed",
  "occurredAt": "2025-10-21T15:42:30.456Z",
  "correlationId": "api-20251021154230-def456",
  "actor": {
    "id": "u123",
    "role": "AGENT"
  },
  "ticket": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "code": "TF-1024",
    "title": "Fallo en pagos con tarjeta Visa",
    "status": "en-espera",
    "priority": "urgent",
    "assignedTo": "u123"
  },
  "changes": {
    "from": "en-proceso",
    "to": "en-espera"
  }
}
```

**Consumidores**:
- `notifications` → Notificar al cliente que el ticket está en espera
- `metrics` → Actualizar métricas de tickets por estado

---

### 3. Evento: `ticket.assigned`

**Routing Key**: `ticket.assigned`

**Cuándo se dispara**: Al asignar/reasignar un ticket a un agente.

```json
{
  "eventId": "evt-20251021155515-ghi789jkl012",
  "eventType": "ticket.assigned",
  "occurredAt": "2025-10-21T15:55:15.789Z",
  "correlationId": "api-20251021155515-ghi789",
  "actor": {
    "id": "u456",
    "role": "ADMIN"
  },
  "ticket": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "code": "TF-1024",
    "title": "Fallo en pagos con tarjeta Visa",
    "status": "en-proceso",
    "priority": "urgent",
    "assignedTo": "u789"
  },
  "changes": {
    "from": "u123",
    "to": "u789"
  }
}
```

**Consumidores**:
- `notifications` → Notificar al nuevo agente que tiene un ticket asignado
- `metrics` → Actualizar carga de trabajo por agente

---

## API REST - Contratos

### Autenticación

Todos los endpoints requieren **Bearer Token JWT** en el header:

```
Authorization: Bearer <jwt-token>
```

El token contiene:
- `userId`: ID del usuario autenticado
- `role`: Rol del usuario (AGENT, ADMIN, CLIENT)
- `teamIds`: IDs de equipos del usuario

---

### POST `/api/tickets` - Crear Ticket

Crea un nuevo ticket y lo auto-asigna según reglas de negocio.

#### Request

```http
POST /api/tickets HTTP/1.1
Content-Type: application/json
Authorization: Bearer <jwt-token>

{
  "title": "Fallo en pagos con tarjeta Visa",
  "description": "El cliente reporta que no puede completar el pago...",
  "priority": "urgent",
  "requesterId": "c88",
  "tags": ["pagos", "urgente"]
}
```

#### Request Body

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `title` | string | ✅ | Título del ticket (max 200 chars) |
| `description` | string | ❌ | Descripción detallada (max 5000 chars) |
| `priority` | enum | ✅ | `low`, `medium`, `high`, `urgent` |
| `requesterId` | string | ✅ | ID del cliente/requester |
| `tags` | string[] | ❌ | Array de tags (max 10) |

#### Response 201 Created

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "TF-1024",
  "title": "Fallo en pagos con tarjeta Visa",
  "description": "El cliente reporta que no puede completar el pago...",
  "status": "nuevo",
  "priority": "urgent",
  "assignedTo": {
    "id": "u123",
    "name": "Ana García",
    "avatarUrl": "https://example.com/avatars/u123.jpg"
  },
  "requester": {
    "id": "c88",
    "name": "Cliente SA"
  },
  "tags": [
    {
      "id": "tag-pagos",
      "label": "pagos",
      "color": "#ef4444"
    },
    {
      "id": "tag-urgente",
      "label": "urgente",
      "color": "#dc2626"
    }
  ],
  "createdAt": "2025-10-21T15:30:45.123Z",
  "updatedAt": "2025-10-21T15:30:45.123Z",
  "capabilities": {
    "move": true,
    "reorder": true,
    "assign": false,
    "addTag": true,
    "removeTag": true,
    "allowedTransitions": ["en-proceso", "en-espera"]
  }
}
```

---

### GET `/api/tickets` - Listar Tickets

Obtiene lista paginada de tickets con filtros.

#### Request

```http
GET /api/tickets?status=en-proceso&assignedTo=u123&tag=urgente&text=pago&page=1&pageSize=20 HTTP/1.1
Authorization: Bearer <jwt-token>
```

#### Query Parameters

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `status` | string | ❌ | Filtrar por estado: `nuevo`, `en-proceso`, `en-espera`, `resuelto` |
| `assignedTo` | string | ❌ | Filtrar por agente asignado (ID) |
| `tag` | string | ❌ | Filtrar por tag (label) |
| `text` | string | ❌ | Búsqueda de texto en título/descripción |
| `page` | number | ❌ | Número de página (default: 1) |
| `pageSize` | number | ❌ | Tamaño de página (default: 20, max: 100) |

#### Response 200 OK

```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "code": "TF-1024",
      "title": "Fallo en pagos con tarjeta Visa",
      "status": "en-proceso",
      "priority": "urgent",
      "assignedTo": {
        "id": "u123",
        "name": "Ana García",
        "avatarUrl": "https://example.com/avatars/u123.jpg"
      },
      "requester": {
        "id": "c88",
        "name": "Cliente SA"
      },
      "tags": [
        {
          "id": "tag-urgente",
          "label": "urgente",
          "color": "#dc2626"
        }
      ],
      "createdAt": "2025-10-21T15:30:45.123Z",
      "updatedAt": "2025-10-21T15:42:30.456Z",
      "capabilities": {
        "move": true,
        "reorder": true,
        "assign": false,
        "addTag": true,
        "removeTag": true,
        "allowedTransitions": ["nuevo", "en-espera", "resuelto"]
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 45,
    "totalPages": 3
  }
}
```

---

### PATCH `/api/tickets/{id}/status` - Cambiar Estado

Mueve un ticket a un nuevo estado/columna.

#### Request

```http
PATCH /api/tickets/550e8400-e29b-41d4-a716-446655440000/status HTTP/1.1
Content-Type: application/json
Authorization: Bearer <jwt-token>

{
  "newStatus": "en-espera",
  "reason": "Esperando respuesta del cliente"
}
```

#### Request Body

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `newStatus` | enum | ✅ | `nuevo`, `en-proceso`, `en-espera`, `resuelto` |
| `reason` | string | ❌ | Razón del cambio (max 500 chars) |

#### Response 200 OK

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "TF-1024",
  "title": "Fallo en pagos con tarjeta Visa",
  "status": "en-espera",
  "priority": "urgent",
  "assignedTo": {
    "id": "u123",
    "name": "Ana García"
  },
  "updatedAt": "2025-10-21T15:42:30.456Z",
  "capabilities": {
    "move": true,
    "allowedTransitions": ["en-proceso", "resuelto"]
  }
}
```

---

### PATCH `/api/tickets/{id}` - Actualizar Ticket

Actualiza campos del ticket (incluyendo asignación manual).

#### Request

```http
PATCH /api/tickets/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Content-Type: application/json
Authorization: Bearer <jwt-token>

{
  "assignedTo": "u789",
  "priority": "high"
}
```

#### Request Body

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `title` | string | ❌ | Nuevo título |
| `description` | string | ❌ | Nueva descripción |
| `assignedTo` | string | ❌ | ID del nuevo agente asignado |
| `priority` | enum | ❌ | Nueva prioridad |

#### Response 200 OK

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "TF-1024",
  "title": "Fallo en pagos con tarjeta Visa",
  "status": "en-proceso",
  "priority": "high",
  "assignedTo": {
    "id": "u789",
    "name": "Carlos Rodríguez",
    "avatarUrl": "https://example.com/avatars/u789.jpg"
  },
  "updatedAt": "2025-10-21T15:55:15.789Z",
  "capabilities": {
    "move": true,
    "assign": true,
    "allowedTransitions": ["nuevo", "en-espera", "resuelto"]
  }
}
```

---

### POST `/api/tickets/{id}/tags` - Agregar Tag

Agrega un tag al ticket.

#### Request

```http
POST /api/tickets/550e8400-e29b-41d4-a716-446655440000/tags HTTP/1.1
Content-Type: application/json
Authorization: Bearer <jwt-token>

{
  "tagId": "tag-pagos"
}
```

#### Request Body

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `tagId` | string | ✅ | ID del tag existente a agregar |

#### Response 200 OK

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "tags": [
    {
      "id": "tag-pagos",
      "label": "pagos",
      "color": "#ef4444"
    },
    {
      "id": "tag-urgente",
      "label": "urgente",
      "color": "#dc2626"
    }
  ],
  "updatedAt": "2025-10-21T16:10:00.000Z"
}
```

---

### DELETE `/api/tickets/{id}/tags/{tagId}` - Remover Tag

Remueve un tag del ticket.

#### Request

```http
DELETE /api/tickets/550e8400-e29b-41d4-a716-446655440000/tags/tag-urgente HTTP/1.1
Authorization: Bearer <jwt-token>
```

#### Response 204 No Content

---

### GET `/api/tickets/{id}/audit-logs` - Historial de Cambios

Obtiene el historial completo de cambios del ticket.

#### Request

```http
GET /api/tickets/550e8400-e29b-41d4-a716-446655440000/audit-logs HTTP/1.1
Authorization: Bearer <jwt-token>
```

#### Response 200 OK

```json
{
  "ticketId": "550e8400-e29b-41d4-a716-446655440000",
  "ticketCode": "TF-1024",
  "logs": [
    {
      "id": "log-001",
      "action": "created",
      "timestamp": "2025-10-21T15:30:45.123Z",
      "actor": {
        "id": "u123",
        "name": "Ana García",
        "role": "AGENT"
      },
      "changes": null
    },
    {
      "id": "log-002",
      "action": "status_changed",
      "timestamp": "2025-10-21T15:42:30.456Z",
      "actor": {
        "id": "u123",
        "name": "Ana García",
        "role": "AGENT"
      },
      "changes": {
        "field": "status",
        "from": "nuevo",
        "to": "en-proceso"
      }
    },
    {
      "id": "log-003",
      "action": "assigned",
      "timestamp": "2025-10-21T15:55:15.789Z",
      "actor": {
        "id": "u456",
        "name": "Admin Usuario",
        "role": "ADMIN"
      },
      "changes": {
        "field": "assignedTo",
        "from": "u123",
        "to": "u789"
      }
    }
  ]
}
```

---

### GET `/api/users?role=AGENT` - Listar Usuarios

Obtiene lista de usuarios filtrada por rol.

#### Request

```http
GET /api/users?role=AGENT HTTP/1.1
Authorization: Bearer <jwt-token>
```

#### Query Parameters

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `role` | enum | ❌ | Filtrar por rol: `AGENT`, `ADMIN`, `CLIENT` |

#### Response 200 OK

```json
{
  "users": [
    {
      "id": "u123",
      "name": "Ana García",
      "email": "ana.garcia@example.com",
      "role": "AGENT",
      "teamIds": ["t-ops", "t-support"],
      "avatarUrl": "https://example.com/avatars/u123.jpg",
      "isActive": true
    },
    {
      "id": "u789",
      "name": "Carlos Rodríguez",
      "email": "carlos.rodriguez@example.com",
      "role": "AGENT",
      "teamIds": ["t-ops"],
      "avatarUrl": "https://example.com/avatars/u789.jpg",
      "isActive": true
    }
  ]
}
```

---

## Formato de Error Estándar

Todos los errores siguen este formato consistente.

### Estructura del Error

```json
{
  "error": "ValidationError | NotFound | Forbidden | Conflict | InternalError",
  "message": "Mensaje legible para humanos",
  "correlationId": "api-20251021153000-abc123",
  "details": [
    {
      "field": "title",
      "issue": "Required"
    },
    {
      "field": "priority",
      "issue": "Must be one of: low, medium, high, urgent"
    }
  ]
}
```

### Códigos de Estado HTTP

| Código | Error Type | Descripción |
|--------|-----------|-------------|
| 400 | `ValidationError` | Datos de entrada inválidos |
| 404 | `NotFound` | Recurso no encontrado |
| 403 | `Forbidden` | Sin permisos para realizar la acción |
| 409 | `Conflict` | Conflicto de estado (ej: transición no permitida) |
| 500 | `InternalError` | Error interno del servidor |

---

### Ejemplos de Errores

#### 400 ValidationError

```json
{
  "error": "ValidationError",
  "message": "Los datos proporcionados son inválidos",
  "correlationId": "api-20251021160000-abc789",
  "details": [
    {
      "field": "title",
      "issue": "Required"
    },
    {
      "field": "priority",
      "issue": "Must be one of: low, medium, high, urgent"
    }
  ]
}
```

---

#### 404 NotFound

```json
{
  "error": "NotFound",
  "message": "Ticket con ID '550e8400-invalid' no encontrado",
  "correlationId": "api-20251021160030-def123",
  "details": []
}
```

---

#### 403 Forbidden

```json
{
  "error": "Forbidden",
  "message": "No tienes permisos para mover este ticket",
  "correlationId": "api-20251021160100-ghi456",
  "details": [
    {
      "field": "capabilities.move",
      "issue": "User does not have permission to move this ticket"
    }
  ]
}
```

---

#### 409 Conflict

```json
{
  "error": "Conflict",
  "message": "Transición de estado no permitida",
  "correlationId": "api-20251021160130-jkl789",
  "details": [
    {
      "field": "status",
      "issue": "Cannot transition from 'nuevo' to 'resuelto' directly. Allowed transitions: [en-proceso, en-espera]"
    }
  ]
}
```

---

#### 500 InternalError

```json
{
  "error": "InternalError",
  "message": "Error interno del servidor. Por favor contacte soporte.",
  "correlationId": "api-20251021160200-mno012",
  "details": []
}
```

---

## Notas de Implementación

### Validación JSON

Todos los ejemplos en este documento son **JSON válido** y pueden ser usados directamente para:
- Tests de integración
- Documentación interactiva (Swagger/OpenAPI)
- Validación de contratos con herramientas como JSON Schema

### Versionado de API

Se recomienda incluir versión en la URL o header:

```
GET /api/v1/tickets
```

O:

```
Accept: application/vnd.ticketflow.v1+json
```

### CORS

El backend debe configurar CORS para permitir requests desde el frontend:

```
Access-Control-Allow-Origin: https://app.ticketflow.com
Access-Control-Allow-Methods: GET, POST, PATCH, DELETE, OPTIONS
Access-Control-Allow-Headers: Authorization, Content-Type
```

### Rate Limiting

Endpoints públicos deben tener rate limiting:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1697901600
```

---

## Documentos Relacionados

Documentación técnica complementaria del sistema TicketFlow:

- **[Modelo de Datos](../docs/data-model.md)** - Estructura de tablas, relaciones e índices
- **[Máquina de Estados](../docs/state-machine.md)** - Transiciones válidas y reglas de negocio
- **[Heurística de Asignación](../docs/assignment-strategy.md)** - Algoritmo de auto-asignación de tickets
- **[Health Contracts](../docs/health.md)** - Endpoints de health check y formato de respuestas
- **[Métricas](../docs/metrics.md)** - Métricas operativas para Prometheus/Grafana

---

## Referencias

- [RFC 7807 - Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- [JSON Schema](https://json-schema.org/)
- [OpenAPI 3.0 Specification](https://swagger.io/specification/)
- [RabbitMQ Topic Exchange](https://www.rabbitmq.com/tutorials/tutorial-five-python.html)
