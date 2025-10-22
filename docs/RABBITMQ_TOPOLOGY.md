# 🐰 RabbitMQ - Topología y Publisher

## 📋 Componentes Implementados

### 1. **RabbitMqPublisher**
Publisher para enviar mensajes a RabbitMQ con soporte para exchanges y routing keys personalizados.

**Métodos principales:**
- `PublishAsync(eventType, payloadJson, correlationId)` - Publicación con routing key automático
- `Publish(exchange, routingKey, jsonPayload, correlationId, messageType)` - Publicación con control total

### 2. **RabbitTopologyBootstrapper**
Inicializador de topología que crea la estructura completa de exchanges, colas y bindings al arrancar el Worker.

---

## 🏗️ Topología de RabbitMQ

### Diagrama

```
                    ┌──────────────────────┐
                    │   Exchange: tickets  │
                    │   Type: Topic        │
                    │   Durable: true      │
                    └──────────┬───────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
        Routing Key: ticket.*         Routing Key: ticket.*
                │                             │
                ▼                             ▼
    ┌───────────────────────┐     ┌───────────────────────┐
    │  Queue: notifications │     │   Queue: metrics      │
    │  Durable: true        │     │   Durable: true       │
    │  Exclusive: false     │     │   Exclusive: false    │
    └───────────────────────┘     └───────────────────────┘
```

### Configuración Detallada

#### Exchange: `tickets`
```
Type: topic
Durable: true          (Sobrevive a reinicios del broker)
AutoDelete: false      (No se elimina automáticamente)
```

**Routing Keys soportadas:**
- `ticket.created` → Cuando se crea un ticket
- `ticket.updated` → Cuando se actualiza un ticket
- `ticket.status.changed` → Cambio de estado
- `ticket.assigned` → Asignación de ticket
- `ticket.deleted` → Eliminación de ticket
- `ticket.*` → Cualquier evento de ticket (wildcard)

---

## 🚀 Inicialización en Program.cs

```csharp
// 1. Configurar conexión
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqPort = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672);
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var rabbitMqExchange = builder.Configuration["RabbitMQ:Exchange"] ?? "tickets";

// 2. Registrar Publisher
builder.Services.AddSingleton<IMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RabbitMqPublisher>>();
    return new RabbitMqPublisher(logger, rabbitMqHost, rabbitMqExchange);
});

// 3. Inicializar topología al arrancar
var host = builder.Build();

var topologyBootstrapper = new RabbitTopologyBootstrapper(
    host.Services.GetRequiredService<ILogger<RabbitTopologyBootstrapper>>(),
    rabbitMqHost,
    rabbitMqPort,
    rabbitMqUsername,
    rabbitMqPassword
);

topologyBootstrapper.InitializeTopology();

host.Run();
```

### Logs Esperados

```log
[INFO] 🔧 Inicializando infraestructura...
[INFO] 🚀 Inicializando topología de RabbitMQ en localhost:5672...
[INFO] 📢 Exchange declarado: 'tickets' (Type: topic, Durable: true)
[INFO] 📥 Cola declarada: 'notifications' (Durable: true, Exclusive: false)
[INFO] 📥 Cola declarada: 'metrics' (Durable: true, Exclusive: false)
[INFO] 🔗 Binding creado: Cola 'notifications' ← Exchange 'tickets' (RoutingKey: 'ticket.*')
[INFO] 🔗 Binding creado: Cola 'metrics' ← Exchange 'tickets' (RoutingKey: 'ticket.*')
[INFO] ✅ Topología de RabbitMQ inicializada correctamente
```

---

## 📤 Uso del Publisher

### Método 1: PublishAsync (IMessagePublisher)

```csharp
await _messagePublisher.PublishAsync(
    eventType: "TicketFlow.Domain.Events.TicketCreatedEvent",
    payloadJson: "{\"ticketId\":\"123\",\"title\":\"Bug en login\"}",
    correlationId: "a1b2c3d4e5f6",
    cancellationToken
);
// Routing key generado: "ticket.created"
```

### Método 2: Publish (Control Total)

```csharp
_publisher.Publish(
    exchange: "tickets",
    routingKey: "ticket.created",
    jsonPayload: "{\"ticketId\":\"123\"}",
    correlationId: "xyz-789",
    messageType: "TicketCreatedEvent"
);
```

---

## 🔍 Verificación

### RabbitMQ Management UI
http://localhost:15672 (guest/guest)

### CLI
```bash
docker exec rabbitmq rabbitmqctl list_exchanges
docker exec rabbitmq rabbitmqctl list_queues
docker exec rabbitmq rabbitmqctl list_bindings
```

---

## ⚙️ Configuración (appsettings.json)

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "Exchange": "tickets"
  }
}
```
