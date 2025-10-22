# ADR-0003: Máquina de Estados del Ticket

**Fecha**: 2025-10-21  
**Estado**: Aceptado  
**Contexto**: MVP de TicketFlow

---

## Contexto

Los tickets pasan por múltiples estados durante su ciclo de vida (nuevo, en proceso, en espera, resuelto). Necesitamos definir qué transiciones de estado son válidas, quién puede ejecutarlas y cómo validar reglas de negocio (ej: no se puede resolver un ticket sin asignación). Las opciones son: (1) FSM (Finite State Machine) explícita con matriz de transiciones, (2) Validaciones ad-hoc en cada endpoint, o (3) Event Sourcing con agregados de dominio.

---

## Decisión

Adoptamos una **Máquina de Estados Finitos (FSM) explícita** implementada en el dominio con el patrón State + Validator:

### Estados y Transiciones Válidas
```
NEW → IN_PROGRESS
IN_PROGRESS ↔ ON_HOLD
IN_PROGRESS → RESOLVED
RESOLVED = terminal (no retorno)
```

### Reglas de Negocio
1. **Estado terminal**: `RESOLVED` no permite transiciones a otros estados (inmutabilidad)
2. **Asignación obligatoria**: Solo se puede transicionar a `RESOLVED` si `AssignedTo != NULL`
3. **Auditoría**: Cada transición registra `AuditLog` con snapshot antes/después
4. **Eventos**: Cada transición publica `ticket.status.changed` vía Outbox

### Validación Centralizada
La lógica de validación reside en:
- **Domain/Services/StatusTransitionValidator.cs**: Matriz de transiciones válidas
- **Domain/Specifications/CanResolveTicketSpec.cs**: Regla de `AssignedTo != NULL`
- **Application/UseCases/ChangeTicketStatusUseCase.cs**: Orquestación de validaciones

---

## Consecuencias

### Positivas
- **Integridad de datos**: Imposible crear estados inconsistentes (ej: ticket resuelto sin asignación)
- **Comportamiento predecible**: La matriz de transiciones es autodocumentada y testeable
- **Auditoría completa**: Cada cambio de estado queda registrado con actor, timestamp y correlationId
- **Debugging simplificado**: Los logs de `AuditLogs` permiten reconstruir el historial completo
- **Extensibilidad**: Agregar nuevos estados (ej: `CANCELLED`, `REOPENED`) solo requiere actualizar la matriz
- **Testing robusto**: Tests unitarios verifican todas las transiciones válidas/inválidas
- **Errores claros**: Códigos HTTP específicos (409 Conflict para transición inválida, 422 para regla violada)

### Negativas
- **Rigidez inicial**: Cambios en la FSM requieren migraciones de datos y deploys coordinados
- **Complejidad de código**: Estado distribuido entre entidad, validator, specifications y use case
- **No soporta flujos complejos**: Workflows con ramas condicionales (if-then-else) requieren FSM con guardas
- **Overhead de validación**: Cada transición ejecuta 2-3 validaciones (matriz + reglas + permisos)

---

## Alternativas Consideradas

### 1. Validaciones ad-hoc en controladores
**Pros**: Simple, menos código  
**Contras**: Lógica dispersa, reglas duplicadas, difícil de testear, propenso a bugs

**Razón de rechazo**: La lógica de negocio debe estar en el dominio, no en la capa de API. Las validaciones ad-hoc no escalan cuando hay múltiples clientes (API REST, gRPC, CLI).

### 2. Event Sourcing con agregados
**Pros**: Historial completo por diseño, replay de eventos  
**Contras**: Complejidad alta, requiere CQRS, proyecciones, event store, curva de aprendizaje

**Razón de rechazo**: Event Sourcing es overkill para MVP. Los `AuditLogs` ya proveen historial suficiente sin la complejidad de un event store. Si en el futuro necesitamos replay, podemos migrar.

### 3. Workflow Engine (Elsa, Temporal)
**Pros**: Workflows visuales, ramas condicionales, paralelismo  
**Contras**: Dependencia externa pesada, overhead operacional, abstracción innecesaria para FSM simple

**Razón de rechazo**: Nuestro flujo es una FSM lineal con 4 estados. Un workflow engine agrega complejidad sin beneficios. Si en el futuro necesitamos aprobaciones multi-nivel o pasos paralelos, lo reconsideraremos.

### 4. Estado implícito (sin FSM)
**Pros**: Máxima flexibilidad, cualquier transición permitida  
**Contras**: Caos total, tickets en estados imposibles, debugging imposible

**Razón de rechazo**: La flexibilidad sin restricciones lleva a inconsistencias. Los constraints explícitos (FSM) son feature, no bug.

### 5. Database triggers para validación
**Pros**: Validación garantizada a nivel de DB  
**Contras**: Lógica de negocio en SQL, difícil de testear, no portable

**Razón de rechazo**: Preferimos lógica de negocio en código (C#) testeable y portable. Los triggers son útiles para constraints técnicos (FK, unique), no para reglas de dominio complejas.

---

## Implementación

### Domain Layer
```csharp
// Domain/Entities/Ticket.cs
public class Ticket {
  public TicketStatus Status { get; private set; }
  
  public Result TransitionTo(TicketStatus newStatus, User actor) {
    // Validar transición permitida
    if (!StatusTransitionValidator.IsValid(Status, newStatus)) {
      return Result.Fail($"Invalid transition: {Status} → {newStatus}");
    }
    
    // Validar reglas de negocio
    if (newStatus == TicketStatus.RESOLVED && !CanResolveSpecification.IsSatisfiedBy(this)) {
      return Result.Fail("Cannot resolve ticket without assignee");
    }
    
    // Aplicar cambio
    var oldStatus = Status;
    Status = newStatus;
    UpdatedAt = DateTime.UtcNow;
    
    if (newStatus == TicketStatus.RESOLVED) {
      ClosedAt = DateTime.UtcNow;
    }
    
    // Emitir evento de dominio (será convertido a evento de integración)
    RaiseDomainEvent(new TicketStatusChangedEvent(Id, oldStatus, newStatus, actor));
    
    return Result.Ok();
  }
}

// Domain/Services/StatusTransitionValidator.cs
public static class StatusTransitionValidator {
  private static readonly Dictionary<TicketStatus, List<TicketStatus>> AllowedTransitions = new() {
    { TicketStatus.NEW, new List<TicketStatus> { TicketStatus.IN_PROGRESS } },
    { TicketStatus.IN_PROGRESS, new List<TicketStatus> { TicketStatus.ON_HOLD, TicketStatus.RESOLVED } },
    { TicketStatus.ON_HOLD, new List<TicketStatus> { TicketStatus.IN_PROGRESS } },
    { TicketStatus.RESOLVED, new List<TicketStatus>() } // Estado terminal
  };
  
  public static bool IsValid(TicketStatus from, TicketStatus to) {
    return AllowedTransitions[from].Contains(to);
  }
  
  public static List<TicketStatus> GetAllowed(TicketStatus current) {
    return AllowedTransitions[current];
  }
}
```

### Application Layer
```csharp
// Application/UseCases/ChangeTicketStatusUseCase.cs
public class ChangeTicketStatusUseCase {
  public async Task<Result> ExecuteAsync(string ticketId, TicketStatus newStatus, string actorId) {
    // 1. Cargar ticket
    var ticket = await _ticketRepo.GetByIdAsync(ticketId);
    if (ticket == null) return Result.Fail("NOT_FOUND");
    
    // 2. Cargar actor
    var actor = await _userRepo.GetByIdAsync(actorId);
    
    // 3. Validar permisos (solo ADMIN/AGENT pueden cambiar status)
    if (!actor.IsAdminOrAgent()) return Result.Fail("FORBIDDEN");
    
    // 4. Intentar transición (validación en dominio)
    var result = ticket.TransitionTo(newStatus, actor);
    if (result.IsFailure) return result;
    
    // 5. Persistir cambios
    await _ticketRepo.UpdateAsync(ticket);
    
    // 6. Registrar audit log
    await _auditRepo.LogAsync(new AuditLog {
      ActorId = actorId,
      EntityType = "Ticket",
      EntityId = ticketId,
      Action = "status_changed",
      BeforeJson = JsonSerializer.Serialize(new { status = ticket.Status }),
      AfterJson = JsonSerializer.Serialize(new { status = newStatus })
    });
    
    // 7. Publicar evento vía Outbox
    await _outboxRepo.InsertAsync(new OutboxEvent {
      Type = "ticket.status.changed",
      PayloadJson = BuildEventPayload(ticket, actor)
    });
    
    return Result.Ok();
  }
}
```

---

## Testing

### Tests Unitarios (Domain)
```csharp
[Fact]
public void TransitionTo_ValidTransition_Succeeds() {
  var ticket = new Ticket { Status = TicketStatus.NEW };
  var result = ticket.TransitionTo(TicketStatus.IN_PROGRESS, admin);
  result.IsSuccess.Should().BeTrue();
  ticket.Status.Should().Be(TicketStatus.IN_PROGRESS);
}

[Fact]
public void TransitionTo_InvalidTransition_Fails() {
  var ticket = new Ticket { Status = TicketStatus.RESOLVED };
  var result = ticket.TransitionTo(TicketStatus.IN_PROGRESS, admin);
  result.IsFailure.Should().BeTrue();
  result.Error.Should().Contain("Invalid transition");
}

[Fact]
public void TransitionTo_ResolveWithoutAssignee_Fails() {
  var ticket = new Ticket { Status = TicketStatus.IN_PROGRESS, AssignedTo = null };
  var result = ticket.TransitionTo(TicketStatus.RESOLVED, admin);
  result.IsFailure.Should().BeTrue();
  result.Error.Should().Contain("without assignee");
}
```

### Tests de Integración (API)
```csharp
[Fact]
public async Task ChangeStatus_ValidTransition_Returns200() {
  var response = await _client.PatchAsync("/tickets/TF-1024/status", 
    new { status = "IN_PROGRESS" });
  response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task ChangeStatus_InvalidTransition_Returns409() {
  // Crear ticket resuelto
  var ticket = await CreateResolvedTicket();
  
  // Intentar reabrir
  var response = await _client.PatchAsync($"/tickets/{ticket.Code}/status", 
    new { status = "IN_PROGRESS" });
  
  response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  var error = await response.Content.ReadAsAsync<Error>();
  error.Code.Should().Be("INVALID_TRANSITION");
}
```

---

## Extensiones Futuras

### Estado CANCELLED
Permitir cancelar tickets desde `NEW` o `IN_PROGRESS`:
```csharp
AllowedTransitions[TicketStatus.NEW].Add(TicketStatus.CANCELLED);
AllowedTransitions[TicketStatus.IN_PROGRESS].Add(TicketStatus.CANCELLED);
```

### Reapertura Controlada
Permitir `RESOLVED` → `REOPENED` con restricciones:
- Solo si resolución fue en últimos 7 días
- Solo por ADMIN o el agente original
- Requiere motivo de reapertura

### Sub-estados
Agregar granularidad sin complejidad:
- `IN_PROGRESS.INVESTIGATING`
- `IN_PROGRESS.WAITING_CLIENT`
- `IN_PROGRESS.ESCALATED`

Implementar con campo adicional `SubStatus` sin cambiar la FSM principal.

---

## Referencias

- [State Pattern - Gang of Four](https://refactoring.guru/design-patterns/state)
- [Finite State Machine](https://en.wikipedia.org/wiki/Finite-state_machine)
- Ver `docs/state-machine.md` para especificación detallada
- Ver `docs/data-model.md` para estructura de AuditLogs
