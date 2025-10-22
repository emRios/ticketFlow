# Application Ports - TicketFlow Backend

Este documento describe las **interfaces (ports)** que define la capa Application y que serán implementadas por la capa Infrastructure. Siguiendo **Hexagonal Architecture**, estos ports permiten que la lógica de negocio sea independiente de detalles técnicos (DB, MQ, etc.).

## Tabla de Contenidos

1. [ITicketRepository](#1-iticketrepository)
2. [IUserRepository](#2-iuserrepository)
3. [ITagRepository](#3-itagrepository)
4. [IAuditLogRepository](#4-iauditlogrepository)
5. [IOutboxStore](#5-ioutboxstore)
6. [IEventBus](#6-ieventbus)
7. [IClock](#7-iclock)
8. [IIdGenerator](#8-iidgenerator)
9. [IAssignmentStrategy](#9-iassignmentstrategy)

---

## 1. ITicketRepository

### Propósito
Persistencia y consulta de tickets. Abstrae el acceso a la tabla `Tickets` y sus relaciones (TicketTags).

### Operaciones

#### `GetByIdAsync(ticketId)`
- **Descripción**: Obtiene un ticket por su ID, incluyendo tags asociados.
- **Retorna**: Entidad `Ticket` o `null` si no existe.

#### `GetAllAsync(filters, pagination)`
- **Descripción**: Lista tickets con filtros opcionales (status, assignedTo, priority, tags) y paginación.
- **Parámetros**: 
  - `filters`: objeto con campos opcionales (status, assignedTo, priority, tagIds)
  - `pagination`: page, pageSize
- **Retorna**: Lista de `Ticket` + total count para paginación.

#### `GetByAssignedToAsync(userId)`
- **Descripción**: Obtiene todos los tickets asignados a un usuario específico.
- **Retorna**: Lista de `Ticket`.

#### `SaveAsync(ticket)`
- **Descripción**: Persiste un nuevo ticket o actualiza uno existente.
- **Transaccional**: Debe ejecutarse dentro de una transacción para garantizar atomicidad con Outbox.

#### `ExistsAsync(ticketId)`
- **Descripción**: Verifica si un ticket existe sin cargarlo completamente.
- **Retorna**: `bool`.

#### `AddTagsAsync(ticketId, tagIds)`
- **Descripción**: Asocia uno o más tags a un ticket (inserta en TicketTags).
- **Transaccional**: Debe ejecutarse dentro de transacción.

#### `RemoveTagAsync(ticketId, tagId)`
- **Descripción**: Desasocia un tag de un ticket (elimina de TicketTags).
- **Transaccional**: Debe ejecutarse dentro de transacción.

#### `GetTicketTagsAsync(ticketId)`
- **Descripción**: Obtiene los tags asociados a un ticket.
- **Retorna**: Lista de `Tag`.

### Invariantes
- Un ticket no puede existir sin `createdBy`.
- `assignedTo` debe ser un userId válido con role `AGENT` (validado por Use Case, no por repo).
- `status` debe ser uno de los valores del enum (`NEW`, `IN_PROGRESS`, `ON_HOLD`, `RESOLVED`).

### Errores Esperables
- **DbConnectionException**: Error de conexión a PostgreSQL.
- **DbConstraintViolationException**: Violación de constraints (FK, unique, etc.).
- **DbTimeoutException**: Timeout en query lento.

---

## 2. IUserRepository

### Propósito
Consulta de usuarios para validaciones y asignaciones. **No maneja creación/actualización de usuarios** (fuera del scope MVP).

### Operaciones

#### `GetByIdAsync(userId)`
- **Descripción**: Obtiene un usuario por su ID.
- **Retorna**: Entidad `User` o `null` si no existe.

#### `GetByRoleAsync(role)`
- **Descripción**: Lista todos los usuarios con un rol específico (ej: `AGENT`).
- **Parámetros**: `role` (enum: `ADMIN`, `AGENT`, `CLIENT`)
- **Retorna**: Lista de `User`.

#### `GetActiveAgentsAsync()`
- **Descripción**: Lista todos los agentes activos (role=AGENT, isActive=true).
- **Retorna**: Lista de `User`.
- **Uso**: Para algoritmo de auto-asignación.

#### `GetAgentWorkloadAsync(userId)`
- **Descripción**: Calcula la carga de trabajo de un agente (count de tickets en NEW + IN_PROGRESS).
- **Retorna**: Objeto con `openCount` e `inProgressCount`.
- **Uso**: Para algoritmo de auto-asignación (ver assignment-strategy.md).

#### `ExistsAsync(userId)`
- **Descripción**: Verifica si un usuario existe.
- **Retorna**: `bool`.

#### `IsAgentAsync(userId)`
- **Descripción**: Verifica si un usuario es AGENT.
- **Retorna**: `bool`.

### Invariantes
- Un usuario debe tener un `role` válido.
- `email` debe ser único (garantizado por DB).

### Errores Esperables
- **DbConnectionException**: Error de conexión a PostgreSQL.
- **DbTimeoutException**: Timeout en query lento.

---

## 3. ITagRepository

### Propósito
Gestión de tags (etiquetas) para categorizar tickets.

### Operaciones

#### `GetByIdAsync(tagId)`
- **Descripción**: Obtiene un tag por su ID.
- **Retorna**: Entidad `Tag` o `null` si no existe.

#### `GetByIdsAsync(tagIds)`
- **Descripción**: Obtiene múltiples tags por sus IDs.
- **Retorna**: Lista de `Tag`.
- **Uso**: Para validar que todos los tagIds existen antes de asociarlos a un ticket.

#### `GetAllAsync()`
- **Descripción**: Lista todos los tags disponibles.
- **Retorna**: Lista de `Tag`.

#### `ExistsAsync(tagId)`
- **Descripción**: Verifica si un tag existe.
- **Retorna**: `bool`.

#### `ExistAllAsync(tagIds)`
- **Descripción**: Verifica si todos los tags en una lista existen.
- **Retorna**: `bool`.

### Invariantes
- `name` debe ser único (garantizado por DB).
- `color` debe ser un código hex válido (validado en capa Application).

### Errores Esperables
- **DbConnectionException**: Error de conexión a PostgreSQL.

---

## 4. IAuditLogRepository

### Propósito
Registro de auditoría para trazabilidad de cambios en tickets.

### Operaciones

#### `SaveAsync(auditLog)`
- **Descripción**: Persiste un registro de auditoría.
- **Transaccional**: Debe ejecutarse dentro de la misma transacción que el cambio auditado.

#### `GetByTicketIdAsync(ticketId, pagination)`
- **Descripción**: Obtiene el historial de cambios de un ticket con paginación.
- **Retorna**: Lista de `AuditLog` + total count.

#### `GetByUserIdAsync(userId, pagination)`
- **Descripción**: Obtiene todas las acciones realizadas por un usuario.
- **Retorna**: Lista de `AuditLog` + total count.

### Invariantes
- Cada `AuditLog` debe tener `ticketId`, `userId`, `action` y `timestamp`.
- `changes` es un JSON string con el detalle de qué cambió.

### Errores Esperables
- **DbConnectionException**: Error de conexión a PostgreSQL.
- **DbConstraintViolationException**: Violación de FK (ticketId o userId inválido).

---

## 5. IOutboxStore

### Propósito
Persistencia de eventos en tabla `Outbox` para garantizar **entrega exactly-once** siguiendo el patrón Transactional Outbox (ver [docs/adr/adr-0002-rabbitmq-y-outbox.md](../../../docs/adr/adr-0002-rabbitmq-y-outbox.md)).

### Operaciones

#### `SaveAsync(outboxMessage)`
- **Descripción**: Guarda un evento en la tabla Outbox.
- **Transaccional**: Debe ejecutarse en la **misma transacción** que el cambio en el agregado (ticket).
- **Parámetros**: 
  - `eventType` (ej: `ticket.created`)
  - `aggregateId` (ticketId)
  - `payload` (JSON serializado)

#### `GetPendingAsync(batchSize)`
- **Descripción**: Obtiene eventos pendientes de publicar (processedAt = null), ordenados por createdAt.
- **Retorna**: Lista de `OutboxMessage` (máximo `batchSize`).
- **Uso**: Worker los consume y publica a RabbitMQ.

#### `MarkAsProcessedAsync(messageId, processedAt)`
- **Descripción**: Marca un evento como procesado después de publicarlo exitosamente a RabbitMQ.
- **Parámetros**: 
  - `messageId`: ID del mensaje en Outbox
  - `processedAt`: timestamp de procesamiento

#### `MarkAsFailedAsync(messageId, errorMessage)`
- **Descripción**: Marca un evento como fallido si hubo error al publicarlo.
- **Parámetros**: 
  - `messageId`: ID del mensaje
  - `errorMessage`: detalle del error
- **Uso**: Para monitoreo y reintentos.

### Invariantes
- Eventos deben insertarse en la **misma transacción** que el cambio en el agregado.
- Un evento procesado (`processedAt != null`) no debe ser reprocesado.

### Errores Esperables
- **DbConnectionException**: Error de conexión a PostgreSQL.
- **DbTransactionException**: Transacción abortada (rollback).

---

## 6. IEventBus

### Propósito
Abstracción para publicar eventos a RabbitMQ. Implementada en Infrastructure, usada por el Worker para consumir tabla Outbox.

### Operaciones

#### `PublishAsync(eventType, payload)`
- **Descripción**: Publica un evento a RabbitMQ en el exchange `ticketflow.events`.
- **Parámetros**: 
  - `eventType`: routing key (ej: `ticket.created`, `ticket.status_changed`)
  - `payload`: objeto serializable a JSON
- **Garantías**: At-least-once delivery (RabbitMQ confirmations).

#### `PublishBatchAsync(events)`
- **Descripción**: Publica múltiples eventos en batch para optimizar throughput.
- **Retorna**: Lista de eventos exitosos y fallidos.

### Invariantes
- El evento debe ser publicado con **routing key correcto** para que los consumidores lo reciban.
- Debe manejar reconexiones automáticas si RabbitMQ está temporalmente caído.

### Errores Esperables
- **EventBusConnectionException**: No se puede conectar a RabbitMQ.
- **EventPublishException**: Error al publicar evento (timeout, channel cerrado).

---

## 7. IClock

### Propósito
Abstracción del tiempo para facilitar **testing** (inyección de tiempo controlado en tests).

### Operaciones

#### `UtcNow()`
- **Descripción**: Retorna el timestamp actual en UTC.
- **Retorna**: `DateTime` (UTC).
- **Uso**: Para llenar `createdAt`, `updatedAt`, `timestamp` en auditoría.

### Invariantes
- Siempre debe retornar UTC (no local time).

### Errores Esperables
Ninguno (operación determinista).

---

## 8. IIdGenerator

### Propósito
Generación de IDs únicos para entidades (tickets, audit logs, outbox messages). Abstrae la estrategia de generación (UUIDs, Snowflake IDs, etc.).

### Operaciones

#### `GenerateTicketId()`
- **Descripción**: Genera un ID único para un ticket.
- **Formato esperado**: `tk-YYYYMM-NNNN` (ej: `tk-202501-0042`)
- **Retorna**: `string`.

#### `GenerateId()`
- **Descripción**: Genera un ID genérico (UUID) para otras entidades (audit logs, outbox).
- **Retorna**: `string` (UUID v4).

### Invariantes
- IDs generados deben ser **únicos** globalmente.
- Para tickets, el formato debe ser legible para humanos y ordenable cronológicamente.

### Errores Esperables
- **IdCollisionException**: Colisión de ID (muy improbable con UUIDs, posible con secuencias).

---

## 9. IAssignmentStrategy

### Propósito
Algoritmo de **auto-asignación** de tickets a agentes disponibles, basado en score de carga de trabajo (ver [docs/assignment-strategy.md](../../../docs/assignment-strategy.md)).

### Operaciones

#### `AssignToAgentAsync(ticket)`
- **Descripción**: Calcula qué agente debe recibir el ticket basándose en score.
- **Parámetros**: `ticket` (entidad recién creada, sin asignar)
- **Retorna**: `userId` del agente seleccionado o `null` si no hay agentes disponibles.
- **Algoritmo**: 
  1. Obtener lista de agentes activos (via `IUserRepository.GetActiveAgentsAsync()`)
  2. Calcular score para cada agente: `openCount + inProgressCount * 1.5`
  3. Seleccionar agente con **menor score**
  4. En caso de empate, usar desempate por orden alfabético o round-robin

#### `CalculateAgentScoreAsync(userId)`
- **Descripción**: Calcula el score de un agente específico (para métricas).
- **Retorna**: `double` (score).

### Invariantes
- Si no hay agentes activos, debe retornar `null` (no lanzar excepción).
- El algoritmo debe ser **determinista** para el mismo estado de datos.

### Errores Esperables
- **DbConnectionException**: Si falla consulta a DB para obtener workload.

---

## Implementación

### Ubicación
Estas interfaces se definen en:
```
backend/src/Application/Ports/
  ├── ITicketRepository.cs
  ├── IUserRepository.cs
  ├── ITagRepository.cs
  ├── IAuditLogRepository.cs
  ├── IOutboxStore.cs
  ├── IEventBus.cs
  ├── IClock.cs
  ├── IIdGenerator.cs
  └── IAssignmentStrategy.cs
```

### Implementaciones (en Infrastructure)
```
backend/src/Infrastructure/Persistence/
  ├── TicketRepository.cs          → implements ITicketRepository
  ├── UserRepository.cs            → implements IUserRepository
  ├── TagRepository.cs             → implements ITagRepository
  ├── AuditLogRepository.cs        → implements IAuditLogRepository
  └── OutboxStore.cs               → implements IOutboxStore

backend/src/Infrastructure/Messaging/
  └── RabbitMqEventBus.cs          → implements IEventBus

backend/src/Infrastructure/Services/
  ├── SystemClock.cs               → implements IClock (retorna DateTime.UtcNow)
  ├── TicketIdGenerator.cs         → implements IIdGenerator
  └── ScoreBasedAssignment.cs      → implements IAssignmentStrategy
```

### Inyección de Dependencias
Los ports se registran en el contenedor DI de ASP.NET Core:
```csharp
// Program.cs o Startup.cs
services.AddScoped<ITicketRepository, TicketRepository>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ITagRepository, TagRepository>();
services.AddScoped<IAuditLogRepository, AuditLogRepository>();
services.AddScoped<IOutboxStore, OutboxStore>();
services.AddSingleton<IEventBus, RabbitMqEventBus>();
services.AddSingleton<IClock, SystemClock>();
services.AddSingleton<IIdGenerator, TicketIdGenerator>();
services.AddScoped<IAssignmentStrategy, ScoreBasedAssignment>();
```

---

## Testing

### Unit Tests (Application Layer)
Los Use Cases se testean con **mocks** de estos ports:
```csharp
var mockRepo = new Mock<ITicketRepository>();
mockRepo.Setup(r => r.GetByIdAsync("tk-001")).ReturnsAsync(ticket);

var useCase = new CreateTicketUseCase(mockRepo.Object, ...);
```

### Integration Tests (Infrastructure Layer)
Las implementaciones reales se testean contra:
- **PostgreSQL**: Testcontainers o DB en memoria (SQLite para repos simples).
- **RabbitMQ**: Testcontainers con broker real.

---

## Documentos Relacionados

- 📄 **[/docs/data-model.md](../../../docs/data-model.md)** - Esquema de base de datos
- 📄 **[/docs/assignment-strategy.md](../../../docs/assignment-strategy.md)** - Algoritmo de auto-asignación
- 📄 **[/docs/adr/adr-0002-rabbitmq-y-outbox.md](../../../docs/adr/adr-0002-rabbitmq-y-outbox.md)** - Patrón Transactional Outbox
- 📄 **[Application UseCases README](../UseCases/README.md)** - Casos de uso que consumen estos ports
