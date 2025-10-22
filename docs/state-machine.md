# Máquina de Estados del Ticket (MVP)

Definición de los estados posibles y transiciones válidas para los tickets en TicketFlow.

---

## Estados Disponibles

El ciclo de vida de un ticket en el sistema se compone de 4 estados:

| Estado | Descripción | Tipo |
|--------|-------------|------|
| **NEW** | Ticket recién creado, sin asignación confirmada | Inicial |
| **IN_PROGRESS** | Ticket en proceso de resolución por un agente | Activo |
| **ON_HOLD** | Ticket pausado temporalmente (espera de info, dependencia externa, etc.) | Activo |
| **RESOLVED** | Ticket resuelto y cerrado | Terminal |

---

## Transiciones Válidas

### Tabla de Transiciones

| Desde | Hacia | Acción | Restricción |
|-------|-------|--------|-------------|
| `NEW` | `IN_PROGRESS` | Agente toma el ticket | - |
| `IN_PROGRESS` | `ON_HOLD` | Pausar ticket | - |
| `ON_HOLD` | `IN_PROGRESS` | Reanudar trabajo | - |
| `IN_PROGRESS` | `RESOLVED` | Cerrar ticket | **AssignedTo no puede ser null** |
| `ON_HOLD` | `IN_PROGRESS` | Reanudar desde pausa | - |

### Diagrama ASCII

```
                    ┌─────────┐
                    │   NEW   │ (Estado inicial)
                    └────┬────┘
                         │
                         │ tomar ticket
                         ▼
                   ┌──────────────┐
             ┌────►│ IN_PROGRESS  │────┐
             │     └──────────────┘    │
             │            │            │
    reanudar │            │ pausar    │ resolver
             │            ▼            │ (requiere AssignedTo != null)
             │     ┌──────────────┐    │
             └─────│   ON_HOLD    │    │
                   └──────────────┘    │
                                       ▼
                                  ┌──────────┐
                                  │ RESOLVED │ (Estado terminal)
                                  └──────────┘
                                       ❌
                                  (no retorno)
```

**Leyenda**:
- `→` : Transición válida
- `↔` : Transición bidireccional
- `❌` : No hay transiciones desde este estado (terminal)

---

## Reglas de Negocio

### 1. Estado Terminal (RESOLVED)

**Regla**: Una vez que un ticket alcanza el estado `RESOLVED`, **no puede volver a ningún estado anterior**.

**Justificación**:
- Mantener integridad del historial
- Evitar reaperturas no controladas
- Simplificar métricas (tiempo de resolución)

**Alternativa**: Si se necesita reabrir un ticket, crear un nuevo ticket con referencia al anterior (campo `RelatedTicketId` en futuro).

### 2. AssignedTo Obligatorio para RESOLVED

**Regla**: Un ticket solo puede pasar a `RESOLVED` si tiene un agente asignado (`AssignedTo != null`).

**Validación**:
```
IF Status = IN_PROGRESS AND AssignedTo IS NULL:
  → 400 Bad Request "Cannot resolve ticket without assignee"
```

**Justificación**:
- Todo ticket resuelto debe tener responsable
- Métricas de performance por agente
- Historial de resoluciones

### 3. Registro de Auditoría

**Regla**: Cada transición de estado debe registrar un evento en `AuditLogs`.

**Campos requeridos**:
- `ActorId`: Usuario que ejecutó la transición
- `EntityType`: "Ticket"
- `EntityId`: ID del ticket
- `Action`: "status_changed"
- `BeforeJson`: `{ "status": "IN_PROGRESS", "updatedAt": "..." }`
- `AfterJson`: `{ "status": "RESOLVED", "updatedAt": "...", "closedAt": "..." }`
- `CorrelationId`: ID único de la operación

**Ejemplo**:
```json
{
  "id": "a1b2c3d4-...",
  "actorId": "u123",
  "entityType": "Ticket",
  "entityId": "TF-1024",
  "action": "status_changed",
  "beforeJson": {
    "status": "IN_PROGRESS",
    "updatedAt": "2025-10-21T10:30:00Z"
  },
  "afterJson": {
    "status": "RESOLVED",
    "updatedAt": "2025-10-21T11:45:00Z",
    "closedAt": "2025-10-21T11:45:00Z"
  },
  "at": "2025-10-21T11:45:00Z",
  "correlationId": "corr-abc-123"
}
```

### 4. Publicación de Eventos

**Regla**: Cada transición de estado debe publicar un evento `ticket.status.changed` vía Outbox.

**Event Payload**:
```json
{
  "eventId": "evt-xyz-789",
  "eventType": "ticket.status.changed",
  "occurredAt": "2025-10-21T11:45:00Z",
  "correlationId": "corr-abc-123",
  "actor": {
    "id": "u123",
    "name": "Ana García",
    "role": "AGENT"
  },
  "ticket": {
    "id": "t1",
    "code": "TF-1024",
    "title": "Problema con facturación",
    "status": "RESOLVED",
    "assignedTo": { "id": "u123", "name": "Ana García" }
  },
  "changes": {
    "status": {
      "from": "IN_PROGRESS",
      "to": "RESOLVED"
    }
  }
}
```

**Consumers**:
- `NotificationsConsumer`: Envía email al cliente notificando resolución
- `MetricsConsumer`: Actualiza métricas de tiempo de resolución

### 5. Asignación Manual de Agente

**Regla**: Cuando un ADMIN/AGENT asigna manualmente un ticket, se debe:

1. **Validar asignación**:
   - Usuario destino debe existir
   - Usuario destino debe tener rol `AGENT`
   - Usuario destino debe estar activo (`IsActive = true`)

2. **Registrar cambio**:
   - Insertar en `AuditLogs` con action `"assigned"`
   - BeforeJson: `{ "assignedTo": null }` o anterior assignee
   - AfterJson: `{ "assignedTo": { "id": "u456", "name": "Luis" } }`

3. **Publicar evento**:
   - Tipo: `ticket.assigned`
   - Payload con actor (quien asignó), ticket (estado actual), changes (assignedTo from/to)

**Ejemplo de cambio**:
```
PATCH /api/tickets/TF-1024/assign
Body: { "assigneeId": "u456" }

→ AuditLog: action="assigned"
→ Event: ticket.assigned
→ Notification: Email a u456 notificando nueva asignación
```

### 6. Auto-Asignación en Creación

**Regla**: Al crear un ticket, el sistema intenta asignar automáticamente un agente disponible.

**Flujo**:

```
1. POST /api/tickets
   Body: { title, description, priority, creatorId }

2. Backend ejecuta:
   a. Crear ticket con AssignedTo = null
   b. Ejecutar política de auto-asignación (Application/Policies)
   c. Si encuentra agente disponible:
      → Actualizar AssignedTo
      → Registrar AuditLog
      → Publicar ticket.assigned
   d. Si NO encuentra agente (todos ocupados):
      → Mantener AssignedTo = null
      → Publicar ticket.assignment.failed
      → Worker reintenta asíncronamente

3. Responder a cliente con ticket creado (con o sin asignación)
```

**Política de Auto-Asignación** (ejemplo MVP):
- Buscar agentes activos (`Role = AGENT`, `IsActive = true`)
- Filtrar por disponibilidad (menos de X tickets asignados)
- Ordenar por carga de trabajo (COUNT de tickets `IN_PROGRESS`)
- Asignar al primero de la lista

**Reintento Asíncrono**:
Si falla auto-asignación:
1. Worker escucha `ticket.assignment.failed`
2. Espera 5 minutos
3. Reintenta política de auto-asignación
4. Si sigue fallando, publica a DLQ para revisión manual

---

## Códigos de Error Recomendados

### 400 Bad Request
**Cuándo**: Datos inválidos en el request
```json
{
  "error": "VALIDATION_ERROR",
  "message": "Cannot resolve ticket without assignee",
  "details": {
    "field": "status",
    "ticketId": "TF-1024",
    "currentAssignedTo": null
  }
}
```

### 403 Forbidden
**Cuándo**: Usuario no autorizado para ejecutar la transición
```json
{
  "error": "FORBIDDEN",
  "message": "Only ADMIN or AGENT can change ticket status",
  "details": {
    "userId": "c789",
    "userRole": "CLIENT",
    "requiredRoles": ["ADMIN", "AGENT"]
  }
}
```

**Ejemplo**: Un CLIENT intenta cambiar status de `IN_PROGRESS` a `RESOLVED`.

### 404 Not Found
**Cuándo**: El ticket no existe
```json
{
  "error": "NOT_FOUND",
  "message": "Ticket not found",
  "details": {
    "ticketId": "TF-9999"
  }
}
```

### 409 Conflict
**Cuándo**: Transición de estado inválida
```json
{
  "error": "INVALID_TRANSITION",
  "message": "Cannot transition from RESOLVED to IN_PROGRESS",
  "details": {
    "ticketId": "TF-1024",
    "currentStatus": "RESOLVED",
    "requestedStatus": "IN_PROGRESS",
    "allowedTransitions": []
  }
}
```

**Ejemplo**: Intentar reabrir un ticket ya `RESOLVED`.

### 422 Unprocessable Entity
**Cuándo**: Estado válido pero regla de negocio no cumplida
```json
{
  "error": "BUSINESS_RULE_VIOLATION",
  "message": "Ticket must be assigned before resolving",
  "details": {
    "ticketId": "TF-1024",
    "currentStatus": "IN_PROGRESS",
    "requestedStatus": "RESOLVED",
    "violation": "MISSING_ASSIGNEE"
  }
}
```

---

## Matriz de Transiciones (Referencia Rápida)

|               | NEW | IN_PROGRESS | ON_HOLD | RESOLVED |
|---------------|-----|-------------|---------|----------|
| **NEW**       | -   | ✅          | ❌      | ❌       |
| **IN_PROGRESS** | ❌  | -           | ✅      | ✅*      |
| **ON_HOLD**   | ❌  | ✅          | -       | ❌       |
| **RESOLVED**  | ❌  | ❌          | ❌      | -        |

**Leyenda**:
- ✅ : Transición permitida
- ✅* : Transición permitida con restricción (AssignedTo != null)
- ❌ : Transición no permitida
- `-` : Mismo estado (no-op)

---

## Implementación en Backend

### 1. Domain/Entities/Ticket.cs
```csharp
// Pseudocódigo (NO código real)
enum TicketStatus { NEW, IN_PROGRESS, ON_HOLD, RESOLVED }

class Ticket {
  Status: TicketStatus
  AssignedTo: UserId?
  
  CanTransitionTo(newStatus): bool
  TransitionTo(newStatus, actor): Result
}
```

### 2. Domain/Services/StatusTransitionValidator.cs
```csharp
// Validador que conoce la matriz de transiciones
ValidateTransition(from, to): Result
GetAllowedTransitions(currentStatus): Status[]
```

### 3. Application/UseCases/ChangeTicketStatusUseCase.cs
```csharp
// Orquesta validación, cambio, audit log y outbox
Execute(ticketId, newStatus, actorId):
  1. Cargar ticket desde repositorio
  2. Validar transición (StatusTransitionValidator)
  3. Validar reglas (ej: AssignedTo para RESOLVED)
  4. Aplicar cambio en entidad
  5. Persistir en DB
  6. Insertar AuditLog (misma transacción)
  7. Insertar Outbox (misma transacción)
  8. Commit
```

### 4. Api/Endpoints/TicketsController.cs
```csharp
PATCH /api/tickets/{code}/status
Body: { "status": "RESOLVED" }

→ Invoca ChangeTicketStatusUseCase
→ Retorna 200 OK o códigos de error (400/403/404/409/422)
```

---

## Testing

### Casos de Prueba Esenciales

1. **Transición válida**: `NEW` → `IN_PROGRESS` (200 OK)
2. **Transición inválida**: `RESOLVED` → `IN_PROGRESS` (409 Conflict)
3. **Regla violada**: `IN_PROGRESS` → `RESOLVED` sin AssignedTo (422 Unprocessable Entity)
4. **Usuario no autorizado**: CLIENT intenta cambiar status (403 Forbidden)
5. **Ticket inexistente**: PATCH /tickets/TF-9999/status (404 Not Found)
6. **Idempotencia**: Cambiar de `IN_PROGRESS` a `IN_PROGRESS` (200 OK, no-op)
7. **Audit log creado**: Verificar registro en `AuditLogs` después de transición
8. **Evento publicado**: Verificar evento en `Outbox` después de transición

---

## Extensiones Futuras

### Posibles Mejoras (Fuera del MVP)

1. **Estado CANCELLED**:
   - Permitir cancelar tickets desde NEW/IN_PROGRESS
   - Requiere motivo de cancelación

2. **Sub-estados**:
   - `IN_PROGRESS.WAITING_CLIENT`
   - `IN_PROGRESS.INVESTIGATING`
   - Más granularidad sin complejidad

3. **Reapertura Controlada**:
   - Permitir `RESOLVED` → `REOPENED` (nuevo estado)
   - Solo si resolución fue en últimos 7 días
   - Requiere aprobación de ADMIN

4. **Transiciones Automáticas**:
   - Auto-pausar (`ON_HOLD`) si no hay actividad en 7 días
   - Auto-resolver si cliente confirma solución

5. **Workflow Personalizado**:
   - Diferentes máquinas de estado según tipo de ticket
   - Ej: "Bug" vs "Feature Request" vs "Support"

---

## Referencias

- Ver `docs/data-model.md` para estructura de tablas
- Ver `contracts/README.md` para formato de eventos
- Ver `docs/rabbitmq-topology.md` para routing de eventos
- [State Pattern (Gang of Four)](https://refactoring.guru/design-patterns/state)
- [Finite State Machine](https://en.wikipedia.org/wiki/Finite-state_machine)
