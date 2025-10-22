# 🔄 Interceptor de Eventos de Dominio → Outbox

## ✅ Implementación Completada

### 📦 Archivos Creados/Modificados

1. **OutboxMapper.cs** - Helper para mapeo de eventos
2. **TicketFlowDbContext.cs** - SaveChangesAsync interceptado

---

## 🛠️ OutboxMapper - Helper de Mapeo

### Ubicación
`Infrastructure/Persistence/EfCore/OutboxStore/OutboxMapper.cs`

### Características

#### 🔧 Opciones de Serialización JSON
```csharp
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
WriteIndented = false (compacto)
DefaultIgnoreCondition = WhenWritingNull (omite nulls)
PropertyNameCaseInsensitive = true
Encoder = UnsafeRelaxedJsonEscaping (Unicode sin escapar)
IncludeFields = true (serializa campos privados si es necesario)
```

#### 📋 Métodos Públicos

1. **ToOutboxMessage(IDomainEvent, string?)**
   - Convierte un evento de dominio a OutboxMessage
   - Parámetros:
     - `domainEvent`: Evento a serializar
     - `correlationId`: ID opcional para rastreo distribuido
   - Retorna: `OutboxMessage` listo para persistir

2. **ToOutboxMessages(IEnumerable<IDomainEvent>, string?)**
   - Convierte múltiples eventos con el mismo correlationId
   - Útil para batch processing

3. **FromOutboxMessage<TEvent>(OutboxMessage)**
   - Deserializa un OutboxMessage de vuelta a evento
   - Genérico tipado

4. **FromOutboxMessage(OutboxMessage, Type)**
   - Deserialización dinámica usando Type

5. **GenerateCorrelationId()**
   - Genera GUID en formato compacto (sin guiones)

### 🎯 Ejemplo de Uso

```csharp
// Mapear un solo evento
var ticketCreated = new TicketCreatedEvent { ... };
var outboxMsg = OutboxMapper.ToOutboxMessage(ticketCreated, "corr-123");

// Mapear múltiples eventos con mismo correlationId
var events = new[] { event1, event2, event3 };
var correlationId = OutboxMapper.GenerateCorrelationId();
var outboxMessages = OutboxMapper.ToOutboxMessages(events, correlationId);

// Deserializar de vuelta
var deserializedEvent = OutboxMapper.FromOutboxMessage<TicketCreatedEvent>(outboxMsg);
```

---

## 💾 SaveChangesAsync - Interceptor de Eventos

### Ubicación
`Infrastructure/Persistence/Persistence/TicketFlowDbContext.cs`

### Flujo de Ejecución

```
1. Detectar entidades con IHasDomainEvents
   ↓
2. Extraer eventos de dominio (DomainEvents)
   ↓
3. Limpiar eventos de las entidades (ClearDomainEvents)
   ↓
4. Generar CorrelationId único para el batch
   ↓
5. Mapear eventos → OutboxMessages (usando OutboxMapper)
   ↓
6. Agregar OutboxMessages al contexto (AddRangeAsync)
   ↓
7. Guardar TODO en UNA SOLA TRANSACCIÓN
```

### 🔒 Garantías Transaccionales

- ✅ **Atomicidad**: Entidades y OutboxMessages se guardan juntos
- ✅ **Consistencia**: Si falla el guardado, se hace rollback de todo
- ✅ **Aislamiento**: Los eventos se limpian antes del commit
- ✅ **Durabilidad**: Al hacer commit, todo está persistido

### 📝 Código Implementado

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 1. Detectar entidades con eventos
    var entitiesWithEvents = ChangeTracker
        .Entries<IHasDomainEvents>()
        .Where(entry => entry.Entity.DomainEvents.Any())
        .Select(entry => entry.Entity)
        .ToList();

    // 2. Extraer eventos
    var domainEvents = entitiesWithEvents
        .SelectMany(entity =>
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents(); // Limpiar
            return events;
        })
        .ToList();

    // 3. Si no hay eventos, guardar normalmente
    if (!domainEvents.Any())
        return await base.SaveChangesAsync(cancellationToken);

    // 4. Generar CorrelationId
    var correlationId = OutboxMapper.GenerateCorrelationId();

    // 5. Mapear a OutboxMessages
    var outboxMessages = OutboxMapper.ToOutboxMessages(domainEvents, correlationId);

    // 6. Agregar al contexto
    await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);

    // 7. Guardar en una transacción
    return await base.SaveChangesAsync(cancellationToken);
}
```

### ⚡ Versión Sincrónica

También se implementó `SaveChanges()` (sin async) con la misma lógica.

---

## 🧪 Escenario de Prueba

### Caso: Crear un Ticket con Eventos

```csharp
// 1. Crear ticket (genera evento de dominio)
var ticket = new Ticket
{
    Title = "Bug en login",
    Status = TicketStatus.Open
};

// 2. La entidad agrega internamente un evento
// ticket.RaiseDomainEvent(new TicketCreatedEvent(...))

// 3. Agregar al contexto
context.Tickets.Add(ticket);

// 4. Al guardar, el interceptor actúa automáticamente
await context.SaveChangesAsync();

// ✅ Resultado:
// - Ticket guardado en tabla Tickets
// - TicketCreatedEvent guardado en OutboxMessages
// - Ambos en la misma transacción
// - CorrelationId generado: "a1b2c3d4e5f6..."
```

### Verificación en DB

```sql
-- Ver el ticket
SELECT * FROM Tickets WHERE Title = 'Bug en login';

-- Ver el evento en Outbox (pendiente de despacho)
SELECT * FROM OutboxMessages 
WHERE DispatchedAt IS NULL 
  AND Type LIKE '%TicketCreated%';

-- Verificar correlationId
SELECT CorrelationId, COUNT(*) 
FROM OutboxMessages 
GROUP BY CorrelationId;
```

---

## 🎯 Ventajas de esta Implementación

✅ **Desacoplamiento**: Eventos se persisten sin llamar a servicios externos  
✅ **Confiabilidad**: Garantía transaccional (todo o nada)  
✅ **Rastreabilidad**: CorrelationId para debugging distribuido  
✅ **Flexibilidad**: Opciones de serialización configurables  
✅ **Reusabilidad**: OutboxMapper testable y reutilizable  
✅ **Performance**: AddRangeAsync para batch inserts  
✅ **Idempotencia**: Base para implementar ProcessedEvents  

---

## 📊 Estructura de Datos Generada

### OutboxMessage Ejemplo

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "TicketFlow.Domain.Events.TicketCreatedEvent",
  "payloadJson": "{\"ticketId\":\"...\",\"title\":\"Bug en login\",\"occurredAt\":\"2025-10-22T10:30:00Z\"}",
  "occurredAt": "2025-10-22T10:30:00Z",
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "dispatchedAt": null,
  "attempts": 0,
  "error": null
}
```

---

## 🚀 Próximos Pasos

1. ✅ **Crear migración**: `dotnet ef migrations add AddOutboxInterceptor`
2. ⏳ **Implementar Worker**: Background service que lee OutboxMessages
3. ⏳ **Despachar eventos**: Publicar a RabbitMQ/Kafka/etc.
4. ⏳ **Implementar reintentos**: Lógica para `Attempts` y `Error`
5. ⏳ **Agregar idempotencia**: Usar ProcessedEvents para evitar duplicados
