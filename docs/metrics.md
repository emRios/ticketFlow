# Métricas Operativas (MVP)

Definición de métricas clave para monitoreo de salud y performance del sistema TicketFlow.

---

## Convenciones Generales

### Nomenclatura
- **Formato**: `snake_case` para nombres de métricas
- **Sufijos estándar**:
  - `_total`: Contadores acumulativos
  - `_ms`: Latencias en milisegundos
  - `_sec`: Duraciones en segundos
  - `_bytes`: Tamaños en bytes

### Etiquetas (Labels)
- **Nombres cortos**: `status`, `queue`, `code`, `route`
- **Valores específicos**: Evitar valores dinámicos ilimitados (no usar `user_id` como label)
- **Cardinalidad**: Mantener combinaciones < 1000 para evitar explosión de series

### Buckets de Latencia
Para histogramas de latencia (en milisegundos):
```
[1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000]
```

**Justificación**:
- 1-50ms: Queries rápidas (cache, índices)
- 100-500ms: Queries normales, llamadas HTTP
- 500-2500ms: Operaciones pesadas (batch, reportes)
- 5000ms+: Timeout típico (considerar alertas)

---

## Métricas del API Backend

### 1. `tickets_total`
**Tipo**: Counter  
**Descripción**: Total de tickets creados, agregados por estado actual  
**Labels**: `status`  
**Valores de `status`**: `NEW`, `IN_PROGRESS`, `ON_HOLD`, `RESOLVED`

**Ejemplo**:
```
tickets_total{status="NEW"} = 42
tickets_total{status="IN_PROGRESS"} = 128
tickets_total{status="ON_HOLD"} = 15
tickets_total{status="RESOLVED"} = 3456
```

**Uso**:
- Dashboard: Gráfico de distribución de tickets por estado
- Alerta: Si `NEW` > 100 → Backlog alto
- SLA: Ratio `RESOLVED / (NEW + IN_PROGRESS)` → Tasa de resolución

---

### 2. `http_requests_total`
**Tipo**: Counter  
**Descripción**: Total de peticiones HTTP recibidas por el API  
**Labels**: `route`, `code`  
**Valores de `route`**: `/tickets`, `/tickets/{id}`, `/health`, etc.  
**Valores de `code`**: `200`, `201`, `400`, `401`, `403`, `404`, `409`, `422`, `500`, `503`

**Ejemplo**:
```
http_requests_total{route="/tickets", code="201"} = 1234
http_requests_total{route="/tickets", code="400"} = 45
http_requests_total{route="/tickets/{id}/status", code="200"} = 890
http_requests_total{route="/tickets/{id}/status", code="409"} = 12
http_requests_total{route="/health", code="200"} = 98765
```

**Uso**:
- Dashboard: Requests por segundo (rate)
- Alerta: Si `code="500"` > 10/min → Error interno
- Alerta: Si `code="503"` > 0 → Servicio degradado
- SLA: Ratio `2xx / total` → Success rate

---

### 3. `outbox_pending_total`
**Tipo**: Gauge  
**Descripción**: Número actual de eventos pendientes de despacho en la tabla Outbox  
**Labels**: Ninguna

**Ejemplo**:
```
outbox_pending_total = 23
```

**Uso**:
- Dashboard: Gráfico de backlog de eventos
- Alerta: Si `outbox_pending_total` > 100 → Dispatcher lento o RabbitMQ caído
- Alerta: Si crecimiento constante (slope > 0) por 5 minutos → Investigar

**Cálculo**:
```sql
SELECT COUNT(*) FROM Outbox WHERE DispatchedAt IS NULL
```

---

### 4. `outbox_dispatch_latency_ms`
**Tipo**: Histogram  
**Descripción**: Tiempo entre creación del evento (`OccurredAt`) y su despacho (`DispatchedAt`)  
**Labels**: `event_type`  
**Valores de `event_type`**: `ticket.created`, `ticket.status.changed`, `ticket.assigned`

**Ejemplo**:
```
outbox_dispatch_latency_ms{event_type="ticket.created", le="50"} = 450
outbox_dispatch_latency_ms{event_type="ticket.created", le="100"} = 480
outbox_dispatch_latency_ms{event_type="ticket.created", le="500"} = 495
outbox_dispatch_latency_ms{event_type="ticket.created", le="+Inf"} = 500
```

**Uso**:
- Dashboard: P50, P95, P99 de latencia de despacho
- Alerta: Si P95 > 1000ms → Dispatcher sobrecargado
- SLA: 95% de eventos despachados en < 500ms

**Cálculo**:
```
latency = DispatchedAt - OccurredAt
```

---

## Métricas del Worker

### 5. `mq_messages_consumed_total`
**Tipo**: Counter  
**Descripción**: Total de mensajes consumidos de RabbitMQ (incluyendo reintentos exitosos)  
**Labels**: `queue`  
**Valores de `queue`**: `notifications`, `metrics`

**Ejemplo**:
```
mq_messages_consumed_total{queue="notifications"} = 5678
mq_messages_consumed_total{queue="metrics"} = 3421
```

**Uso**:
- Dashboard: Throughput de procesamiento (rate)
- Comparar con `mq_retries_total` → Ratio de éxito
- Alerta: Si rate = 0 por 5 minutos → Consumer detenido

---

### 6. `mq_retries_total`
**Tipo**: Counter  
**Descripción**: Total de mensajes que fallaron y fueron reenviados para reintento  
**Labels**: `queue`, `reason`  
**Valores de `queue`**: `notifications`, `metrics`  
**Valores de `reason`**: `timeout`, `smtp_error`, `validation_error`, `unknown`

**Ejemplo**:
```
mq_retries_total{queue="notifications", reason="smtp_error"} = 23
mq_retries_total{queue="notifications", reason="timeout"} = 5
mq_retries_total{queue="metrics", reason="validation_error"} = 2
```

**Uso**:
- Dashboard: Gráfico de reintentos por razón
- Alerta: Si `mq_retries_total` spike > 50/hora → Problema con dependencia externa (SMTP, etc.)
- Alerta: Si `reason="timeout"` incrementa → Servicios lentos

**Incremento**:
```csharp
if (processingFailed) {
  metrics.IncrementCounter("mq_retries_total", 
    ("queue", queueName), 
    ("reason", GetFailureReason(exception))
  );
}
```

---

### 7. `mq_dlq_total`
**Tipo**: Counter  
**Descripción**: Total de mensajes enviados a Dead Letter Queue (DLQ) después de agotar reintentos  
**Labels**: `queue`  
**Valores de `queue`**: `notifications`, `metrics`

**Ejemplo**:
```
mq_dlq_total{queue="notifications"} = 3
mq_dlq_total{queue="metrics"} = 0
```

**Uso**:
- Dashboard: Gráfico de mensajes perdidos
- Alerta: Si `mq_dlq_total` > 0 sostenido → **CRÍTICO** - Revisar DLQ manualmente
- SLA: DLQ debe ser 0 en operación normal

**Incremento**:
```csharp
if (attemptsExceeded && movedToDLQ) {
  metrics.IncrementCounter("mq_dlq_total", ("queue", queueName));
}
```

---

### 8. `notification_sent_total`
**Tipo**: Counter  
**Descripción**: Total de notificaciones enviadas exitosamente  
**Labels**: `channel`  
**Valores de `channel`**: `email`, `sms`, `push` (futuro)

**Ejemplo**:
```
notification_sent_total{channel="email"} = 4567
notification_sent_total{channel="sms"} = 0
```

**Uso**:
- Dashboard: Notificaciones enviadas por canal
- Comparar con `mq_messages_consumed_total{queue="notifications"}` → Ratio de éxito
- Alerta: Si ratio < 90% → Problema con proveedor (SMTP)

**Incremento**:
```csharp
if (emailSentSuccessfully) {
  metrics.IncrementCounter("notification_sent_total", ("channel", "email"));
}
```

---

## Alertas Sugeridas

### 1. DLQ Sostenido (CRÍTICO)
**Condición**:
```
mq_dlq_total > 0 AND rate(mq_dlq_total[5m]) > 0
```

**Significado**: Mensajes están llegando a DLQ continuamente

**Acción**:
1. Revisar logs del Worker con `grep "DLQ"`
2. Consultar tabla RabbitMQ DLQ en UI: http://localhost:15672/#/queues
3. Identificar razón de fallos recurrentes
4. Reparar causa raíz (SMTP caído, validación incorrecta, etc.)
5. Reprocesar mensajes de DLQ manualmente

---

### 2. Spike de Reintentos
**Condición**:
```
rate(mq_retries_total[5m]) > 10
```

**Significado**: Incremento anormal de reintentos (> 10 por minuto en últimos 5 min)

**Acción**:
1. Filtrar por `reason` para identificar causa
2. Si `reason="smtp_error"` → Verificar proveedor de email
3. Si `reason="timeout"` → Revisar latencia de dependencias
4. Si `reason="validation_error"` → Revisar cambios recientes en código

---

### 3. Outbox Pendiente Creciente
**Condición**:
```
deriv(outbox_pending_total[5m]) > 0 AND outbox_pending_total > 50
```

**Significado**: Backlog de eventos creciendo constantemente

**Acción**:
1. Verificar health de RabbitMQ: `GET /health` → Check `rabbitmq.status`
2. Revisar logs del Outbox Dispatcher
3. Verificar conectividad entre API y RabbitMQ
4. Considerar escalar Workers si throughput es insuficiente

---

### 4. Errores 5xx en API
**Condición**:
```
rate(http_requests_total{code=~"5.."}[5m]) > 5
```

**Significado**: Más de 5 errores internos por minuto

**Acción**:
1. Revisar logs del API filtrando por `level=ERROR`
2. Identificar endpoint afectado con label `route`
3. Verificar health de dependencias (DB, RabbitMQ)
4. Revisar stack traces en logs estructurados

---

### 5. Consumer Detenido
**Condición**:
```
rate(mq_messages_consumed_total{queue="notifications"}[5m]) == 0
```

**Significado**: Worker no está consumiendo mensajes

**Acción**:
1. Verificar estado del Worker: `docker ps` o `kubectl get pods`
2. Revisar logs del Worker: `docker logs ticketflow-worker --tail 100`
3. Verificar conexión a RabbitMQ
4. Reiniciar Worker si es necesario

---

### 6. Latencia Alta de Outbox
**Condición**:
```
histogram_quantile(0.95, outbox_dispatch_latency_ms) > 2000
```

**Significado**: P95 de latencia > 2 segundos

**Acción**:
1. Verificar carga del Outbox Dispatcher (CPU, memoria)
2. Revisar número de eventos pendientes (`outbox_pending_total`)
3. Considerar reducir `OUTBOX_DISPATCH_INTERVAL_MS` (más frecuente)
4. Escalar API horizontalmente si es necesario

---

## Implementación

### Backend (C#)
Usar biblioteca de métricas compatible con Prometheus (ej: `prometheus-net`):

```csharp
// Pseudocódigo
public class MetricsService {
  private readonly Counter _ticketsTotal;
  private readonly Counter _httpRequestsTotal;
  private readonly Gauge _outboxPendingTotal;
  private readonly Histogram _outboxDispatchLatency;
  
  public MetricsService() {
    _ticketsTotal = Metrics.CreateCounter(
      "tickets_total", 
      "Total tickets by status",
      new CounterConfiguration { LabelNames = new[] { "status" } }
    );
    
    _outboxDispatchLatency = Metrics.CreateHistogram(
      "outbox_dispatch_latency_ms",
      "Latency of outbox dispatch in milliseconds",
      new HistogramConfiguration {
        LabelNames = new[] { "event_type" },
        Buckets = new[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 }
      }
    );
  }
  
  public void IncrementTicketsTotal(string status) {
    _ticketsTotal.WithLabels(status).Inc();
  }
  
  public void ObserveDispatchLatency(string eventType, double latencyMs) {
    _outboxDispatchLatency.WithLabels(eventType).Observe(latencyMs);
  }
}
```

### Worker (C#)
Similar al backend, usando mismas convenciones:

```csharp
// Pseudocódigo
public class WorkerMetrics {
  private readonly Counter _messagesConsumed;
  private readonly Counter _retries;
  private readonly Counter _dlq;
  private readonly Counter _notificationsSent;
  
  public void IncrementConsumed(string queue) {
    _messagesConsumed.WithLabels(queue).Inc();
  }
  
  public void IncrementRetry(string queue, string reason) {
    _retries.WithLabels(queue, reason).Inc();
  }
}
```

### Exposición de Métricas
Ambos servicios deben exponer endpoint `/metrics` en formato Prometheus:

**API**:
```
GET http://localhost:5000/metrics
```

**Worker**:
```
GET http://localhost:9090/metrics
```

**Formato de respuesta** (Prometheus):
```
# HELP tickets_total Total tickets by status
# TYPE tickets_total counter
tickets_total{status="NEW"} 42
tickets_total{status="IN_PROGRESS"} 128
tickets_total{status="RESOLVED"} 3456

# HELP outbox_dispatch_latency_ms Latency of outbox dispatch in milliseconds
# TYPE outbox_dispatch_latency_ms histogram
outbox_dispatch_latency_ms_bucket{event_type="ticket.created",le="50"} 450
outbox_dispatch_latency_ms_bucket{event_type="ticket.created",le="100"} 480
outbox_dispatch_latency_ms_sum{event_type="ticket.created"} 23456.78
outbox_dispatch_latency_ms_count{event_type="ticket.created"} 500
```

---

## Integración con Prometheus

### Configuración de Scraping

Agregar targets en `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'ticketflow-api'
    static_configs:
      - targets: ['api:5000']
    scrape_interval: 15s
    metrics_path: /metrics

  - job_name: 'ticketflow-worker'
    static_configs:
      - targets: ['worker:9090']
    scrape_interval: 15s
    metrics_path: /metrics
```

### Queries Útiles (PromQL)

**Tasa de requests por segundo**:
```promql
rate(http_requests_total[5m])
```

**Success rate (2xx/total)**:
```promql
sum(rate(http_requests_total{code=~"2.."}[5m])) 
/ 
sum(rate(http_requests_total[5m]))
```

**P95 de latencia de outbox**:
```promql
histogram_quantile(0.95, rate(outbox_dispatch_latency_ms_bucket[5m]))
```

**Backlog de tickets nuevos**:
```promql
tickets_total{status="NEW"}
```

**Ratio de reintentos**:
```promql
rate(mq_retries_total[5m]) / rate(mq_messages_consumed_total[5m])
```

---

## Dashboard en Grafana

### Paneles Recomendados

#### 1. Overview
- **Tickets por estado**: Gauge con `tickets_total{status}`
- **Throughput API**: Graph con `rate(http_requests_total[5m])`
- **Success rate**: Stat panel con ratio `2xx / total`
- **Outbox pending**: Graph con `outbox_pending_total`

#### 2. API Health
- **Requests por endpoint**: Table con `sum by (route) (rate(http_requests_total[5m]))`
- **Errores 4xx/5xx**: Graph con `rate(http_requests_total{code=~"[45].."}[5m])`
- **Latencia de outbox**: Heatmap con `outbox_dispatch_latency_ms`

#### 3. Worker Health
- **Messages consumed**: Graph con `rate(mq_messages_consumed_total[5m])`
- **Retries por razón**: Stacked graph con `rate(mq_retries_total[5m]) by (reason)`
- **DLQ count**: Stat panel con `mq_dlq_total` (alerta si > 0)
- **Notificaciones enviadas**: Graph con `rate(notification_sent_total[5m])`

#### 4. Alerting Status
- **Active alerts**: Prometheus alerts panel
- **Outbox latency P95**: Gauge con threshold en 1000ms
- **Pending messages**: Graph con `outbox_pending_total` (threshold en 100)

---

## Testing de Métricas

### Casos de Prueba

1. **Counter incrementa**: Crear ticket → Verificar `tickets_total{status="NEW"}` += 1
2. **Histogram registra**: Despachar evento → Verificar bucket correcto en `outbox_dispatch_latency_ms`
3. **Gauge actualiza**: Insertar en Outbox → Verificar `outbox_pending_total` += 1
4. **Labels correctos**: Request a `/tickets` con 201 → Verificar `http_requests_total{route="/tickets",code="201"}`
5. **Formato Prometheus**: GET `/metrics` → Validar sintaxis con parser de Prometheus

---

## Extensiones Futuras

### Métricas Adicionales (Post-MVP)

**API**:
- `db_query_duration_ms{query}`: Latencia de queries SQL
- `jwt_validation_total{result}`: Validaciones de JWT (success/failure)
- `rate_limit_exceeded_total{route}`: Requests bloqueados por rate limiting

**Worker**:
- `email_delivery_duration_ms`: Latencia de envío de emails
- `batch_processing_size`: Tamaño de lotes procesados
- `worker_cpu_usage_percent`: Uso de CPU del worker
- `worker_memory_usage_bytes`: Uso de memoria

**Business Metrics**:
- `ticket_resolution_time_sec`: Tiempo promedio de resolución
- `tickets_per_agent{agent_id}`: Carga de trabajo por agente
- `sla_violations_total`: Tickets que excedieron SLA
- `customer_satisfaction_score`: CSAT por ticket resuelto

---

## Referencias

- Ver `docs/health.md` para endpoints de health check
- Ver `docs/rabbitmq-topology.md` para colas y eventos
- [Prometheus Best Practices](https://prometheus.io/docs/practices/naming/)
- [Grafana Dashboard Gallery](https://grafana.com/grafana/dashboards/)
- [RED Method (Rate, Errors, Duration)](https://www.weave.works/blog/the-red-method-key-metrics-for-microservices-architecture/)
