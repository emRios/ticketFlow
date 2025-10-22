# Endpoints API - TicketFlow Backend

Este documento describe los **endpoints MVP** del backend de TicketFlow. Cada endpoint incluye propósito, roles requeridos, request/response, errores posibles y side-effects (eventos publicados, auditorías).

## Especificación Completa

Para detalles técnicos completos (schemas, ejemplos, validaciones), consulta:  
📄 **[/contracts/openapi.yaml](../../../contracts/openapi.yaml)**

---

## Tabla de Contenidos

1. [POST /tickets](#1-post-tickets)
2. [GET /tickets](#2-get-tickets)
3. [PATCH /tickets/{id}/status](#3-patch-ticketsidstatus)
4. [PATCH /tickets/{id}](#4-patch-ticketsid)
5. [POST /tickets/{id}/tags](#5-post-ticketsidtags)
6. [DELETE /tickets/{id}/tags/{tagId}](#6-delete-ticketsidtagstagid)
7. [GET /tickets/{id}/audit-logs](#7-get-ticketsidaudit-logs)
8. [GET /users](#8-get-users)
9. [GET /health](#9-get-health)

---

## 1. POST /tickets

### Propósito
Crear un nuevo ticket. Si el cliente no especifica `assignedTo`, el sistema **auto-asigna** a un agente disponible usando el algoritmo de score (ver [docs/assignment-strategy.md](../../../docs/assignment-strategy.md)).

### Roles Requeridos
- `ADMIN` (puede asignar manualmente)
- `AGENT` (puede crear tickets para otros)
- `CLIENT` (solo crea tickets sin asignar, el sistema auto-asigna)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>",
  "Content-Type": "application/json"
}
```

**Body:**
```json
{
  "title": "No puedo hacer login",
  "description": "Al intentar ingresar con mi email, recibo error 500",
  "priority": "HIGH",
  "assignedTo": "u002",  // Opcional. Si se omite, auto-asigna
  "tagIds": ["t001", "t004"]  // Opcional
}
```

**Validaciones:**
- `title`: requerido, max 200 caracteres
- `description`: requerido, max 2000 caracteres
- `priority`: enum (`LOW`, `MEDIUM`, `HIGH`, `URGENT`)
- `assignedTo`: debe ser userId con role `AGENT`, si se especifica
- `tagIds`: array de IDs existentes en tabla `Tags`

### Response

**201 Created:**
```json
{
  "id": "tk-202501-0042",
  "title": "No puedo hacer login",
  "description": "Al intentar ingresar con mi email, recibo error 500",
  "status": "NEW",
  "priority": "HIGH",
  "createdBy": "u004",
  "assignedTo": "u002",
  "tags": [
    { "id": "t001", "name": "login", "color": "#FF5733" },
    { "id": "t004", "name": "bug", "color": "#E74C3C" }
  ],
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z"
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación fallida | `"title is required"`, `"priority must be LOW/MEDIUM/HIGH/URGENT"` |
| **403** | CLIENT intenta asignar manualmente | `"Clients cannot assign tickets manually"` |
| **404** | assignedTo no existe o no es AGENT | `"User u999 not found or is not an agent"` |
| **404** | tagId no existe | `"Tag t999 not found"` |
| **500** | Error interno (DB, auto-assignment falló) | `"Failed to auto-assign ticket"` |

### Side-Effects

1. **Evento publicado:**
   ```json
   {
     "eventType": "ticket.created",
     "aggregateId": "tk-202501-0042",
     "payload": { "status": "NEW", "assignedTo": "u002", "priority": "HIGH" }
   }
   ```

2. **Auditoría:**
   - Se crea registro en `AuditLogs`:
     ```
     action=CREATE, userId=u004, changes='{"status":"NEW","assignedTo":"u002"}'
     ```

3. **Outbox:**
   - Registro en tabla `Outbox` para garantizar entrega del evento.

---

## 2. GET /tickets

### Propósito
Listar tickets con filtros opcionales (status, assignedTo, priority, tags) y paginación.

### Roles Requeridos
- `ADMIN` (ve todos los tickets)
- `AGENT` (ve solo tickets asignados a él/ella)
- `CLIENT` (ve solo tickets creados por él/ella)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Params:**
```
GET /tickets?status=NEW&assignedTo=u002&priority=HIGH&tags=t001,t004&page=1&pageSize=20
```

**Parámetros opcionales:**
- `status`: filtrar por estado (`NEW`, `IN_PROGRESS`, `ON_HOLD`, `RESOLVED`)
- `assignedTo`: filtrar por userId asignado
- `priority`: filtrar por prioridad
- `tags`: filtrar por tagIds (comma-separated)
- `page`: número de página (default: 1)
- `pageSize`: tamaño de página (default: 20, max: 100)

### Response

**200 OK:**
```json
{
  "data": [
    {
      "id": "tk-202501-0042",
      "title": "No puedo hacer login",
      "status": "NEW",
      "priority": "HIGH",
      "assignedTo": "u002",
      "createdBy": "u004",
      "tags": [
        { "id": "t001", "name": "login", "color": "#FF5733" }
      ],
      "createdAt": "2025-01-15T10:30:00Z",
      "updatedAt": "2025-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 42,
    "totalPages": 3
  }
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación de query params | `"pageSize must be <= 100"`, `"status must be NEW/IN_PROGRESS/ON_HOLD/RESOLVED"` |
| **403** | AGENT/CLIENT intenta ver tickets de otros | `"Forbidden: you can only view your own tickets"` |
| **500** | Error interno (DB) | `"Failed to fetch tickets"` |

### Side-Effects

Ninguno (operación de solo lectura).

---

## 3. PATCH /tickets/{id}/status

### Propósito
Cambiar el estado de un ticket siguiendo las reglas de la **máquina de estados** (ver [docs/state-machine.md](../../../docs/state-machine.md)).

### Roles Requeridos
- `ADMIN` (puede cambiar cualquier ticket)
- `AGENT` (solo tickets asignados a él/ella)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>",
  "Content-Type": "application/json"
}
```

**Body:**
```json
{
  "newStatus": "IN_PROGRESS",
  "comment": "Comenzando a investigar el problema"  // Opcional
}
```

**Validaciones:**
- `newStatus`: requerido, enum (`NEW`, `IN_PROGRESS`, `ON_HOLD`, `RESOLVED`)
- La transición debe ser válida según FSM (ver matriz en state-machine.md)

### Response

**200 OK:**
```json
{
  "id": "tk-202501-0042",
  "status": "IN_PROGRESS",
  "updatedAt": "2025-01-15T11:00:00Z",
  "updatedBy": "u002"
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación fallida | `"newStatus is required"` |
| **403** | AGENT intenta cambiar ticket de otro | `"Forbidden: ticket is not assigned to you"` |
| **404** | Ticket no existe | `"Ticket tk-999 not found"` |
| **409** | Transición inválida | `"Cannot transition from RESOLVED to NEW"` (código error: `E_INVALID_TRANSITION`) |
| **500** | Error interno (DB, evento falló) | `"Failed to update ticket status"` |

### Side-Effects

1. **Evento publicado:**
   ```json
   {
     "eventType": "ticket.status_changed",
     "aggregateId": "tk-202501-0042",
     "payload": { "oldStatus": "NEW", "newStatus": "IN_PROGRESS", "userId": "u002" }
   }
   ```

2. **Auditoría:**
   ```
   action=UPDATE_STATUS, userId=u002, changes='{"status":{"old":"NEW","new":"IN_PROGRESS"}}'
   ```

3. **Outbox:**
   - Registro en tabla `Outbox`.

---

## 4. PATCH /tickets/{id}

### Propósito
Actualizar campos de un ticket (title, description, priority, assignedTo). **No cambia el status** (usar endpoint `/status` para eso).

### Roles Requeridos
- `ADMIN` (puede editar cualquier ticket)
- `AGENT` (solo tickets asignados a él/ella, no puede cambiar `assignedTo`)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>",
  "Content-Type": "application/json"
}
```

**Body (todos los campos opcionales):**
```json
{
  "title": "Nuevo título actualizado",
  "description": "Descripción más detallada",
  "priority": "URGENT",
  "assignedTo": "u003"  // Solo ADMIN puede cambiar
}
```

### Response

**200 OK:**
```json
{
  "id": "tk-202501-0042",
  "title": "Nuevo título actualizado",
  "description": "Descripción más detallada",
  "priority": "URGENT",
  "assignedTo": "u003",
  "updatedAt": "2025-01-15T11:15:00Z",
  "updatedBy": "u001"
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación fallida | `"title max length is 200"`, `"priority must be LOW/MEDIUM/HIGH/URGENT"` |
| **403** | AGENT intenta cambiar `assignedTo` | `"Forbidden: only admins can reassign tickets"` |
| **403** | AGENT intenta editar ticket de otro | `"Forbidden: ticket is not assigned to you"` |
| **404** | Ticket no existe | `"Ticket tk-999 not found"` |
| **404** | assignedTo no existe o no es AGENT | `"User u999 not found or is not an agent"` |
| **500** | Error interno (DB) | `"Failed to update ticket"` |

### Side-Effects

1. **Evento publicado (si cambió assignedTo):**
   ```json
   {
     "eventType": "ticket.reassigned",
     "aggregateId": "tk-202501-0042",
     "payload": { "oldAssignedTo": "u002", "newAssignedTo": "u003", "userId": "u001" }
   }
   ```

2. **Auditoría:**
   ```
   action=UPDATE, userId=u001, changes='{"title":{"old":"...","new":"..."},"assignedTo":{"old":"u002","new":"u003"}}'
   ```

3. **Outbox:**
   - Registro en tabla `Outbox` (si hubo evento).

---

## 5. POST /tickets/{id}/tags

### Propósito
Agregar uno o más tags a un ticket existente.

### Roles Requeridos
- `ADMIN` (cualquier ticket)
- `AGENT` (solo tickets asignados a él/ella)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>",
  "Content-Type": "application/json"
}
```

**Body:**
```json
{
  "tagIds": ["t002", "t003"]
}
```

### Response

**200 OK:**
```json
{
  "ticketId": "tk-202501-0042",
  "tags": [
    { "id": "t001", "name": "login", "color": "#FF5733" },
    { "id": "t002", "name": "urgente", "color": "#FFC300" },
    { "id": "t003", "name": "pagos", "color": "#28B463" }
  ]
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación fallida | `"tagIds is required"`, `"tagIds must be an array"` |
| **403** | AGENT intenta editar ticket de otro | `"Forbidden: ticket is not assigned to you"` |
| **404** | Ticket no existe | `"Ticket tk-999 not found"` |
| **404** | Tag no existe | `"Tag t999 not found"` |
| **409** | Tag ya está asociado | `"Tag t002 is already associated with this ticket"` |
| **500** | Error interno (DB) | `"Failed to add tags"` |

### Side-Effects

1. **Evento publicado:**
   ```json
   {
     "eventType": "ticket.tags_added",
     "aggregateId": "tk-202501-0042",
     "payload": { "tagIds": ["t002", "t003"], "userId": "u002" }
   }
   ```

2. **Auditoría:**
   ```
   action=ADD_TAGS, userId=u002, changes='{"tagsAdded":["t002","t003"]}'
   ```

3. **Outbox:**
   - Registro en tabla `Outbox`.

---

## 6. DELETE /tickets/{id}/tags/{tagId}

### Propósito
Remover un tag de un ticket.

### Roles Requeridos
- `ADMIN` (cualquier ticket)
- `AGENT` (solo tickets asignados a él/ella)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Path Params:**
- `id`: ticketId
- `tagId`: ID del tag a remover

### Response

**204 No Content**

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **403** | AGENT intenta editar ticket de otro | `"Forbidden: ticket is not assigned to you"` |
| **404** | Ticket no existe | `"Ticket tk-999 not found"` |
| **404** | Tag no existe | `"Tag t999 not found"` |
| **404** | Tag no está asociado al ticket | `"Tag t002 is not associated with this ticket"` |
| **500** | Error interno (DB) | `"Failed to remove tag"` |

### Side-Effects

1. **Evento publicado:**
   ```json
   {
     "eventType": "ticket.tag_removed",
     "aggregateId": "tk-202501-0042",
     "payload": { "tagId": "t002", "userId": "u002" }
   }
   ```

2. **Auditoría:**
   ```
   action=REMOVE_TAG, userId=u002, changes='{"tagRemoved":"t002"}'
   ```

3. **Outbox:**
   - Registro en tabla `Outbox`.

---

## 7. GET /tickets/{id}/audit-logs

### Propósito
Obtener el historial de cambios (auditoría) de un ticket.

### Roles Requeridos
- `ADMIN` (cualquier ticket)
- `AGENT` (solo tickets asignados a él/ella)
- `CLIENT` (solo tickets creados por él/ella)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Params (opcionales):**
```
GET /tickets/tk-202501-0042/audit-logs?page=1&pageSize=50
```

### Response

**200 OK:**
```json
{
  "data": [
    {
      "id": "al-001",
      "ticketId": "tk-202501-0042",
      "action": "CREATE",
      "userId": "u004",
      "userName": "Juan Perez",
      "changes": "{\"status\":\"NEW\",\"assignedTo\":\"u002\"}",
      "timestamp": "2025-01-15T10:30:00Z"
    },
    {
      "id": "al-002",
      "ticketId": "tk-202501-0042",
      "action": "UPDATE_STATUS",
      "userId": "u002",
      "userName": "Maria Lopez",
      "changes": "{\"status\":{\"old\":\"NEW\",\"new\":\"IN_PROGRESS\"}}",
      "timestamp": "2025-01-15T11:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 5,
    "totalPages": 1
  }
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **403** | Usuario no tiene permiso para ver este ticket | `"Forbidden: you cannot view this ticket's audit logs"` |
| **404** | Ticket no existe | `"Ticket tk-999 not found"` |
| **500** | Error interno (DB) | `"Failed to fetch audit logs"` |

### Side-Effects

Ninguno (operación de solo lectura).

---

## 8. GET /users

### Propósito
Listar usuarios filtrados por rol (principalmente para obtener lista de agentes disponibles para asignación).

### Roles Requeridos
- `ADMIN` (puede listar todos los roles)
- `AGENT` (puede listar solo AGENT y CLIENT)
- `CLIENT` (solo puede listar AGENT para ver quién está asignado)

### Request

**Headers:**
```json
{
  "Authorization": "Bearer <JWT_TOKEN>"
}
```

**Query Params:**
```
GET /users?role=AGENT&active=true
```

**Parámetros opcionales:**
- `role`: filtrar por rol (`ADMIN`, `AGENT`, `CLIENT`)
- `active`: filtrar por usuarios activos (true/false)

### Response

**200 OK:**
```json
{
  "data": [
    {
      "id": "u002",
      "name": "Maria Lopez",
      "email": "maria@ticketflow.com",
      "role": "AGENT",
      "isActive": true
    },
    {
      "id": "u003",
      "name": "Carlos Ruiz",
      "email": "carlos@ticketflow.com",
      "role": "AGENT",
      "isActive": true
    }
  ]
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **400** | Validación fallida | `"role must be ADMIN/AGENT/CLIENT"` |
| **403** | CLIENT intenta listar ADMIN | `"Forbidden: clients cannot view admin users"` |
| **500** | Error interno (DB) | `"Failed to fetch users"` |

### Side-Effects

Ninguno (operación de solo lectura).

---

## 9. GET /health

### Propósito
Health check del servicio API. Verifica conectividad con PostgreSQL y RabbitMQ.

### Roles Requeridos
Ninguno (endpoint público para monitoreo).

### Request

**Headers:**
Ninguno requerido.

### Response

**200 OK (healthy):**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-15T12:00:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "healthy",
    "rabbitmq": "healthy"
  }
}
```

**503 Service Unavailable (unhealthy):**
```json
{
  "status": "unhealthy",
  "timestamp": "2025-01-15T12:00:00Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "unhealthy",
    "rabbitmq": "healthy"
  },
  "errors": [
    "PostgreSQL connection failed: timeout"
  ]
}
```

### Errores

| Código | Descripción | Ejemplo |
|--------|-------------|---------|
| **503** | Servicio no saludable | Uno o más dependencies están `unhealthy` |

### Side-Effects

Ninguno (operación de solo lectura, no escribe en DB ni publica eventos).

---

## Resumen de Side-Effects por Endpoint

| Endpoint | Evento Publicado | Auditoría | Outbox |
|----------|------------------|-----------|--------|
| **POST /tickets** | `ticket.created` | ✅ CREATE | ✅ |
| **GET /tickets** | - | - | - |
| **PATCH /tickets/{id}/status** | `ticket.status_changed` | ✅ UPDATE_STATUS | ✅ |
| **PATCH /tickets/{id}** | `ticket.reassigned` (si cambió assignedTo) | ✅ UPDATE | ✅ (condicional) |
| **POST /tickets/{id}/tags** | `ticket.tags_added` | ✅ ADD_TAGS | ✅ |
| **DELETE /tickets/{id}/tags/{tagId}** | `ticket.tag_removed` | ✅ REMOVE_TAG | ✅ |
| **GET /tickets/{id}/audit-logs** | - | - | - |
| **GET /users** | - | - | - |
| **GET /health** | - | - | - |

---

## Documentos Relacionados

- 📄 **[/contracts/openapi.yaml](../../../contracts/openapi.yaml)** - Especificación OpenAPI 3.0.3 completa
- 📄 **[/docs/state-machine.md](../../../docs/state-machine.md)** - Reglas de transición de estados
- 📄 **[/docs/assignment-strategy.md](../../../docs/assignment-strategy.md)** - Algoritmo de auto-asignación
- 📄 **[/docs/data-model.md](../../../docs/data-model.md)** - Esquema de base de datos
- 📄 **[/docs/health.md](../../../docs/health.md)** - Contratos de health checks
- 📄 **[/docs/metrics.md](../../../docs/metrics.md)** - Métricas operacionales

---

## Implementación

Los endpoints se implementarán usando **ASP.NET Core Minimal APIs** en:
- `backend/src/Api/Endpoints/TicketsEndpoints.cs`
- `backend/src/Api/Endpoints/UsersEndpoints.cs`
- `backend/src/Api/Endpoints/HealthEndpoints.cs`

Cada endpoint delegará la lógica de negocio a **Application Use Cases** siguiendo Clean Architecture:
```
Api (Endpoints) → Application (Use Cases) → Domain (Entities + Rules) → Infrastructure (DB + MQ)
```

**Autenticación:**  
Todos los endpoints (excepto `/health`) requieren **JWT Bearer token** en header `Authorization`.

**Autorización:**  
Implementada con **ASP.NET Core Policies** basados en roles (`ADMIN`, `AGENT`, `CLIENT`).

**Validación:**  
Usando **FluentValidation** en capa Application.

**Outbox Pattern:**  
Worker procesa tabla `Outbox` y publica eventos a RabbitMQ (ver [docs/adr/adr-0002-rabbitmq-y-outbox.md](../../../docs/adr/adr-0002-rabbitmq-y-outbox.md)).
