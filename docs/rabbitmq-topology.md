# RabbitMQ Topology - TicketFlow

## Visión General

Sistema de mensajería basado en RabbitMQ con patrón **Topic Exchange** para enrutamiento flexible de eventos de tickets.

---

## Exchange

### `tickets` (Topic Exchange)

Exchange principal para todos los eventos relacionados con tickets.

**Tipo**: `topic`  
**Durabilidad**: `durable` (sobrevive a reinicios del broker)  
**Auto-delete**: `false`

**Routing Keys**:
- `ticket.created` - Ticket nuevo creado
- `ticket.status.changed` - Ticket movido entre columnas
- `ticket.assigned` - Ticket asignado a un agente
- `ticket.updated` - Ticket modificado (título, tags, etc.)
- `ticket.deleted` - Ticket eliminado

**Ejemplo de publicación**:

```
Exchange: tickets
Routing Key: ticket.created
Headers:
  - x-event-id: evt-20251021153000-abc123
  - x-correlation-id: api-20251021153000-abc123
  - x-timestamp: 2025-10-21T15:30:00Z
Body:
  {
    "ticketId": "TF-1024",
    "title": "Fallo en pagos",
    "columnId": "nuevo",
    "assigneeId": "u123",
    "requesterId": "c88",
    "timestamp": "2025-10-21T15:30:00Z"
  }
```

---

## Queues

### 1. `notifications`

Cola para procesamiento de notificaciones a usuarios (email, push, etc.).

**Configuración**:
- **Durabilidad**: `durable`
- **Exclusividad**: `false` (múltiples consumers)
- **Auto-delete**: `false`
- **TTL**: Sin límite (mensajes permanecen hasta ser procesados)
- **Max Length**: 10,000 mensajes (protección contra saturación)
- **DLX**: `notifications.dlx` (Dead Letter Exchange)

**Bindings**:

| Routing Key | Descripción |
|-------------|-------------|
| `ticket.created` | Notificar al asignado que tiene un nuevo ticket |
| `ticket.status.changed` | Notificar cambios de estado al asignado y requester |
| `ticket.assigned` | Notificar al nuevo asignado |

**Consumer**:
- **Servicio**: `worker`
- **Prefetch Count**: 10 (procesa hasta 10 mensajes en paralelo)
- **Ack Mode**: Manual (confirma después de enviar notificación)

---

### 2. `metrics`

Cola para recolección de métricas y análisis.

**Configuración**:
- **Durabilidad**: `durable`
- **Exclusividad**: `false`
- **Auto-delete**: `false`
- **TTL**: 24 horas (métricas antiguas se descartan automáticamente)
- **Max Length**: 50,000 mensajes

**Bindings**:

| Routing Key | Descripción |
|-------------|-------------|
| `ticket.*` | Captura TODOS los eventos de tickets |

**Consumer**:
- **Servicio**: `worker`
- **Prefetch Count**: 50 (alta capacidad de procesamiento)
- **Ack Mode**: Automático (metrics no son críticos)

---

### 3. `notifications.dlq` (Dead Letter Queue)

Cola para mensajes de notificaciones que fallaron después de todos los reintentos.

**Configuración**:
- **Durabilidad**: `durable`
- **Exclusividad**: `false`
- **Auto-delete**: `false`
- **TTL**: 7 días (retención para análisis)

**Bindings**:

| Routing Key | Descripción |
|-------------|-------------|
| `ticket.created` | Mensajes fallidos de creación |
| `ticket.status.changed` | Mensajes fallidos de cambio de estado |
| `ticket.assigned` | Mensajes fallidos de asignación |

**Procesamiento**:
- **Modo**: Manual (requiere intervención humana)
- **Alertas**: Email a equipo de ops cuando `dlq > 0`
- **Reintento**: Script manual para republicar mensajes después de resolver el problema

---

## Headers Recomendados

Todos los mensajes publicados al exchange `tickets` deben incluir:

### `x-event-id`

Identificador único del evento.

**Formato**: `evt-{timestamp}-{uuid}`

**Ejemplo**: `evt-20251021153000-abc123def456`

**Uso**: Idempotencia (evitar procesar el mismo evento dos veces)

---

### `x-correlation-id`

Identificador de la operación end-to-end.

**Formato**: `{service}-{timestamp}-{uuid}`

**Ejemplo**: `api-20251021153000-abc123def456`

**Uso**: Rastreo distribuido (correlacionar logs entre API y Worker)

---

### `x-timestamp`

Timestamp de cuando se generó el evento (ISO 8601).

**Ejemplo**: `2025-10-21T15:30:00.123Z`

**Uso**: Análisis temporal, detección de delays en procesamiento

---

### `x-retry-count` (opcional)

Contador de reintentos (agregado automáticamente por RabbitMQ al reintentar).

**Ejemplo**: `0`, `1`, `2`, `3`

**Uso**: Decisiones de retry en el consumer

---

## Política de Reintentos

### Estrategia: Exponential Backoff Simple

Cuando un mensaje falla al procesarse, se reintenta con delays crecientes:

| Intento | Delay | Cola de Destino | Acción |
|---------|-------|-----------------|--------|
| 1 | 0s | `notifications` | Procesamiento inmediato |
| 2 | 1 minuto | `notifications.retry-1` | Dead letter después de TTL |
| 3 | 5 minutos | `notifications.retry-2` | Dead letter después de TTL |
| 4 | 15 minutos | `notifications.retry-3` | Dead letter después de TTL |
| 5+ | - | `notifications.dlq` | Requiere intervención manual |

---

### Implementación con Dead Letter Exchange (DLX)

#### 1. Queue `notifications`

```
Queue: notifications
DLX: notifications.dlx
DLX Routing Key: retry-1
x-max-retries: 3
```

Cuando un mensaje es **rejected** o **nack** con `requeue=false`, va a la DLX.

---

#### 2. Exchange `notifications.dlx` (Topic)

```
Exchange: notifications.dlx
Type: topic
Bindings:
  - retry-1 → notifications.retry-1
  - retry-2 → notifications.retry-2
  - retry-3 → notifications.retry-3
  - dlq → notifications.dlq
```

---

#### 3. Retry Queues

**`notifications.retry-1`**:
```
Queue: notifications.retry-1
TTL: 60000ms (1 minuto)
DLX: tickets (volver al exchange principal)
DLX Routing Key: {original-routing-key}
```

**`notifications.retry-2`**:
```
Queue: notifications.retry-2
TTL: 300000ms (5 minutos)
DLX: tickets
DLX Routing Key: {original-routing-key}
```

**`notifications.retry-3`**:
```
Queue: notifications.retry-3
TTL: 900000ms (15 minutos)
DLX: tickets
DLX Routing Key: {original-routing-key}
```

---

#### 4. Dead Letter Queue Final

**`notifications.dlq`**:
```
Queue: notifications.dlq
TTL: 604800000ms (7 días)
No DLX (final)
```

---

### Flujo de Reintentos

```
1. Mensaje llega a `notifications`
   ↓
2. Consumer falla al procesar
   ↓
3. Consumer hace NACK con requeue=false
   ↓
4. RabbitMQ envía mensaje a DLX `notifications.dlx` con routing key `retry-1`
   ↓
5. Mensaje llega a `notifications.retry-1`
   ↓
6. Espera 1 minuto (TTL)
   ↓
7. RabbitMQ envía mensaje de vuelta al exchange `tickets` con routing key original
   ↓
8. Mensaje llega a `notifications` (segundo intento)
   ↓
9. Si falla de nuevo → retry-2 (5 min) → retry-3 (15 min) → dlq (final)
```

---

## Monitoreo de Colas

### Métricas Clave

```
Queue: notifications
  - Messages Ready: 12
  - Messages Unacknowledged: 3
  - Consumer Count: 2
  - Publish Rate: 15 msg/s
  - Deliver Rate: 14 msg/s

Queue: notifications.retry-1
  - Messages Ready: 2
  
Queue: notifications.retry-2
  - Messages Ready: 1

Queue: notifications.dlq
  - Messages Ready: 0  ← Ideal, siempre en 0
```

**Alertas**:
- `notifications.ready > 100`: Worker lento
- `notifications.dlq > 0`: Mensajes perdidos, revisar logs
- `retry-3 > 10`: Problema persistente con envío de notificaciones

---

## Ejemplo de Configuración (C# con RabbitMQ.Client)

```csharp
// Declarar exchange principal
channel.ExchangeDeclare(
    exchange: "tickets",
    type: ExchangeType.Topic,
    durable: true,
    autoDelete: false
);

// Declarar DLX para reintentos
channel.ExchangeDeclare(
    exchange: "notifications.dlx",
    type: ExchangeType.Topic,
    durable: true
);

// Declarar queue notifications con DLX
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "notifications.dlx" },
    { "x-dead-letter-routing-key", "retry-1" },
    { "x-max-length", 10000 }
};

channel.QueueDeclare(
    queue: "notifications",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: args
);

// Bindings
channel.QueueBind("notifications", "tickets", "ticket.created");
channel.QueueBind("notifications", "tickets", "ticket.status.changed");
channel.QueueBind("notifications", "tickets", "ticket.assigned");
```

---

## Buenas Prácticas

1. **Idempotencia**: Usar `x-event-id` para evitar procesar el mismo evento dos veces
2. **Timeouts**: Consumer debe tener timeout de procesamiento < 30 segundos
3. **Logging**: Registrar `correlationId` en todos los logs del consumer
4. **Ack Manual**: Usar `basicAck` solo después de procesar exitosamente
5. **Nack con Requeue=False**: Para activar política de reintentos
6. **Monitoreo**: Configurar alertas para DLQ y colas de retry
7. **Limpieza**: Script periódico para limpiar DLQ después de análisis

---

## Diagrama de Topología

```
┌──────────────────┐
│  API Backend     │
│  (Publisher)     │
└────────┬─────────┘
         │ publish (routing key: ticket.created|status.changed|assigned)
         ↓
┌──────────────────────────────────────┐
│  Exchange: tickets (Topic)           │
└──────────┬───────────────────┬───────┘
           │                   │
           │ bind              │ bind (ticket.*)
           │ (specific keys)   │
           ↓                   ↓
  ┌────────────────┐    ┌────────────┐
  │  notifications │    │  metrics   │
  │  Queue         │    │  Queue     │
  └────────┬───────┘    └─────┬──────┘
           │                  │
           │ consume          │ consume
           ↓                  ↓
  ┌─────────────────┐  ┌─────────────┐
  │  Worker         │  │  Worker     │
  │  (Notifications)│  │  (Metrics)  │
  └─────────────────┘  └─────────────┘
           │
           │ NACK (on failure)
           ↓
  ┌─────────────────────┐
  │  notifications.dlx  │
  │  (DLX Exchange)     │
  └──────┬─────┬────┬───┘
         │     │    │
         ↓     ↓    ↓
    retry-1 retry-2 retry-3 → dlq
    (1m)    (5m)    (15m)   (final)
```
