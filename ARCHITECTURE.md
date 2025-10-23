# 🏗️ Arquitectura y Flujo de Colas - TicketFlow

Diagrama simple de la arquitectura del sistema y el flujo de mensajería.

---

## 📐 Arquitectura General

```
┌─────────────────────────────────────────────────────────────────┐
│                         TICKETFLOW                              │
└─────────────────────────────────────────────────────────────────┘

    ┌──────────────┐         ┌──────────────┐         ┌──────────────┐
    │   Frontend   │◄────────┤   Backend    │◄────────┤   Worker     │
    │   (Vite)     │  HTTP   │   (.NET 8)   │  Queue  │   (.NET 8)   │
    │  Port: 5173  │────────►│  Port: 5076  │────────►│  Background  │
    └──────────────┘         └──────┬───────┘         └──────┬───────┘
                                    │                        │
                                    │                        │
                    ┌───────────────┴────────────┬───────────┴────────┐
                    │                            │                    │
              ┌─────▼─────┐              ┌──────▼──────┐     ┌───────▼──────┐
              │ PostgreSQL│              │  RabbitMQ   │     │  RabbitMQ    │
              │ Port: 5432│              │  Port: 5672 │     │  Port: 5672  │
              └───────────┘              └─────────────┘     └──────────────┘
```

---

## 🔄 Flujo de Datos

### 1. Creación de Ticket

```
[Usuario] ──┐
            │
            ▼
    ┌───────────────┐
    │   Frontend    │  1. POST /api/tickets
    └───────┬───────┘
            │
            ▼
    ┌───────────────┐
    │   Backend     │  2. Crear Ticket
    │     API       │  3. Guardar en DB
    └───────┬───────┘  4. Publicar eventos
            │
            ├──────────────┐
            │              │
            ▼              ▼
    ┌───────────┐   ┌────────────┐
    │ PostgreSQL│   │  RabbitMQ  │
    └───────────┘   └─────┬──────┘
                          │
                          ▼
                   ┌──────────────┐
                   │    Worker    │  5. Procesar eventos
                   │              │  6. Métricas/Notificaciones
                   └──────────────┘
```

### 2. Cambio de Estado

```
[Agente] ──┐
           │
           ▼
   ┌───────────────┐
   │   Frontend    │  1. PATCH /api/tickets/{id}/status
   └───────┬───────┘
           │
           ▼
   ┌───────────────┐
   │   Backend     │  2. Actualizar estado
   │     API       │  3. Generar evento
   └───────┬───────┘     (TicketStatusChangedEvent)
           │
           ├──────────────┐
           │              │
           ▼              ▼
   ┌───────────┐   ┌────────────┐
   │ Outbox    │   │  PostgreSQL│
   │ Pattern   │   └────────────┘
   └─────┬─────┘
         │
         ▼ (Background)
   ┌────────────┐
   │  RabbitMQ  │
   └─────┬──────┘
         │
         ├──────────────┬──────────────┐
         │              │              │
         ▼              ▼              ▼
   ┌──────────┐  ┌──────────┐  ┌──────────┐
   │ Métricas │  │Notif.    │  │ Audit    │
   │ Queue    │  │Queue     │  │ Log      │
   └──────────┘  └──────────┘  └──────────┘
```

---

## 🎯 Componentes Principales

### Frontend (Vite + TypeScript)
```
┌─────────────────────────────┐
│  Frontend Components        │
├─────────────────────────────┤
│ • Tablero Kanban            │
│ • Gestión de Usuarios       │
│ • Detalles de Ticket        │
│ • Login/Auth                │
└─────────────────────────────┘
         │
         │ HTTP/REST
         ▼
```

### Backend API (.NET 8)
```
┌─────────────────────────────┐
│  REST API Endpoints         │
├─────────────────────────────┤
│ • /api/tickets              │
│ • /api/users                │
│ • /api/auth                 │
└─────────────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  Application Layer          │
├─────────────────────────────┤
│ • Commands (CQRS)           │
│ • Queries                   │
│ • Handlers                  │
└─────────────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  Domain Layer               │
├─────────────────────────────┤
│ • Entities (Ticket, User)   │
│ • Value Objects             │
│ • Domain Events             │
└─────────────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  Infrastructure             │
├─────────────────────────────┤
│ • EF Core (PostgreSQL)      │
│ • RabbitMQ Publisher        │
│ • Outbox Pattern            │
└─────────────────────────────┘
```

### Worker (Background Processor)
```
┌─────────────────────────────┐
│  Outbox Processor           │
├─────────────────────────────┤
│ • Lee OutboxMessages        │
│ • Publica a RabbitMQ        │
│ • Marca como procesados     │
└─────────────────────────────┘
         │
         ▼
┌─────────────────────────────┐
│  Event Consumers            │
├─────────────────────────────┤
│ • MetricsConsumer           │
│ • NotificationsConsumer     │
└─────────────────────────────┘
```

---

## 📨 Colas de RabbitMQ

### Topología de Colas

```
                    ┌──────────────────┐
                    │   RabbitMQ       │
                    │   Exchange       │
                    └────────┬─────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │   tickets.  │  │   tickets.  │  │   tickets.  │
    │   metrics   │  │   notify    │  │   audit     │
    └─────────────┘  └─────────────┘  └─────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │  Metrics    │  │  Notify     │  │  Audit      │
    │  Consumer   │  │  Consumer   │  │  Consumer   │
    └─────────────┘  └─────────────┘  └─────────────┘
```

### Eventos Publicados

| Evento | Cola | Propósito |
|--------|------|-----------|
| `TicketCreatedEvent` | tickets.metrics | Contadores, estadísticas |
| `TicketStatusChangedEvent` | tickets.metrics | Métricas de cambios |
| `TicketAssignedEvent` | tickets.notifications | Notificar a agente |
| `TicketCreatedEvent` | tickets.notifications | Notificar creación |

---

## 🔐 Outbox Pattern

### Flujo del Outbox

```
1. [API] Recibe request
        │
        ▼
2. [Transaction] 
   ├── Guardar cambios en DB
   └── Guardar evento en Outbox
        │
        ▼
3. [Commit] Ambos guardan atómicamente
        │
        ▼
4. [Worker] Lee Outbox cada 5 segundos
        │
        ▼
5. [Worker] Publica a RabbitMQ
        │
        ▼
6. [Worker] Marca como procesado
```

### Por qué Outbox?

✅ **Garantiza entrega** - No se pierden eventos  
✅ **Transaccional** - Todo o nada  
✅ **Resiliente** - Reintenta en caso de falla  
✅ **Trazabilidad** - Auditoría completa  

---

## 🗄️ Base de Datos

### Tablas Principales

```
┌──────────────────┐
│     Tickets      │
├──────────────────┤
│ Id (PK)          │
│ Title            │
│ Description      │
│ Status           │
│ Priority         │
│ AssignedTo (FK)  │
│ CreatedBy (FK)   │
│ CreatedAt        │
│ UpdatedAt        │
└──────────────────┘
         │
         │ 1:N
         ▼
┌──────────────────┐
│ TicketActivities │
├──────────────────┤
│ Id (PK)          │
│ TicketId (FK)    │
│ UserId (FK)      │
│ Action           │
│ OldValue         │
│ NewValue         │
│ Timestamp        │
└──────────────────┘

┌──────────────────┐
│      Users       │
├──────────────────┤
│ Id (PK)          │
│ Name             │
│ Email (Unique)   │
│ Role             │
│ CreatedAt        │
└──────────────────┘

┌──────────────────┐
│ OutboxMessages   │
├──────────────────┤
│ Id (PK)          │
│ EventType        │
│ Payload (JSON)   │
│ CreatedAt        │
│ ProcessedAt      │
└──────────────────┘
```

---

## 🚀 Flujo Completo: Crear y Asignar Ticket

```
┌─────────┐
│ Cliente │ 1. Crea ticket desde web
└────┬────┘
     │
     ▼
┌─────────────────┐
│   Frontend      │ 2. POST /api/tickets
└────┬────────────┘    { title, description, priority }
     │
     ▼
┌─────────────────┐
│   Backend API   │ 3. CreateTicketHandler
└────┬────────────┘    - Crear entidad Ticket
     │                 - Status = "nuevo"
     │                 - Generar TicketCreatedEvent
     ▼
┌─────────────────┐
│   PostgreSQL    │ 4. Guardar en transacción
│                 │    - INSERT Tickets
│   + Outbox      │    - INSERT OutboxMessages
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│   Worker        │ 5. Leer Outbox (polling 5s)
│   Outbox        │    - SELECT * FROM OutboxMessages
│   Processor     │      WHERE ProcessedAt IS NULL
└────┬────────────┘
     │
     ▼
┌─────────────────┐
│   RabbitMQ      │ 6. Publicar a exchanges
│                 │    - tickets.metrics
│                 │    - tickets.notifications
└────┬────────────┘
     │
     ├──────────────┬──────────────┐
     ▼              ▼              ▼
┌─────────┐   ┌──────────┐   ┌─────────┐
│Metrics  │   │Notificar │   │Audit    │
│Consumer │   │Admin     │   │Log      │
└─────────┘   └──────────┘   └─────────┘

     ⏱️ Admin asigna agente
     
┌─────────┐
│  Admin  │ 7. Asignar agente
└────┬────┘
     │
     ▼
┌─────────────────┐
│   Frontend      │ 8. POST /api/tickets/{id}/assign
└────┬────────────┘    { agentId }
     │
     ▼
┌─────────────────┐
│   Backend API   │ 9. AssignTicketHandler
└────┬────────────┘    - ticket.AssignTo(agentId)
     │                 - Status = "en-proceso"
     │                 - Generar TicketAssignedEvent
     ▼
┌─────────────────┐
│   PostgreSQL    │ 10. Guardar cambios
│   + Outbox      │     + TicketActivity log
└────┬────────────┘
     │
     ▼
    (Mismo flujo Outbox → RabbitMQ → Consumers)
```

---

## 📊 Monitoreo

### URLs de Monitoreo

| Servicio | URL | Información |
|----------|-----|-------------|
| **RabbitMQ Management** | http://localhost:15672 | Colas, mensajes, consumidores |
| **API Health** | http://localhost:5076/health | Estado de la API |
| **Swagger** | http://localhost:5076/swagger | Documentación API |

---

## 📝 Notas Técnicas

- **Outbox Polling**: Cada 5 segundos
- **Message TTL**: Sin límite (mensajes persistentes)
- **Retry Policy**: 3 reintentos con backoff exponencial
- **Transacciones**: PostgreSQL nivel Read Committed
- **Serialización**: JSON (System.Text.Json)

---

**Ver también**:
- `docs/rabbitmq-topology.md` - Detalles de RabbitMQ
- `docs/OUTBOX_WORKER.md` - Implementación del Outbox Pattern
- `EXECUTION.md` - Cómo ejecutar el sistema
