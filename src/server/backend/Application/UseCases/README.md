# Application Use Cases - TicketFlow Backend

Este documento describe los **casos de uso (Use Cases)** de la capa Application. Cada Use Case orquesta la lógica de negocio usando entidades del Domain y ports (interfaces) que abstrae Infrastructure.

## Tabla de Contenidos

1. [CreateTicket](#1-createticket)
2. [ChangeTicketStatus](#2-changeticketstatus)
3. [AssignTicket](#3-assignticket)
4. [UpdateTicket](#4-updateticket)
5. [ListTickets](#5-listtickets)
6. [AddTagsToTicket](#6-addtagstoticket)
7. [RemoveTagFromTicket](#7-removetagfromticket)
8. [GetTicketAuditLogs](#8-getticketauditlogs)
9. [ListUsers](#9-listusers)

---

## 1. CreateTicket

### Propósito
Crear un nuevo ticket. Si el cliente no especifica `assignedTo`, el sistema **auto-asigna** a un agente disponible usando `IAssignmentStrategy`.

### Entrada (Input DTO)
```
{
  title: string (requerido, max 200 chars)
  description: string (requerido, max 2000 chars)
  priority: enum (LOW | MEDIUM | HIGH | URGENT)
  assignedTo: string (opcional, userId)
  tagIds: string[] (opcional)
  createdBy: string (requerido, userId del autor)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  status: "NEW"
  assignedTo: string (puede ser auto-asignado)
  tags: Tag[]
  createdAt: DateTime
}
```

### Reglas de Negocio

1. **Validación de entrada:**
   - `title` y `description` son requeridos.
   - `priority` debe ser un valor válido del enum.
   - Si `assignedTo` se especifica, debe existir y ser role `AGENT`.

2. **Auto-asignación:**
   - Si `assignedTo` es `null` o vacío, ejecutar `IAssignmentStrategy.AssignToAgentAsync()`.
   - Si no hay agentes disponibles, crear ticket sin asignar (`assignedTo = null`).

3. **Tags:**
   - Validar que todos los `tagIds` existen usando `ITagRepository.ExistAllAsync()`.
   - Asociar tags al ticket en tabla `TicketTags`.

4. **Estado inicial:**
   - El ticket siempre se crea con `status = NEW`.

5. **Timestamps:**
   - `createdAt` y `updatedAt` se obtienen de `IClock.UtcNow()`.

6. **ID generación:**
   - Generar ticketId usando `IIdGenerator.GenerateTicketId()` (formato: `tk-YYYYMM-NNNN`).

### Transaccionalidad

**Todo debe ejecutarse en una transacción atómica:**
1. Insertar ticket en `Tickets`.
2. Insertar tags en `TicketTags` (si hay).
3. Insertar auditoría en `AuditLogs` (action=CREATE).
4. Insertar evento en `Outbox` (event=`ticket.created`).

Si cualquier paso falla → **rollback completo**.

### Eventos a Outbox

**ticket.created**
```json
{
  "eventType": "ticket.created",
  "aggregateId": "tk-202501-0042",
  "payload": {
    "ticketId": "tk-202501-0042",
    "status": "NEW",
    "priority": "HIGH",
    "assignedTo": "u002",
    "createdBy": "u004",
    "tagIds": ["t001", "t004"]
  }
}
```

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | Campos inválidos (title vacío, priority inválido, etc.) |
| **403** | ForbiddenException | CLIENT intenta especificar `assignedTo` manualmente |
| **404** | UserNotFoundException | `assignedTo` no existe o no es AGENT |
| **404** | TagNotFoundException | Uno o más `tagIds` no existen |
| **500** | DatabaseException | Error de transacción en DB |
| **500** | AssignmentException | Error en algoritmo de asignación |

### Dependencias (Ports)
- `ITicketRepository`: Guardar ticket.
- `IUserRepository`: Validar `assignedTo`, obtener agentes activos.
- `ITagRepository`: Validar `tagIds`.
- `IAuditLogRepository`: Registrar auditoría.
- `IOutboxStore`: Guardar evento.
- `IAssignmentStrategy`: Auto-asignar agente.
- `IClock`: Obtener timestamp.
- `IIdGenerator`: Generar ticketId.

---

## 2. ChangeTicketStatus

### Propósito
Cambiar el estado de un ticket siguiendo las reglas de la **máquina de estados** (ver [docs/state-machine.md](../../../docs/state-machine.md)).

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  newStatus: enum (NEW | IN_PROGRESS | ON_HOLD | RESOLVED)
  comment: string (opcional, max 500 chars)
  userId: string (requerido, quien hace el cambio)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  oldStatus: string
  newStatus: string
  updatedAt: DateTime
}
```

### Reglas de Negocio

1. **Validación de existencia:**
   - El ticket debe existir (usar `ITicketRepository.GetByIdAsync()`).

2. **Validación de transición:**
   - Usar `StatusTransitionValidator` (entidad Domain) para verificar que la transición es válida.
   - **Matriz de transiciones permitidas** (ver state-machine.md):
     - NEW → IN_PROGRESS, ON_HOLD, RESOLVED
     - IN_PROGRESS → ON_HOLD, RESOLVED
     - ON_HOLD → IN_PROGRESS, RESOLVED
     - RESOLVED → (ninguna transición permitida)

3. **Autorización:**
   - Solo ADMIN puede cambiar cualquier ticket.
   - AGENT solo puede cambiar tickets asignados a él/ella.

4. **Comentario opcional:**
   - Si se proporciona `comment`, agregarlo al campo `description` o crear un registro de comentario (fuera del scope MVP, pero preparar para futuro).

### Transaccionalidad

**Transacción atómica:**
1. Actualizar `status` y `updatedAt` en `Tickets`.
2. Insertar auditoría en `AuditLogs` (action=UPDATE_STATUS).
3. Insertar evento en `Outbox` (event=`ticket.status_changed`).

### Eventos a Outbox

**ticket.status_changed**
```json
{
  "eventType": "ticket.status_changed",
  "aggregateId": "tk-202501-0042",
  "payload": {
    "ticketId": "tk-202501-0042",
    "oldStatus": "NEW",
    "newStatus": "IN_PROGRESS",
    "userId": "u002",
    "comment": "Comenzando a investigar el problema"
  }
}
```

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | `newStatus` inválido |
| **403** | ForbiddenException | AGENT intenta cambiar ticket de otro |
| **404** | TicketNotFoundException | Ticket no existe |
| **409** | InvalidTransitionException | Transición no permitida (ej: RESOLVED → NEW) |
| **500** | DatabaseException | Error de transacción |

### Dependencias (Ports)
- `ITicketRepository`: Obtener y actualizar ticket.
- `IAuditLogRepository`: Registrar auditoría.
- `IOutboxStore`: Guardar evento.
- `IClock`: Obtener timestamp.

---

## 3. AssignTicket

### Propósito
Re-asignar un ticket a otro agente (solo ADMIN puede hacerlo).

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  newAssignedTo: string (requerido, userId)
  userId: string (requerido, quien hace la asignación)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  oldAssignedTo: string | null
  newAssignedTo: string
  updatedAt: DateTime
}
```

### Reglas de Negocio

1. **Validación de usuario:**
   - `newAssignedTo` debe existir y tener role `AGENT` (usar `IUserRepository.IsAgentAsync()`).

2. **Autorización:**
   - Solo ADMIN puede re-asignar tickets.

3. **Permitir desasignación:**
   - Si `newAssignedTo = null`, se permite desasignar el ticket (quedará sin agente).

### Transaccionalidad

**Transacción atómica:**
1. Actualizar `assignedTo` y `updatedAt` en `Tickets`.
2. Insertar auditoría en `AuditLogs` (action=REASSIGN).
3. Insertar evento en `Outbox` (event=`ticket.reassigned`).

### Eventos a Outbox

**ticket.reassigned**
```json
{
  "eventType": "ticket.reassigned",
  "aggregateId": "tk-202501-0042",
  "payload": {
    "ticketId": "tk-202501-0042",
    "oldAssignedTo": "u002",
    "newAssignedTo": "u003",
    "userId": "u001"
  }
}
```

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **403** | ForbiddenException | AGENT o CLIENT intenta re-asignar |
| **404** | TicketNotFoundException | Ticket no existe |
| **404** | UserNotFoundException | `newAssignedTo` no existe o no es AGENT |
| **500** | DatabaseException | Error de transacción |

### Dependencias (Ports)
- `ITicketRepository`: Obtener y actualizar ticket.
- `IUserRepository`: Validar `newAssignedTo`.
- `IAuditLogRepository`: Registrar auditoría.
- `IOutboxStore`: Guardar evento.
- `IClock`: Obtener timestamp.

---

## 4. UpdateTicket

### Propósito
Actualizar campos de un ticket (title, description, priority). **No cambia status ni assignedTo** (usar casos de uso específicos para eso).

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  title: string (opcional)
  description: string (opcional)
  priority: enum (opcional)
  userId: string (requerido, quien hace el update)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  updatedFields: string[] (lista de campos actualizados)
  updatedAt: DateTime
}
```

### Reglas de Negocio

1. **Validación:**
   - Si `title` se especifica, max 200 chars.
   - Si `description` se especifica, max 2000 chars.
   - Si `priority` se especifica, debe ser un valor válido del enum.

2. **Autorización:**
   - ADMIN puede editar cualquier ticket.
   - AGENT solo puede editar tickets asignados a él/ella.

3. **Actualización parcial:**
   - Solo se actualizan los campos especificados en el input.

### Transaccionalidad

**Transacción atómica:**
1. Actualizar campos en `Tickets`.
2. Insertar auditoría en `AuditLogs` (action=UPDATE, changes=JSON con old/new values).
3. **No** se publica evento si solo cambian campos descriptivos (title, description, priority).

### Eventos a Outbox

Ninguno (cambios menores no generan eventos).

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | Campos inválidos |
| **403** | ForbiddenException | AGENT intenta editar ticket de otro |
| **404** | TicketNotFoundException | Ticket no existe |
| **500** | DatabaseException | Error de transacción |

### Dependencias (Ports)
- `ITicketRepository`: Obtener y actualizar ticket.
- `IAuditLogRepository`: Registrar auditoría.
- `IClock`: Obtener timestamp.

---

## 5. ListTickets

### Propósito
Listar tickets con filtros opcionales y paginación. Implementa **filtros por rol**:
- ADMIN: ve todos los tickets.
- AGENT: solo tickets asignados a él/ella.
- CLIENT: solo tickets creados por él/ella.

### Entrada (Input DTO)
```
{
  status: enum (opcional)
  assignedTo: string (opcional)
  priority: enum (opcional)
  tagIds: string[] (opcional)
  page: int (default: 1)
  pageSize: int (default: 20, max: 100)
  userId: string (requerido, quien hace la query)
  userRole: enum (ADMIN | AGENT | CLIENT)
}
```

### Salida (Output DTO)
```
{
  data: Ticket[] (lista de tickets)
  pagination: {
    page: int
    pageSize: int
    totalItems: int
    totalPages: int
  }
}
```

### Reglas de Negocio

1. **Filtros por rol:**
   - Si `userRole = ADMIN`, no aplicar filtros adicionales.
   - Si `userRole = AGENT`, agregar filtro `assignedTo = userId`.
   - Si `userRole = CLIENT`, agregar filtro `createdBy = userId`.

2. **Validación de paginación:**
   - `pageSize` max 100.
   - `page` mínimo 1.

3. **Filtros opcionales:**
   - Si `status` se especifica, filtrar por status.
   - Si `assignedTo` se especifica (solo ADMIN), filtrar por assignedTo.
   - Si `priority` se especifica, filtrar por priority.
   - Si `tagIds` se especifica, filtrar tickets que tengan **todos** esos tags.

### Transaccionalidad

No requiere transacción (operación de solo lectura).

### Eventos a Outbox

Ninguno (operación de solo lectura).

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | `pageSize > 100`, `page < 1`, `status` inválido |
| **500** | DatabaseException | Error en query |

### Dependencias (Ports)
- `ITicketRepository`: Ejecutar query con filtros.

---

## 6. AddTagsToTicket

### Propósito
Asociar uno o más tags a un ticket existente.

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  tagIds: string[] (requerido, min 1 tag)
  userId: string (requerido, quien hace el cambio)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  tags: Tag[] (lista completa de tags asociados)
}
```

### Reglas de Negocio

1. **Validación:**
   - Todos los `tagIds` deben existir (usar `ITagRepository.ExistAllAsync()`).

2. **Idempotencia:**
   - Si un tag ya está asociado al ticket, **no lanzar error**, simplemente ignorarlo.

3. **Autorización:**
   - ADMIN puede agregar tags a cualquier ticket.
   - AGENT solo puede agregar tags a tickets asignados a él/ella.

### Transaccionalidad

**Transacción atómica:**
1. Insertar en `TicketTags` (para cada tagId).
2. Insertar auditoría en `AuditLogs` (action=ADD_TAGS).
3. Insertar evento en `Outbox` (event=`ticket.tags_added`).

### Eventos a Outbox

**ticket.tags_added**
```json
{
  "eventType": "ticket.tags_added",
  "aggregateId": "tk-202501-0042",
  "payload": {
    "ticketId": "tk-202501-0042",
    "tagIds": ["t002", "t003"],
    "userId": "u002"
  }
}
```

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | `tagIds` vacío o no es array |
| **403** | ForbiddenException | AGENT intenta agregar tags a ticket de otro |
| **404** | TicketNotFoundException | Ticket no existe |
| **404** | TagNotFoundException | Uno o más tagIds no existen |
| **500** | DatabaseException | Error de transacción |

### Dependencias (Ports)
- `ITicketRepository`: Agregar tags.
- `ITagRepository`: Validar tags.
- `IAuditLogRepository`: Registrar auditoría.
- `IOutboxStore`: Guardar evento.
- `IClock`: Obtener timestamp.

---

## 7. RemoveTagFromTicket

### Propósito
Desasociar un tag de un ticket.

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  tagId: string (requerido)
  userId: string (requerido, quien hace el cambio)
}
```

### Salida (Output DTO)
```
{
  ticketId: string
  removedTagId: string
}
```

### Reglas de Negocio

1. **Validación:**
   - El tag debe estar asociado al ticket (si no lo está, lanzar 404).

2. **Autorización:**
   - ADMIN puede remover tags de cualquier ticket.
   - AGENT solo puede remover tags de tickets asignados a él/ella.

### Transaccionalidad

**Transacción atómica:**
1. Eliminar de `TicketTags`.
2. Insertar auditoría en `AuditLogs` (action=REMOVE_TAG).
3. Insertar evento en `Outbox` (event=`ticket.tag_removed`).

### Eventos a Outbox

**ticket.tag_removed**
```json
{
  "eventType": "ticket.tag_removed",
  "aggregateId": "tk-202501-0042",
  "payload": {
    "ticketId": "tk-202501-0042",
    "tagId": "t002",
    "userId": "u002"
  }
}
```

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **403** | ForbiddenException | AGENT intenta remover tag de ticket de otro |
| **404** | TicketNotFoundException | Ticket no existe |
| **404** | TagNotFoundException | Tag no existe |
| **404** | TagNotAssociatedException | Tag no está asociado al ticket |
| **500** | DatabaseException | Error de transacción |

### Dependencias (Ports)
- `ITicketRepository`: Remover tag.
- `ITagRepository`: Validar tag.
- `IAuditLogRepository`: Registrar auditoría.
- `IOutboxStore`: Guardar evento.
- `IClock`: Obtener timestamp.

---

## 8. GetTicketAuditLogs

### Propósito
Obtener el historial de cambios (auditoría) de un ticket específico.

### Entrada (Input DTO)
```
{
  ticketId: string (requerido)
  page: int (default: 1)
  pageSize: int (default: 50, max: 100)
  userId: string (requerido, quien hace la query)
  userRole: enum (ADMIN | AGENT | CLIENT)
}
```

### Salida (Output DTO)
```
{
  data: AuditLog[] (lista de logs ordenados por timestamp desc)
  pagination: {
    page: int
    pageSize: int
    totalItems: int
    totalPages: int
  }
}
```

### Reglas de Negocio

1. **Autorización:**
   - ADMIN puede ver auditoría de cualquier ticket.
   - AGENT solo puede ver auditoría de tickets asignados a él/ella.
   - CLIENT solo puede ver auditoría de tickets creados por él/ella.

2. **Orden:**
   - Los logs se retornan ordenados por `timestamp DESC` (más recientes primero).

### Transaccionalidad

No requiere transacción (operación de solo lectura).

### Eventos a Outbox

Ninguno (operación de solo lectura).

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **403** | ForbiddenException | Usuario no tiene permiso para ver este ticket |
| **404** | TicketNotFoundException | Ticket no existe |
| **500** | DatabaseException | Error en query |

### Dependencias (Ports)
- `ITicketRepository`: Validar existencia del ticket y permisos.
- `IAuditLogRepository`: Obtener logs.

---

## 9. ListUsers

### Propósito
Listar usuarios filtrados por rol (principalmente para obtener lista de agentes disponibles).

### Entrada (Input DTO)
```
{
  role: enum (opcional, ADMIN | AGENT | CLIENT)
  active: bool (opcional, default: true)
  userId: string (requerido, quien hace la query)
  userRole: enum (ADMIN | AGENT | CLIENT)
}
```

### Salida (Output DTO)
```
{
  data: User[] (lista de usuarios, sin password)
}
```

### Reglas de Negocio

1. **Filtros por rol del usuario:**
   - ADMIN puede listar todos los roles.
   - AGENT puede listar solo AGENT y CLIENT.
   - CLIENT solo puede listar AGENT (para ver quién está asignado a sus tickets).

2. **Filtro por activos:**
   - Si `active = true`, solo retornar usuarios con `isActive = true`.

### Transaccionalidad

No requiere transacción (operación de solo lectura).

### Eventos a Outbox

Ninguno (operación de solo lectura).

### Errores Esperables

| Código | Descripción | Cuándo |
|--------|-------------|--------|
| **400** | ValidationException | `role` inválido |
| **403** | ForbiddenException | CLIENT intenta listar ADMIN |
| **500** | DatabaseException | Error en query |

### Dependencias (Ports)
- `IUserRepository`: Ejecutar query con filtros.

---

## Resumen de Eventos por Use Case

| Use Case | Evento(s) Publicado(s) |
|----------|------------------------|
| **CreateTicket** | `ticket.created` |
| **ChangeTicketStatus** | `ticket.status_changed` |
| **AssignTicket** | `ticket.reassigned` |
| **UpdateTicket** | Ninguno (cambios menores) |
| **ListTickets** | Ninguno (solo lectura) |
| **AddTagsToTicket** | `ticket.tags_added` |
| **RemoveTagFromTicket** | `ticket.tag_removed` |
| **GetTicketAuditLogs** | Ninguno (solo lectura) |
| **ListUsers** | Ninguno (solo lectura) |

---

## Estructura de Implementación

Los Use Cases se implementan en:
```
backend/src/Application/UseCases/
  ├── CreateTicketUseCase.cs
  ├── ChangeTicketStatusUseCase.cs
  ├── AssignTicketUseCase.cs
  ├── UpdateTicketUseCase.cs
  ├── ListTicketsUseCase.cs
  ├── AddTagsToTicketUseCase.cs
  ├── RemoveTagFromTicketUseCase.cs
  ├── GetTicketAuditLogsUseCase.cs
  └── ListUsersUseCase.cs
```

Cada Use Case sigue el patrón:
```csharp
public class CreateTicketUseCase
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITagRepository _tagRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IOutboxStore _outboxStore;
    private readonly IAssignmentStrategy _assignmentStrategy;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public async Task<CreateTicketOutput> ExecuteAsync(CreateTicketInput input)
    {
        // 1. Validación
        // 2. Lógica de negocio
        // 3. Persistencia (transacción)
        // 4. Retornar resultado
    }
}
```

---

## Testing

### Unit Tests
Testear cada Use Case con mocks de los ports:
```csharp
var mockTicketRepo = new Mock<ITicketRepository>();
var mockAssignmentStrategy = new Mock<IAssignmentStrategy>();
mockAssignmentStrategy.Setup(s => s.AssignToAgentAsync(It.IsAny<Ticket>()))
                      .ReturnsAsync("u002");

var useCase = new CreateTicketUseCase(mockTicketRepo.Object, ...);
var output = await useCase.ExecuteAsync(input);

Assert.Equal("NEW", output.Status);
Assert.Equal("u002", output.AssignedTo);
```

### Integration Tests
Testear flujos completos con DB real (Testcontainers):
```csharp
// Dado: DB con seed data (2 agentes activos)
// Cuando: Se crea ticket sin assignedTo
// Entonces: El ticket se asigna al agente con menor score
```

---

## Documentos Relacionados

- 📄 **[/backend/src/Api/Endpoints/README.md](../Api/Endpoints/README.md)** - Endpoints que invocan estos Use Cases
- 📄 **[/backend/src/Application/Ports/README.md](../Ports/README.md)** - Interfaces que consumen los Use Cases
- 📄 **[/docs/state-machine.md](../../../docs/state-machine.md)** - Reglas de transición de estados
- 📄 **[/docs/assignment-strategy.md](../../../docs/assignment-strategy.md)** - Algoritmo de auto-asignación
- 📄 **[/docs/adr/adr-0002-rabbitmq-y-outbox.md](../../../docs/adr/adr-0002-rabbitmq-y-outbox.md)** - Patrón Transactional Outbox
- 📄 **[/docs/data-model.md](../../../docs/data-model.md)** - Esquema de base de datos
