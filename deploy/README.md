# 🐳 Docker Compose - TicketFlow

## 📋 Servicios Configurados

### 1. **postgres** (PostgreSQL 16)
- **Puerto**: 5432
- **Database**: ticketflow
- **Usuario**: ticketflow_user
- **Password**: ticketflow_pass
- **Volume**: postgres_data (persistente)
- **Healthcheck**: `pg_isready`

### 2. **rabbitmq** (RabbitMQ 3.13 + Management)
- **Puerto AMQP**: 5672
- **Puerto Management UI**: 15672
- **Usuario**: ticketflow_user
- **Password**: ticketflow_pass
- **Volume**: rabbitmq_data (persistente)
- **Healthcheck**: `rabbitmq-diagnostics ping`

### 3. **api** (TicketFlow.Api)
- **Puerto**: 5076
- **Image**: ticketflow-api:latest
- **Depends on**: postgres, rabbitmq
- **Env file**: `deploy/env/backend.env`
- **Healthcheck**: HTTP GET /health
- **Dll**: TicketFlow.Api.dll

### 4. **worker** (TicketFlow.Worker)
- **Image**: ticketflow-worker:latest
- **Depends on**: postgres, rabbitmq, api
- **Env file**: `deploy/env/worker.env`
- **Dll**: TicketFlow.Worker.dll

---

## 🚀 Comandos

### Levantar todos los servicios

```bash
cd deploy
docker compose up -d
```

### Levantar solo infraestructura (postgres + rabbitmq)

```bash
docker compose up postgres rabbitmq -d
```

### Ver logs en tiempo real

```bash
# Todos los servicios
docker compose logs -f

# Solo API
docker compose logs -f api

# Solo Worker
docker compose logs -f worker

# Solo infraestructura
docker compose logs -f postgres rabbitmq
```

### Rebuild después de cambios en código

```bash
docker compose up --build -d
```

### Ver estado de servicios

```bash
docker compose ps
```

### Detener servicios

```bash
docker compose stop
```

### Detener y eliminar contenedores

```bash
docker compose down
```

### Detener y eliminar volúmenes (⚠️ BORRA DATOS)

```bash
docker compose down -v
```

---

## 🔧 Variables de Entorno

### Archivo: `deploy/env/backend.env`

```env
APP_URL=http://localhost:5076
LOG_LEVEL=Information
OUTBOX_ENABLED=true
OUTBOX_DISPATCH_INTERVAL_MS=5000
CORS__ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000
```

**Nota**: Las variables críticas (DB, RabbitMQ, JWT) se configuran directamente en `docker-compose.yml` para evitar problemas de precedencia.

### Archivo: `deploy/env/worker.env`

```env
EMAIL_FROM=noreply@ticketflow.com
EMAIL_SMTP_HOST=smtp.mailtrap.io
EMAIL_SMTP_PORT=587
APP_URL=http://localhost:5076
LOG_LEVEL=Information
WORKER_POLLING_INTERVAL_SECONDS=5
WORKER_BATCH_SIZE=50
WORKER_MAX_ATTEMPTS=5
```

---

## 🏥 Health Checks

### PostgreSQL

```bash
docker exec ticketflow-postgres pg_isready -U ticketflow_user -d ticketflow
```

### RabbitMQ

```bash
docker exec ticketflow-rabbitmq rabbitmq-diagnostics ping
```

### API

```bash
curl http://localhost:5076/health
```

---

## 📊 Verificación de Topología RabbitMQ

### Management UI

1. Abrir: http://localhost:15672
2. Login: `ticketflow_user` / `ticketflow_pass`
3. Ir a **Exchanges** → Verificar `tickets` (tipo: topic)
4. Ir a **Queues** → Verificar `notifications` y `metrics`

### CLI

```bash
# Listar exchanges
docker exec ticketflow-rabbitmq rabbitmqctl list_exchanges

# Listar colas
docker exec ticketflow-rabbitmq rabbitmqctl list_queues

# Listar bindings
docker exec ticketflow-rabbitmq rabbitmqctl list_bindings
```

---

## 🗄️ Acceso a PostgreSQL

### Usando psql dentro del contenedor

```bash
docker exec -it ticketflow-postgres psql -U ticketflow_user -d ticketflow
```

### Desde host (si tienes psql instalado)

```bash
psql -h localhost -p 5432 -U ticketflow_user -d ticketflow
```

**Comandos útiles en psql:**

```sql
-- Listar tablas
\dt

-- Ver OutboxMessages
SELECT * FROM "OutboxMessages" WHERE "DispatchedAt" IS NULL;

-- Ver ProcessedEvents
SELECT * FROM "ProcessedEvents";

-- Ver Tickets
SELECT * FROM "Tickets";
```

---

## 🧪 Pruebas End-to-End

### 1. Crear un ticket desde la API

```bash
curl -X POST http://localhost:5076/api/tickets \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "title": "Test desde Docker",
    "description": "Ticket de prueba",
    "priority": "HIGH",
    "status": "OPEN"
  }'
```

### 2. Verificar OutboxMessage creado

```bash
docker exec -it ticketflow-postgres psql -U ticketflow_user -d ticketflow \
  -c "SELECT \"Id\", \"Type\", \"OccurredAt\", \"DispatchedAt\" FROM \"OutboxMessages\";"
```

### 3. Ver logs del Worker procesando

```bash
docker compose logs -f worker
```

Deberías ver:

```log
[INFO] Lock advisory (42) adquirido. Procesando mensajes Outbox...
[INFO] Procesando 1 mensajes del Outbox (Lock ID: 42)
[INFO] 📤 Mensaje publicado → Exchange: tickets, RoutingKey: ticket.created
[INFO] ✅ Mensaje publicado: TicketCreatedEvent (ID: ...)
[INFO] Procesamiento completado. Exitosos: 1, Fallidos: 0, Omitidos: 0
```

### 4. Verificar mensaje en RabbitMQ

- Ir a http://localhost:15672
- Queues → `notifications` o `metrics`
- Click en la cola → **Get messages**

---

## 🔍 Troubleshooting

### Error: "Connection refused" al conectar a postgres

**Solución**: Espera a que el healthcheck pase (10-15 segundos)

```bash
docker compose logs postgres | grep "ready to accept connections"
```

### Error: Worker no puede conectarse a RabbitMQ

**Solución**: Verifica que RabbitMQ esté listo

```bash
docker compose ps rabbitmq
# Status debe ser "healthy"
```

### API no responde en puerto 5076

**Solución**: Verifica logs de la API

```bash
docker compose logs api | grep "Now listening"
# Debe mostrar: Now listening on: http://[::]:5076
```

### Reiniciar un servicio específico

```bash
docker compose restart api
docker compose restart worker
```

### Ver uso de recursos

```bash
docker stats ticketflow-api ticketflow-worker
```

---

## 📦 Estructura de Volúmenes

```
deploy/
├── docker-compose.yml
├── env/
│   ├── backend.env          ← Variables de entorno API
│   ├── worker.env           ← Variables de entorno Worker
│   ├── backend.env.example  ← Template
│   └── worker.env.example   ← Template
└── volumes/ (auto-creado por Docker)
    ├── postgres_data/       ← Datos de PostgreSQL
    └── rabbitmq_data/       ← Datos de RabbitMQ
```

---

## 🔄 Workflow de Desarrollo

### 1. Primera vez

```bash
cd deploy
docker compose up -d
# Esperar 30 segundos para que todo arranque
docker compose logs -f
```

### 2. Después de cambios en código

```bash
# Rebuild solo API
docker compose up --build api -d

# Rebuild solo Worker
docker compose up --build worker -d

# Rebuild todo
docker compose up --build -d
```

### 3. Limpiar y empezar de cero

```bash
docker compose down -v  # ⚠️ Borra datos
docker compose up -d
```

---

## 📊 Arquitectura de Red

```
┌─────────────────────────────────────────────────┐
│         ticketflow-network (bridge)             │
│                                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │ postgres │  │ rabbitmq │  │   api    │     │
│  │  :5432   │  │  :5672   │  │  :5076   │     │
│  └──────────┘  └──────────┘  └──────────┘     │
│                                    ▲            │
│                                    │            │
│                              ┌──────────┐       │
│                              │  worker  │       │
│                              └──────────┘       │
└─────────────────────────────────────────────────┘
         │            │            │
         │            │            │
    localhost:5432  localhost:5672  localhost:5076
                  localhost:15672
```

---

## ✅ Checklist de Verificación

Después de ejecutar `docker compose up -d`, verifica:

- [ ] `docker compose ps` muestra 4 servicios "Up"
- [ ] Postgres: `docker exec ticketflow-postgres pg_isready` retorna "accepting connections"
- [ ] RabbitMQ Management UI: http://localhost:15672 accesible
- [ ] API Health: `curl http://localhost:5076/health` retorna 200 OK
- [ ] Worker logs: `docker compose logs worker` muestra "Outbox Worker iniciado"
- [ ] RabbitMQ Topology: Exchange `tickets` y colas `notifications`, `metrics` creados

---

## 🚀 Siguiente Paso

Una vez que todos los servicios estén corriendo:

1. Generar JWT token (ver `docs/OUTBOX_WORKER.md`)
2. Crear un ticket desde el frontend o con curl
3. Ver el ticket procesado por el Worker
4. Verificar mensaje en RabbitMQ
5. Consumir mensajes desde las colas

¡Todo listo para pruebas en tiempo real! 🎉
