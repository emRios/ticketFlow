# ADR-0002: RabbitMQ y Transactional Outbox

**Fecha**: 2025-10-21  
**Estado**: Aceptado  
**Contexto**: MVP de TicketFlow

---

## Contexto

El sistema necesita enviar notificaciones por email y actualizar métricas cuando ocurren eventos de negocio (ticket creado, status cambiado, ticket asignado). La comunicación entre el API Backend y el Worker debe ser asíncrona, confiable y garantizar que ningún evento se pierda, incluso si RabbitMQ está temporalmente caído. Se requiere garantizar **at-least-once delivery** y **idempotencia** en el procesamiento.

---

## Decisión

Adoptamos **RabbitMQ como message broker** con el patrón **Transactional Outbox** para publicación de eventos:

### 1. Transactional Outbox
Cuando el API crea o modifica un ticket:
1. **Insertar ticket** en tabla `Tickets`
2. **Insertar evento** en tabla `Outbox` (en la misma transacción ACID)
3. **Commit** de la transacción
4. **Dispatcher asíncrono** lee eventos pendientes de `Outbox` y los publica a RabbitMQ
5. **Marcar evento como despachado** (`DispatchedAt != NULL`)

### 2. RabbitMQ Topology
- **Exchange**: `tickets` (tipo `topic`)
- **Queues**: `notifications`, `metrics`, con DLQ (`notifications.dlq`, `metrics.dlq`)
- **Routing keys**: `ticket.created`, `ticket.status.changed`, `ticket.assigned`
- **Headers**: `x-event-id` (idempotencia), `x-correlation-id` (rastreo distribuido)

### 3. Idempotencia en Worker
Antes de procesar un mensaje:
1. Verificar si `eventId` existe en tabla `ProcessedEvents`
2. Si existe → ACK inmediato (mensaje ya procesado)
3. Si no existe → Procesar + Insertar en `ProcessedEvents` + ACK

---

## Consecuencias

### Positivas
- **Garantía de entrega**: Eventos persisten en `Outbox` aunque RabbitMQ esté caído
- **Consistencia eventual**: DB y mensajería siempre coherentes (no se pierden eventos)
- **Idempotencia**: Worker puede procesar duplicados sin efectos secundarios
- **Observabilidad**: `Outbox.DispatchedAt` permite detectar latencia de despacho
- **Reintentos controlados**: RabbitMQ maneja reintentos con exponential backoff (1m → 5m → 15m)
- **DLQ para análisis**: Mensajes con errores persistentes van a DLQ para revisión manual
- **Desacoplamiento**: API y Worker no comparten estado en memoria, solo mensajes

### Negativas
- **Latencia adicional**: Outbox dispatcher agrega 100-500ms de delay (configurable con `OUTBOX_DISPATCH_INTERVAL_MS`)
- **Tabla Outbox crece**: Requiere limpieza periódica de eventos antiguos (retener 30 días)
- **Complejidad operacional**: Monitorear colas de RabbitMQ, DLQ y tabla `Outbox` pendiente
- **Costo de infraestructura**: RabbitMQ consume ~512MB RAM (mitigado: lightweight en MVP)

---

## Alternativas Consideradas

### 1. Publicación directa a RabbitMQ (sin Outbox)
**Pros**: Más simple, menos latencia  
**Contras**: Si RabbitMQ cae después de commit de DB, el evento se pierde. No hay garantía de entrega.

**Razón de rechazo**: Inaceptable perder notificaciones críticas (ej: "Ticket asignado a ti"). La confiabilidad es más importante que 100-200ms de latencia.

### 2. Webhooks HTTP directos
**Pros**: Sin infraestructura de mensajería  
**Contras**: Requiere reintentos manuales, timeouts HTTP bloquean transacciones, no escala, no hay orden garantizado

**Razón de rechazo**: Webhooks sincrónicos no escalan y aumentan latencia del API. RabbitMQ permite procesamiento asíncrono en batch.

### 3. Azure Service Bus / AWS SQS
**Pros**: Servicios gestionados, sin mantenimiento de broker  
**Contras**: Vendor lock-in, costos por millón de mensajes, menos control sobre topología

**Razón de rechazo**: Preferimos portabilidad (Docker on-premise o cualquier cloud) y control total sobre exchanges/queues. RabbitMQ es open-source y maduro.

### 4. Kafka
**Pros**: Throughput extremo (millones de eventos/seg), event sourcing nativo, retención infinita  
**Contras**: Complejidad operacional alta (ZooKeeper/KRaft, particiones, rebalanceo), overhead para < 1000 msgs/min

**Razón de rechazo**: Sobrecarga innecesaria para MVP. RabbitMQ maneja 10,000 msgs/min fácilmente, suficiente para años.

### 5. Polling de tabla Tickets (sin mensajería)
**Pros**: Sin infraestructura adicional  
**Contras**: Polling constante aumenta carga de DB, latencia alta (5-10 segundos), no reactivo

**Razón de rechazo**: Polling no escala y no es event-driven. RabbitMQ permite reacción inmediata (< 100ms) a eventos.

---

## Implementación

### Outbox Dispatcher (API)
```csharp
// Background service que corre cada 500ms
public class OutboxDispatcher : BackgroundService {
  protected override async Task ExecuteAsync(CancellationToken ct) {
    while (!ct.IsCancellationRequested) {
      var pendingEvents = await _outboxRepo.GetPendingAsync(limit: 100);
      
      foreach (var evt in pendingEvents) {
        await _rabbitPublisher.PublishAsync(evt.Type, evt.PayloadJson);
        await _outboxRepo.MarkDispatchedAsync(evt.Id);
      }
      
      await Task.Delay(500, ct); // OUTBOX_DISPATCH_INTERVAL_MS
    }
  }
}
```

### Consumer con Idempotencia (Worker)
```csharp
public class NotificationsConsumer {
  public async Task ConsumeAsync(BasicDeliverEventArgs ea) {
    var eventId = ea.BasicProperties.Headers["x-event-id"];
    
    // Idempotencia: verificar si ya fue procesado
    if (await _processedRepo.ExistsAsync(eventId)) {
      _channel.BasicAck(ea.DeliveryTag, multiple: false);
      return;
    }
    
    // Procesar mensaje (enviar email, etc.)
    await _notificationService.SendAsync(message);
    
    // Registrar como procesado
    await _processedRepo.InsertAsync(eventId);
    
    // ACK para remover de cola
    _channel.BasicAck(ea.DeliveryTag, multiple: false);
  }
}
```

---

## Monitoreo y Alertas

### Métricas clave
- `outbox_pending_total` > 100 → Dispatcher lento o RabbitMQ caído
- `mq_dlq_total` > 0 → Mensajes fallaron después de reintentos (CRÍTICO)
- `outbox_dispatch_latency_ms` P95 > 2000ms → Cuello de botella en dispatcher

### Health checks
- API: Verificar conexión a RabbitMQ en `/health`
- Worker: Heartbeat cada 30 segundos con timestamp de último mensaje procesado

---

## Migración Futura

Si el volumen crece (> 100,000 msgs/min):
1. **Escalar Workers horizontalmente**: Múltiples instancias consumen la misma cola (RabbitMQ balancea automáticamente)
2. **Sharding de Outbox**: Particionar tabla `Outbox` por fecha para queries rápidos
3. **Cambiar a Kafka**: Solo si event sourcing completo o retención > 7 días es necesario

Para MVP, RabbitMQ + Outbox cubre 99% de los casos sin complejidad adicional.

---

## Referencias

- [Transactional Outbox Pattern - Microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- [RabbitMQ Best Practices](https://www.rabbitmq.com/best-practices.html)
- [Idempotent Consumer Pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
- Ver `docs/rabbitmq-topology.md` para configuración detallada
