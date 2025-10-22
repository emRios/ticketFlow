# Modelo de Datos (MVP TicketFlow)

Definición conceptual del modelo de datos para el sistema de gestión de tickets.

---

## Tablas Principales

### 1. Users

Tabla de usuarios del sistema (agentes, administradores y clientes).

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `Id` | GUID/UUID | PK | Identificador único del usuario |
| `Name` | String(200) | NOT NULL | Nombre completo del usuario |
| `Email` | String(255) | NOT NULL, UNIQUE | Correo electrónico único |
| `Role` | Enum | NOT NULL | `ADMIN` \| `AGENT` \| `CLIENT` |
| `IsActive` | Boolean | NOT NULL, DEFAULT true | Estado activo/inactivo |
| `CreatedAt` | DateTime | NOT NULL | Fecha de creación (UTC) |

**Notas**:
- Email debe ser único en todo el sistema
- Role determina permisos y scopes
- IsActive permite deshabilitar usuarios sin eliminarlos

---

### 2. Tickets

Tabla principal de tickets del sistema.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `Id` | GUID/UUID | PK | Identificador único interno |
| `Code` | String(20) | NOT NULL, UNIQUE | Código visible (ej: TF-1024) |
| `Title` | String(200) | NOT NULL | Título del ticket |
| `Description` | Text | NULL | Descripción detallada |
| `Status` | Enum | NOT NULL | `NEW` \| `IN_PROGRESS` \| `ON_HOLD` \| `RESOLVED` |
| `Priority` | Enum | NOT NULL | `LOW` \| `MEDIUM` \| `HIGH` \| `URGENT` |
| `CreatorId` | GUID/UUID | NOT NULL, FK → Users.Id | Usuario que creó el ticket |
| `AssignedTo` | GUID/UUID | NULL, FK → Users.Id | Agente asignado (puede ser null) |
| `CreatedAt` | DateTime | NOT NULL | Fecha de creación (UTC) |
| `UpdatedAt` | DateTime | NOT NULL | Última actualización (UTC) |
| `ClosedAt` | DateTime | NULL | Fecha de cierre (solo si Status = RESOLVED) |

**Notas**:
- Code debe ser único y generado automáticamente (ej: TF-1024, TF-1025)
- AssignedTo puede ser null al momento de creación
- UpdatedAt se actualiza en cada modificación
- ClosedAt solo tiene valor cuando Status = RESOLVED

---

### 3. Tags

Tabla de etiquetas/tags disponibles en el sistema.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `Id` | GUID/UUID | PK | Identificador único del tag |
| `Name` | String(50) | NOT NULL, UNIQUE | Nombre del tag (ej: "urgente", "pagos") |
| `Color` | String(7) | NULL | Color en formato hex (ej: #ef4444) |

**Notas**:
- Name debe ser único (case-insensitive recomendado)
- Color es opcional, se puede usar un default en frontend

---

### 4. TicketTags

Tabla de relación muchos-a-muchos entre Tickets y Tags.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `TicketId` | GUID/UUID | PK, FK → Tickets.Id | Referencia al ticket |
| `TagId` | GUID/UUID | PK, FK → Tags.Id | Referencia al tag |

**Clave Primaria Compuesta**: `(TicketId, TagId)`

**Notas**:
- La PK compuesta evita duplicados automáticamente
- Un ticket puede tener múltiples tags
- Un tag puede estar en múltiples tickets

---

### 5. AuditLogs

Tabla de auditoría para rastrear cambios en entidades.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `Id` | GUID/UUID | PK | Identificador único del log |
| `ActorId` | GUID/UUID | NULL, FK → Users.Id | Usuario que ejecutó la acción (null si sistema) |
| `EntityType` | String(50) | NOT NULL | Tipo de entidad (ej: "Ticket", "User") |
| `EntityId` | String(50) | NOT NULL | ID de la entidad modificada |
| `Action` | String(50) | NOT NULL | Acción realizada (ej: "created", "status_changed") |
| `BeforeJson` | JSON/Text | NULL | Estado anterior (null en creación) |
| `AfterJson` | JSON/Text | NOT NULL | Estado posterior |
| `At` | DateTime | NOT NULL | Timestamp del cambio (UTC) |
| `CorrelationId` | String(100) | NOT NULL | ID para rastreo distribuido |

**Notas**:
- ActorId puede ser null para acciones del sistema
- BeforeJson es null en acciones de creación
- CorrelationId permite rastrear operaciones end-to-end
- JSON almacena snapshot completo del estado

---

### 6. Outbox

Tabla para implementar el patrón Transactional Outbox (publicación de eventos).

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `Id` | GUID/UUID | PK | Identificador único del evento |
| `Type` | String(100) | NOT NULL | Tipo de evento (ej: "ticket.created") |
| `PayloadJson` | JSON/Text | NOT NULL | Payload del evento completo |
| `OccurredAt` | DateTime | NOT NULL | Timestamp de cuando ocurrió el evento (UTC) |
| `CorrelationId` | String(100) | NOT NULL | ID para rastreo distribuido |
| `DispatchedAt` | DateTime | NULL | Timestamp de cuando se despachó (null = pendiente) |
| `Attempts` | Integer | NOT NULL, DEFAULT 0 | Número de intentos de despacho |

**Notas**:
- DispatchedAt null indica evento pendiente de publicar
- Attempts permite implementar retry logic
- PayloadJson contiene el event envelope completo
- Se inserta en misma transacción que el cambio en Tickets

---

### 7. ProcessedEvents

Tabla para garantizar idempotencia en el procesamiento de eventos.

| Campo | Tipo | Restricciones | Descripción |
|-------|------|---------------|-------------|
| `EventId` | String(100) | PK | ID único del evento procesado |
| `ProcessedAt` | DateTime | NOT NULL | Timestamp de procesamiento (UTC) |

**Notas**:
- EventId corresponde al campo `eventId` del event envelope
- Antes de procesar un evento, se verifica si ya existe en esta tabla
- Si existe, se ignora el evento (idempotencia)
- Se inserta después de procesar exitosamente

---

## Índices Sugeridos

Para optimizar consultas comunes:

### Tickets
```
INDEX idx_tickets_status ON Tickets(Status)
INDEX idx_tickets_assigned_to ON Tickets(AssignedTo)
INDEX idx_tickets_created_at ON Tickets(CreatedAt DESC)
INDEX idx_tickets_code ON Tickets(Code)  -- Ya cubierto por UNIQUE
```

### AuditLogs
```
INDEX idx_audit_entity ON AuditLogs(EntityType, EntityId, At DESC)
INDEX idx_audit_correlation ON AuditLogs(CorrelationId)
INDEX idx_audit_actor ON AuditLogs(ActorId, At DESC)
```

### Outbox
```
INDEX idx_outbox_pending ON Outbox(DispatchedAt, OccurredAt)
  WHERE DispatchedAt IS NULL
INDEX idx_outbox_correlation ON Outbox(CorrelationId)
```

### Users
```
INDEX idx_users_email ON Users(Email)  -- Ya cubierto por UNIQUE
INDEX idx_users_role ON Users(Role, IsActive)
```

---

## Relaciones (Foreign Keys)

```
Tickets.CreatorId → Users.Id
  ON DELETE: RESTRICT (no eliminar usuario con tickets creados)

Tickets.AssignedTo → Users.Id
  ON DELETE: SET NULL (liberar tickets al eliminar agente)

TicketTags.TicketId → Tickets.Id
  ON DELETE: CASCADE (eliminar tags al eliminar ticket)

TicketTags.TagId → Tags.Id
  ON DELETE: CASCADE (eliminar relación al eliminar tag)

AuditLogs.ActorId → Users.Id
  ON DELETE: SET NULL (mantener logs aunque se elimine usuario)
```

---

## Notas de Implementación

### 1. TicketTags - Sin Duplicados
La clave primaria compuesta `(TicketId, TagId)` garantiza que no existan duplicados automáticamente. No se requiere validación adicional en código.

### 2. AssignedTo Null en Creación
Los tickets pueden crearse sin asignación (`AssignedTo = NULL`). Las reglas de auto-asignación se implementan en la capa de Application/Domain, no en base de datos.

### 3. Outbox Pattern
- **Inserción**: En misma transacción que el cambio en Tickets
- **Despacho**: Worker periódico lee eventos pendientes (`DispatchedAt IS NULL`)
- **Reintentos**: Incrementar `Attempts` en cada fallo
- **Límite**: Mover a DLQ después de N intentos (configurar en Worker)

### 4. ProcessedEvents - Idempotencia
Flujo de procesamiento en Worker:
1. Recibir evento de RabbitMQ
2. Verificar si `EventId` existe en `ProcessedEvents`
3. Si existe → ACK inmediato (ya procesado)
4. Si no existe → Procesar + Insertar en `ProcessedEvents` + ACK

### 5. Timestamps UTC
Todos los campos DateTime deben almacenarse en **UTC** para evitar problemas de zonas horarias. La conversión a zona local se hace en frontend.

### 6. Soft Delete
La tabla `Users` implementa soft delete con el campo `IsActive`. No se recomienda eliminar físicamente usuarios que tengan tickets asociados.

### 7. Audit Trail Completo
`AuditLogs` almacena snapshots completos (JSON) del estado antes/después. Esto permite:
- Ver historial completo de cambios
- Revertir cambios si es necesario
- Análisis forense de acciones

---

## Consideraciones de Escalabilidad

### Particionamiento
Si el volumen crece:
- `AuditLogs` puede particionarse por fecha (`At`)
- `Outbox` puede limpiarse periódicamente (retener solo últimos 30 días)
- `ProcessedEvents` puede limpiarse después de X días

### Archivado
Tickets resueltos después de 1 año pueden moverse a tabla de archivo:
- `TicketsArchive` con misma estructura
- Mantener solo tickets activos en `Tickets` para queries rápidos

### Caché
Consultas frecuentes que pueden cachearse:
- Lista de Tags (cambia poco)
- Usuarios activos por rol
- Tickets por status (con TTL corto)

---

## Referencias

- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Audit Log Design](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [Idempotent Consumer Pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
