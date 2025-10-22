# Health Checks y Monitoreo

## API `/health`

Endpoint de health check del backend que valida el estado de los componentes críticos.

### Componentes Verificados

#### 1. Database (PostgreSQL/SQL Server)
- **Check**: Conexión activa a la base de datos
- **Validación**: Query simple (`SELECT 1`) para verificar disponibilidad
- **Timeout**: 5 segundos
- **Estado esperado**: `Healthy`
- **Acciones en fallo**:
  - Log de error con `correlationId`
  - Retornar status `Unhealthy`
  - Notificar a sistema de alertas

#### 2. RabbitMQ
- **Check**: Conexión al broker de mensajes
- **Validación**: Verificar canal activo y exchange configurado
- **Timeout**: 3 segundos
- **Estado esperado**: `Healthy`
- **Acciones en fallo**:
  - Log de error con `correlationId`
  - Retornar status `Degraded` (API puede funcionar sin mensajería)
  - Notificar a sistema de alertas

### Respuesta del Endpoint

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "timestamp": "2025-10-21T15:30:00Z",
  "components": {
    "database": {
      "status": "Healthy",
      "responseTime": "15ms"
    },
    "rabbitmq": {
      "status": "Healthy",
      "responseTime": "8ms"
    }
  }
}
```

---

## Worker Heartbeat

Sistema de monitoreo del worker que valida el procesamiento de mensajes.

### Consumers Monitoreados

#### 1. Notifications Consumer
- **Queue**: `notifications`
- **Eventos procesados**:
  - `ticket.created`
  - `ticket.status.changed`
  - `ticket.assigned`
- **Health check**:
  - Verificar último mensaje procesado < 5 minutos
  - Validar tasa de procesamiento > 0 msg/s
  - Revisar errores consecutivos < 10

#### 2. Metrics Consumer
- **Queue**: `metrics`
- **Eventos procesados**: `ticket.*` (todos)
- **Health check**:
  - Verificar último mensaje procesado < 10 minutos
  - Validar acumulación de métricas
  - Revisar pérdida de mensajes

### Heartbeat Log

Cada 30 segundos el worker registra:

```json
{
  "timestamp": "2025-10-21T15:30:00Z",
  "correlationId": "worker-hb-abc123",
  "eventId": "WorkerHeartbeat",
  "consumers": {
    "notifications": {
      "status": "Active",
      "lastProcessed": "2025-10-21T15:29:55Z",
      "processed": 1523,
      "errors": 2
    },
    "metrics": {
      "status": "Active",
      "lastProcessed": "2025-10-21T15:29:58Z",
      "processed": 3047,
      "errors": 0
    }
  }
}
```

---

## Métricas Mínimas

### 1. Tickets por Estado

Contador de tickets agrupados por columna/estado:

```json
{
  "metrics": {
    "tickets_by_status": {
      "nuevo": 45,
      "en-proceso": 123,
      "en-espera": 67,
      "resuelto": 1205
    },
    "timestamp": "2025-10-21T15:30:00Z"
  }
}
```

**Uso**: Dashboard de gestión, análisis de cuellos de botella.

---

### 2. Backlog de Notifications

Cantidad de mensajes pendientes de procesar en la queue:

```json
{
  "metrics": {
    "notifications_backlog": {
      "pending": 12,
      "processing": 3,
      "threshold": 100,
      "status": "OK"
    },
    "timestamp": "2025-10-21T15:30:00Z"
  }
}
```

**Alertas**:
- `pending > 100`: Warning - Worker lento
- `pending > 500`: Critical - Worker saturado
- `pending > 1000`: Emergency - Escalar workers

---

### 3. Reintentos y Dead Letter Queue (DLQ)

Contador de mensajes que fallaron y fueron reenviados:

```json
{
  "metrics": {
    "retries": {
      "notifications": {
        "retry_1": 5,
        "retry_2": 2,
        "retry_3": 1,
        "dlq": 0
      }
    },
    "timestamp": "2025-10-21T15:30:00Z"
  }
}
```

**Acciones**:
- `retry_1 > 10/min`: Investigar errores transitorios
- `retry_3 > 5/min`: Problema persistente, revisar logs
- `dlq > 0`: Mensajes perdidos, intervención manual requerida

---

## Logging con CorrelationId y EventId

### CorrelationId

Identificador único que rastrea una operación a través de múltiples servicios.

**Formato**: `{service}-{timestamp}-{uuid}`

**Ejemplo**: `api-20251021153000-abc123def456`

**Propagación**:
1. API genera `correlationId` al recibir request
2. Se almacena en logs de la operación
3. Se envía en headers de RabbitMQ (`x-correlation-id`)
4. Worker usa el mismo `correlationId` para logs de procesamiento

**Uso**: Rastrear una operación end-to-end desde API hasta notificación enviada.

---

### EventId

Identificador del tipo de evento/operación para facilitar filtrado.

**Ejemplos**:
- `TicketCreated`
- `TicketMoved`
- `TicketAssigned`
- `NotificationSent`
- `NotificationFailed`
- `WorkerHeartbeat`
- `HealthCheckExecuted`

**Formato de Log**:

```json
{
  "timestamp": "2025-10-21T15:30:00.123Z",
  "level": "Information",
  "correlationId": "api-20251021153000-abc123",
  "eventId": "TicketCreated",
  "message": "Ticket TF-1024 created by user u123",
  "properties": {
    "ticketId": "TF-1024",
    "userId": "u123",
    "columnId": "nuevo"
  }
}
```

**Uso**: Filtrar logs por tipo de operación, análisis de patrones, debugging.

---

## Alertas Recomendadas

| Métrica | Umbral Warning | Umbral Critical | Acción |
|---------|---------------|-----------------|--------|
| Database Response Time | > 200ms | > 1000ms | Escalar DB, revisar queries |
| RabbitMQ Backlog | > 100 | > 500 | Escalar workers |
| DLQ Messages | > 0 | > 10 | Intervención manual |
| Worker Errors | > 10/min | > 50/min | Reiniciar worker, revisar código |
| Health Check Fails | 2 consecutivos | 5 consecutivos | Alerta a equipo on-call |

---

## Dashboard de Monitoreo

**Grafana/Prometheus** (recomendado):
- Panel 1: Tickets por estado (gráfico de barras)
- Panel 2: Backlog de notifications (time series)
- Panel 3: Tasa de reintentos (time series)
- Panel 4: Health status (indicadores)
- Panel 5: Response times (histogramas)

**Logs centralizados** (Seq/ELK):
- Filtrar por `correlationId` para rastreo end-to-end
- Filtrar por `eventId` para análisis de patrones
- Dashboard de errores agrupados por servicio


1) Levantar infraestructura
# Desde la carpeta deploy/
docker compose up -d
docker compose ps


Debes ver ticketflow-postgres y ticketflow-rabbitmq con Up (healthy).

2) Healthchecks (compose)
docker inspect --format='{{json .State.Health}}' ticketflow-postgres | jq
docker inspect --format='{{json .State.Health}}' ticketflow-rabbitmq | jq


Estado esperado: "Status":"healthy" en ambos.

3) PostgreSQL – conectividad
# Ping
docker exec -it ticketflow-postgres pg_isready -U ticketflow_user -d ticketflow

# Cadena de conexión (para referencia)
# Host=localhost;Port=5432;Database=ticketflow;Username=ticketflow_user;Password=ticketflow_pass

4) RabbitMQ – UI y AMQP

UI: http://localhost:15672

Usuario/Pass: ticketflow_user / ticketflow_pass

AMQP: amqp://ticketflow_user:ticketflow_pass@localhost:5672/

Ver bindings (rápido)

En la UI: Exchanges → amq.topic (o tu tickets cuando exista).

Confirma que el vhost / está activo y la conexión aparece en Connections.

5) Apagar (si hace falta)
docker compose down
# agrega --volumes si quieres limpiar datos persistentes

6) Problemas comunes (quick fix)

Puerto 5432/15672 ocupado → cierra otros contenedores/servicios (Postgres/Rabbit locales).

unhealthy → revisa logs:

docker logs ticketflow-postgres --tail 100
docker logs ticketflow-rabbitmq --tail 100


Credenciales inválidas → alinea con deploy/docker-compose.yml y deploy/env/*.example.

---

## Contracts de Health

Ejemplos de respuestas JSON de los endpoints de health check.

### API `/health` - 200 OK (Healthy)

Respuesta cuando todos los componentes están operativos:

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "db",
      "status": "Healthy",
      "latencyMs": 12
    },
    {
      "name": "rabbitmq",
      "status": "Healthy",
      "latencyMs": 8
    }
  ],
  "version": "api-1.0.0",
  "uptimeSec": 12345,
  "correlationId": "e2a421f8-2c46-4dcd-9d51-95f1c3d9d5c0"
}
```

**Campos**:
- `status`: Estado general del servicio (`Healthy` | `Degraded` | `Unhealthy`)
- `checks`: Array con resultado de cada componente verificado
  - `name`: Identificador del componente (db, rabbitmq, etc.)
  - `status`: Estado del componente específico
  - `latencyMs`: Tiempo de respuesta del check en milisegundos
  - `error`: (Opcional) Mensaje de error si el check falla
- `version`: Versión semántica del servicio API
- `uptimeSec`: Tiempo en segundos desde que el servicio inició
- `correlationId`: ID único para rastreo de la petición

---

### API `/health` - 503 Unhealthy

Respuesta cuando al menos un componente crítico falla:

```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "db",
      "status": "Unhealthy",
      "error": "timeout"
    },
    {
      "name": "rabbitmq",
      "status": "Healthy",
      "latencyMs": 10
    }
  ],
  "version": "api-1.0.0",
  "uptimeSec": 234,
  "correlationId": "e2a421f8-2c46-4dcd-9d51-95f1c3d9d5c0"
}
```

**Códigos HTTP**:
- `200 OK`: Todos los checks Healthy
- `503 Service Unavailable`: Al menos un check Unhealthy
- `500 Internal Server Error`: Error inesperado al ejecutar checks

**Uso**:
- Load balancers (Kubernetes, Docker Swarm) consultan este endpoint
- Si retorna 503, el servicio se marca como "down" y se redirige tráfico
- Logs estructurados registran cada cambio de estado (Healthy → Unhealthy)

---

### Worker Heartbeat (Ejemplo)

El worker expone métricas de salud internamente (o vía logs estructurados):

```json
{
  "status": "Running",
  "consumers": [
    {
      "queue": "notifications",
      "lastMessageAt": "2025-10-21T18:45:00Z",
      "pending": 0,
      "retriesLastHour": 1
    },
    {
      "queue": "metrics",
      "lastMessageAt": "2025-10-21T18:44:12Z",
      "pending": 5
    }
  ],
  "version": "worker-1.0.0",
  "uptimeSec": 9876
}
```

**Campos**:
- `status`: Estado del worker (`Running` | `Degraded` | `Stopped`)
- `consumers`: Array con estado de cada consumer de RabbitMQ
  - `queue`: Nombre de la cola consumida
  - `lastMessageAt`: Timestamp del último mensaje procesado (UTC)
  - `pending`: Número de mensajes pendientes en la cola
  - `retriesLastHour`: (Opcional) Cantidad de reintentos en la última hora
- `version`: Versión semántica del worker
- `uptimeSec`: Tiempo en segundos desde que el worker inició

**Alertas sugeridas**:
- Si `lastMessageAt` tiene más de 10 minutos → Consumer atascado
- Si `pending` > 100 → Acumulación de mensajes (backlog)
- Si `retriesLastHour` > 20 → Posible problema con procesamiento
- Si `status` = `Stopped` → Worker caído, escalar manualmente

**Implementación**:
- Worker puede exponer endpoint HTTP simple (ej: `:9090/health`)
- O escribir heartbeat a archivo/log cada 30 segundos
- Monitoreo externo (Prometheus, Grafana) consulta periódicamente