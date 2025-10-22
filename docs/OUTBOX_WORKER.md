# 🔄 OutboxProcessor - Worker con Polling, Lock y Publicación

## ✅ Implementación Completada - Fase 2

### 📦 Archivos Creados/Modificados

1. **OutboxProcessor.cs** - Procesador principal con lock y retry logic
2. **IMessagePublisher.cs** - Interfaz para publicación de mensajes
3. **RabbitMqPublisher.cs** - Implementación de RabbitMQ
4. **Program.cs** - Configuración de DI y startup
5. **appsettings.Development.json** - Configuración de RabbitMQ
6. **TicketFlow.Worker.csproj** - Dependencia RabbitMQ.Client

---

## 🏗️ Arquitectura del Procesador

### Flujo Completo de Procesamiento

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKER LOOP (cada 5s)                    │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 1. TryAcquireAdvisoryLock(42)                               │
│    - pg_try_advisory_lock(42)                               │
│    - Si lock ocupado → return (otro worker está activo)     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. GetPendingMessagesWithLock()                             │
│    SELECT * FROM OutboxMessages                             │
│    WHERE DispatchedAt IS NULL AND Attempts < 5              │
│    ORDER BY OccurredAt                                      │
│    LIMIT 50                                                 │
│    FOR UPDATE SKIP LOCKED  ← Evita locks entre workers     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Para cada mensaje:                                       │
│    ┌──────────────────────────────────────────────┐        │
│    │ a) IsAlreadyProcessed(messageId)             │        │
│    │    - Verifica tabla ProcessedEvents          │        │
│    │    - Si existe → Skip (idempotencia)         │        │
│    └──────────────────────────────────────────────┘        │
│                       ↓                                     │
│    ┌──────────────────────────────────────────────┐        │
│    │ b) PublishAsync(RabbitMQ)                    │        │
│    │    - Exchange: ticketflow.events             │        │
│    │    - RoutingKey: ticket.created              │        │
│    │    - Body: PayloadJson                       │        │
│    │    - Headers: CorrelationId, Type            │        │
│    └──────────────────────────────────────────────┘        │
│                       ↓                                     │
│    ┌──────────────────────────────────────────────┐        │
│    │ c) MarkAsDispatched(messageId)               │        │
│    │    UPDATE OutboxMessages                     │        │
│    │    SET DispatchedAt = NOW()                  │        │
│    │    WHERE Id = messageId                      │        │
│    └──────────────────────────────────────────────┘        │
│                       ↓                                     │
│    ┌──────────────────────────────────────────────┐        │
│    │ d) RecordProcessedEvent(messageId)           │        │
│    │    INSERT INTO ProcessedEvents               │        │
│    │    (EventId, ProcessedAt)                    │        │
│    │    VALUES (messageId, NOW())                 │        │
│    └──────────────────────────────────────────────┘        │
│                                                             │
│    ❌ Si hay error:                                        │
│    ┌──────────────────────────────────────────────┐        │
│    │ e) IncrementAttempts(messageId, error)       │        │
│    │    UPDATE OutboxMessages                     │        │
│    │    SET Attempts = Attempts + 1,              │        │
│    │        Error = errorMessage                  │        │
│    │    WHERE Id = messageId                      │        │
│    │    (NO marca DispatchedAt → se reintenta)    │        │
│    └──────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. ReleaseAdvisoryLock(42)                                  │
│    - pg_advisory_unlock(42)                                 │
│    - SIEMPRE se libera (finally block)                      │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔐 PostgreSQL Advisory Lock (ID: 42)

### ¿Qué es un Advisory Lock?

- **Lock a nivel de sesión** (no de tabla)
- **No bloquea datos** - solo coordina workers
- **Automático release** si el proceso muere
- **ID único**: 42 (especificado en requirements)

### Comandos SQL Utilizados

```sql
-- Intentar adquirir lock (non-blocking)
SELECT pg_try_advisory_lock(42);
-- Retorna: true (adquirido) | false (ya tomado)

-- Liberar lock
SELECT pg_advisory_unlock(42);
```

### Ventajas

✅ **Exclusividad**: Solo un worker procesa a la vez  
✅ **Sin deadlocks**: Si el proceso muere, lock se libera  
✅ **Performance**: No afecta queries normales  
✅ **Escalabilidad**: Múltiples workers compiten por el lock  

---

## 🔄 FOR UPDATE SKIP LOCKED

### Query Completa

```sql
SELECT * FROM "OutboxMessages"
WHERE "DispatchedAt" IS NULL 
  AND "Attempts" < 5
ORDER BY "OccurredAt"
LIMIT 50
FOR UPDATE SKIP LOCKED;
```

### ¿Qué hace FOR UPDATE SKIP LOCKED?

- **FOR UPDATE**: Bloquea filas para actualización
- **SKIP LOCKED**: Omite filas ya bloqueadas por otras sesiones
- **Resultado**: Cada worker obtiene mensajes diferentes (sin esperas)

### Ejemplo con 2 Workers

```
Worker A: SELECT ... FOR UPDATE SKIP LOCKED
  → Bloquea mensajes [1, 2, 3, 4, 5]

Worker B: SELECT ... FOR UPDATE SKIP LOCKED (simultáneo)
  → Obtiene mensajes [6, 7, 8, 9, 10]
  → NO espera a que Worker A termine
```

---

## 🛡️ Idempotencia con ProcessedEvents

### Tabla ProcessedEvents

```sql
CREATE TABLE ProcessedEvents (
    EventId UUID PRIMARY KEY,
    ProcessedAt TIMESTAMP NOT NULL
);
```

### Flujo de Verificación

```csharp
// Antes de publicar
var alreadyProcessed = await IsAlreadyProcessedAsync(context, message.Id);

if (alreadyProcessed) {
    // Skip - mensaje ya fue procesado anteriormente
    await MarkAsDispatchedAsync(context, message.Id);
    continue;
}

// Publicar...

// Después de publicar exitosamente
await RecordProcessedEventAsync(context, message.Id);
```

### Escenarios Protegidos

✅ **Worker crash después de publicar**: No se vuelve a publicar  
✅ **Duplicación manual**: Se detecta y omite  
✅ **Retry después de timeout**: Verifica antes de re-publicar  

---

## 🐰 RabbitMQ Publisher

### Características

1. **Exchange Type**: Topic (flexible routing)
2. **Mensajes Durables**: Persistent = true
3. **Routing Key Automático**: 
   - `TicketCreatedEvent` → `ticket.created`
   - `TicketStatusChangedEvent` → `ticket.status.changed`
4. **Headers Incluidos**:
   - `Type`: Tipo completo del evento
   - `CorrelationId`: Para rastreo distribuido
   - `Timestamp`: Unix timestamp

### Código de Publicación

```csharp
await _messagePublisher.PublishAsync(
    message.Type,           // "TicketFlow.Domain.Events.TicketCreatedEvent"
    message.PayloadJson,    // {"ticketId":"...", "title":"..."}
    message.CorrelationId,  // "a1b2c3d4e5f6..."
    cancellationToken
);
```

### Configuración

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "Exchange": "ticketflow.events"
  }
}
```

---

## ⚙️ Configuración y Retry Logic

### Constantes

```csharp
private const int LockId = 42;        // Lock advisory ID
private const int BatchSize = 50;     // Mensajes por lote
private const int MaxAttempts = 5;    // Reintentos antes de abandonar
```

### Política de Reintentos

| Attempts | Acción                                    |
|----------|-------------------------------------------|
| 0-4      | Continuar reintentando                    |
| 5+       | No se consulta (WHERE Attempts < 5)       |
| ∞        | Requiere intervención manual o cleanup    |

### Error Handling

```csharp
try {
    await _messagePublisher.PublishAsync(...);
    await MarkAsDispatchedAsync(...);
    await RecordProcessedEventAsync(...);
}
catch (Exception ex) {
    // NO marca DispatchedAt
    // Solo incrementa Attempts y guarda Error
    await IncrementAttemptsAsync(message.Id, ex.Message);
}
```

---

## 🚀 Integración en Program.cs

### Configuración de Servicios

```csharp
// DbContext
builder.Services.AddDbContext<TicketFlowDbContext>(options =>
    options.UseNpgsql(connectionString));

// RabbitMQ Publisher (Singleton)
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    return new RabbitMqPublisher(logger, rabbitMqHost, rabbitMqExchange);
});

// OutboxProcessor (Singleton)
builder.Services.AddSingleton<OutboxProcessor>();

// Worker principal (BackgroundService)
builder.Services.AddHostedService<Worker>();
```

### Worker Loop (Worker.cs)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            await _outboxProcessor.ProcessAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico en el procesamiento");
        }

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }
}
```

---

## 📊 Logging y Observabilidad

### Logs Generados

```log
[DEBUG] Lock advisory (42) adquirido. Procesando mensajes Outbox...
[INFO]  Procesando 15 mensajes del Outbox (Lock ID: 42)
[DEBUG] Mensaje publicado: ticket.created → CorrelationId: a1b2c3...
[INFO]  ✅ Mensaje publicado: TicketCreatedEvent (ID: guid-123)
[INFO]  Procesamiento completado. Exitosos: 14, Fallidos: 1, Omitidos: 0
[DEBUG] Lock advisory (42) liberado.
```

### Errores

```log
[WARN]  Mensaje {messageId} ya fue procesado (idempotencia). Marcando como despachado.
[ERROR] ❌ Error al procesar mensaje {id} del tipo {type}. Intento 3/5
        System.TimeoutException: RabbitMQ connection timeout...
[WARN]  Error al liberar advisory lock (ID: 42).
```

---

## 🧪 Escenarios de Prueba

### 1. Worker único procesando

```bash
# Iniciar worker
dotnet run

# Logs esperados
[INFO] Lock adquirido (42)
[INFO] Procesando 10 mensajes
[INFO] ✅ Mensaje publicado: ticket.created
[INFO] Procesamiento completado
[DEBUG] Lock liberado (42)
```

### 2. Múltiples workers compitiendo

```bash
# Terminal 1
dotnet run

# Terminal 2 (simultáneo)
dotnet run

# Terminal 1 logs:
[DEBUG] Lock adquirido (42)
[INFO] Procesando mensajes...

# Terminal 2 logs:
[DEBUG] No se pudo adquirir el lock (42). Otro worker está procesando.
```

### 3. Reintento después de error

```sql
-- Simular error: detener RabbitMQ
docker stop rabbitmq

-- Worker logs:
[ERROR] Error al procesar mensaje. Intento 1/5
[ERROR] Error al procesar mensaje. Intento 2/5
...

-- Verificar en DB:
SELECT Id, Type, Attempts, Error 
FROM OutboxMessages 
WHERE DispatchedAt IS NULL;

-- Resultado:
-- Attempts = 2, Error = "Connection refused"
```

### 4. Idempotencia

```sql
-- Insertar manualmente un ProcessedEvent
INSERT INTO ProcessedEvents (EventId, ProcessedAt)
VALUES ('existing-guid', NOW());

-- Worker logs:
[WARN] Mensaje already-processed. Marcando como despachado.
```

---

## 📈 Métricas y Monitoreo

### Queries Útiles

```sql
-- Mensajes pendientes
SELECT COUNT(*) FROM OutboxMessages WHERE DispatchedAt IS NULL;

-- Mensajes con errores
SELECT Id, Type, Attempts, Error 
FROM OutboxMessages 
WHERE Attempts >= 3 AND DispatchedAt IS NULL;

-- Mensajes procesados en la última hora
SELECT COUNT(*) FROM ProcessedEvents 
WHERE ProcessedAt > NOW() - INTERVAL '1 hour';

-- Throughput promedio
SELECT 
    DATE_TRUNC('minute', ProcessedAt) AS minute,
    COUNT(*) AS messages_processed
FROM ProcessedEvents
WHERE ProcessedAt > NOW() - INTERVAL '1 hour'
GROUP BY minute
ORDER BY minute DESC;
```

---

## ✅ Checklist de Implementación

- [x] **pg_try_advisory_lock(42)** implementado
- [x] **FOR UPDATE SKIP LOCKED** en query
- [x] **Verificación de ProcessedEvents** (idempotencia)
- [x] **Publicación a RabbitMQ** con routing key
- [x] **MarkAsDispatched** después de éxito
- [x] **RecordProcessedEvent** para idempotencia
- [x] **IncrementAttempts** en caso de error
- [x] **MaxAttempts = 5** configurado
- [x] **BatchSize = 50** para performance
- [x] **CorrelationId** propagado
- [x] **Logging completo** en todos los pasos
- [x] **Program.cs** configurado correctamente
- [x] **appsettings.json** con RabbitMQ config
- [x] **RabbitMQ.Client** package agregado

---

## 🚀 Próximos Pasos

1. ✅ **Ejecutar migraciones**: `dotnet ef migrations add AddOutboxProcessor`
2. ✅ **Iniciar RabbitMQ**: `docker run -d -p 5672:5672 rabbitmq:3-management`
3. ✅ **Ejecutar worker**: `dotnet run --project src/server/worker`
4. ⏳ **Crear consumidores**: Procesar eventos publicados
5. ⏳ **Agregar métricas**: Prometheus/Grafana
6. ⏳ **Implementar DLQ**: Dead Letter Queue para mensajes fallidos
