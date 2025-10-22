# Heurística de Auto-Asignación (MVP)

Estrategia para asignar automáticamente tickets a agentes disponibles, balanceando carga de trabajo y rotando equitativamente.

---

## Objetivo

**Balancear carga de trabajo** entre agentes activos y **rotar asignaciones** para evitar que siempre los mismos agentes reciban tickets nuevos.

### Principios
1. **Equidad**: Distribuir tickets de forma justa
2. **Carga equilibrada**: Priorizar agentes con menos trabajo activo
3. **Determinismo**: Desempates predecibles y auditables
4. **Resiliencia**: Manejo de concurrencia y reintentos

---

## Fórmula de Score

### Cálculo de Carga

Cada agente recibe un **score de carga** basado en tickets activos:

```
score = openCount + (inProgressCount * 1.5)
```

**Donde**:
- `openCount`: Tickets asignados en estado `NEW` o `ON_HOLD`
- `inProgressCount`: Tickets asignados en estado `IN_PROGRESS` (peso mayor)

**Justificación de pesos**:
- Tickets `IN_PROGRESS` requieren más atención inmediata → peso 1.5
- Tickets `NEW` o `ON_HOLD` pueden esperar → peso 1.0
- Tickets `RESOLVED` no cuentan (ya cerrados)

### Ejemplos de Score

| Agente | NEW | IN_PROGRESS | ON_HOLD | Score | Cálculo |
|--------|-----|-------------|---------|-------|---------|
| Ana    | 2   | 3           | 1       | **7.5** | 3 + (3 × 1.5) |
| Luis   | 1   | 2           | 0       | **4.0** | 1 + (2 × 1.5) |
| María  | 0   | 1           | 2       | **3.5** | 2 + (1 × 1.5) |
| Carlos | 0   | 0           | 0       | **0.0** | 0 + (0 × 1.5) |

**Orden de asignación**: Carlos (0.0) → María (3.5) → Luis (4.0) → Ana (7.5)

---

## Desempates

Si múltiples agentes tienen el **mismo score**, aplicar criterios de desempate en orden:

### 1. `lastAssignedAt` más antiguo
**Regla**: Priorizar agente que recibió su última asignación hace más tiempo.

**Ejemplo**:
```
Ana:    score=2.0, lastAssignedAt=2025-10-20T10:00:00Z
Luis:   score=2.0, lastAssignedAt=2025-10-20T14:00:00Z
María:  score=2.0, lastAssignedAt=2025-10-21T08:00:00Z

→ Orden: Ana (más antiguo) → Luis → María
```

**Justificación**: Rotación justa, evita que siempre los mismos agentes reciban tickets.

### 2. `AgentId` ascendente (determinismo)
**Regla**: Si `lastAssignedAt` también es igual (o null), ordenar por ID de agente alfabéticamente.

**Ejemplo**:
```
Agente u123: score=0, lastAssignedAt=null
Agente u456: score=0, lastAssignedAt=null

→ Orden: u123 (ID menor) → u456
```

**Justificación**: Garantiza comportamiento determinista en tests y auditorías.

---

## Flujo de Auto-Asignación

### Paso 1: Filtrar Agentes Activos

Consultar base de datos para obtener candidatos elegibles:

```sql
SELECT Id, Name
FROM Users
WHERE Role = 'AGENT'
  AND IsActive = true
```

**Validación**:
- Si no hay agentes activos → Fallar con `NO_AGENTS_AVAILABLE`
- Continuar con candidatos encontrados

---

### Paso 2: Calcular Score y Ordenar

Para cada agente candidato:

1. **Contar tickets por estado**:
```sql
SELECT 
  AssignedTo,
  SUM(CASE WHEN Status IN ('NEW', 'ON_HOLD') THEN 1 ELSE 0 END) as openCount,
  SUM(CASE WHEN Status = 'IN_PROGRESS' THEN 1 ELSE 0 END) as inProgressCount
FROM Tickets
WHERE AssignedTo IN (/* lista de agentes activos */)
  AND Status != 'RESOLVED'
GROUP BY AssignedTo
```

2. **Calcular score**: `openCount + (inProgressCount * 1.5)`

3. **Obtener `lastAssignedAt`**:
```sql
SELECT 
  ActorId, 
  MAX(At) as lastAssignedAt
FROM AuditLogs
WHERE Action = 'assigned'
  AND ActorId IN (/* lista de agentes */)
GROUP BY ActorId
```

4. **Ordenar candidatos**:
```
ORDER BY 
  score ASC,
  lastAssignedAt ASC NULLS FIRST,
  AgentId ASC
```

---

### Paso 3: Seleccionar Primer Candidato

Tomar el primer agente de la lista ordenada:

```csharp
// Pseudocódigo
var selectedAgent = orderedCandidates.First();
```

**Validación**:
- Si lista está vacía → Fallar con `NO_AGENTS_AVAILABLE`

---

### Paso 4: Persistir Asignación

Ejecutar en **transacción atómica**:

```sql
BEGIN TRANSACTION;

-- 1. Actualizar ticket
UPDATE Tickets
SET AssignedTo = @selectedAgentId,
    UpdatedAt = @now
WHERE Id = @ticketId
  AND AssignedTo IS NULL;  -- Optimistic lock

-- 2. Insertar AuditLog
INSERT INTO AuditLogs (Id, ActorId, EntityType, EntityId, Action, BeforeJson, AfterJson, At, CorrelationId)
VALUES (@logId, 'SYSTEM', 'Ticket', @ticketId, 'assigned', '{"assignedTo":null}', '{"assignedTo":{"id":"u123","name":"Ana"}}', @now, @correlationId);

-- 3. Insertar Outbox (evento ticket.assigned)
INSERT INTO Outbox (Id, Type, PayloadJson, OccurredAt, CorrelationId, DispatchedAt, Attempts)
VALUES (@eventId, 'ticket.assigned', @payload, @now, @correlationId, NULL, 0);

COMMIT;
```

**Manejo de concurrencia**:
- Si `UPDATE` afecta 0 filas → Ticket ya fue asignado por otro proceso
- Lanzar excepción `ConcurrencyException`
- Reintentar flujo completo (ver Paso 5)

---

### Paso 5: Reintentar en Concurrencia

Si la transacción falla por conflicto de concurrencia:

1. **Detectar conflicto**:
```csharp
catch (ConcurrencyException ex) {
  attempts++;
  if (attempts > MAX_RETRIES) {
    throw new AssignmentFailedException("Max retries exceeded");
  }
}
```

2. **Esperar con backoff exponencial**:
```
Intento 1: Esperar 100ms
Intento 2: Esperar 200ms
```

3. **Reintentar desde Paso 1** (recalcular scores con datos actualizados)

**Máximo de reintentos**: 2 intentos adicionales (3 intentos totales)

**Justificación**:
- Evita loops infinitos en alta concurrencia
- 3 intentos son suficientes para casos normales
- Si falla, publicar `ticket.assignment.failed` para reintento asíncrono

---

## Observabilidad

### Métricas de Monitoreo

#### 1. `agent_load_score`
**Tipo**: Gauge  
**Labels**: `agent_id`, `agent_name`  
**Descripción**: Score actual de carga de cada agente

**Uso**:
```
agent_load_score{agent_id="u123", agent_name="Ana"} = 7.5
agent_load_score{agent_id="u456", agent_name="Luis"} = 4.0
```

**Alertas**:
- Si algún agente tiene score > 15 → Posible sobrecarga
- Si diferencia entre max y min es > 10 → Desbalance en equipo

---

#### 2. `assign_attempts_total`
**Tipo**: Counter  
**Labels**: `result` (success|retry|failure)  
**Descripción**: Total de intentos de asignación

**Ejemplo**:
```
assign_attempts_total{result="success"} = 1523
assign_attempts_total{result="retry"} = 89
assign_attempts_total{result="failure"} = 3
```

**Alertas**:
- Si `retry` > 10% de `success` → Alta concurrencia
- Si `failure` > 0 → Investigar logs de error

---

#### 3. `assign_conflicts_total`
**Tipo**: Counter  
**Labels**: `ticket_id`  
**Descripción**: Número de conflictos de concurrencia detectados

**Uso**:
```
assign_conflicts_total{ticket_id="TF-1024"} = 2
```

**Alertas**:
- Si `assign_conflicts_total` crece rápidamente → Revisar lógica de locks

---

### Logs Estructurados

Cada intento de asignación debe loguear:

```json
{
  "timestamp": "2025-10-21T10:30:00Z",
  "level": "INFO",
  "event": "assignment_attempt",
  "correlationId": "corr-abc-123",
  "ticketId": "TF-1024",
  "ticketCode": "TF-1024",
  "selectedAgent": {
    "id": "u123",
    "name": "Ana García",
    "score": 7.5,
    "openCount": 3,
    "inProgressCount": 3,
    "lastAssignedAt": "2025-10-20T10:00:00Z"
  },
  "candidatesCount": 5,
  "attempts": 1,
  "result": "success"
}
```

---

## Riesgos y Mitigaciones

### 1. Starvation (Inanición)

**Riesgo**: Un agente con alta carga nunca recibe nuevos tickets (siempre pierde en ordenamiento).

**Mitigación**:
- ✅ Desempate por `lastAssignedAt` garantiza rotación
- ✅ Cuando agente resuelve tickets, su score baja y vuelve a rotar
- ✅ Monitorear métrica `agent_load_score` para detectar desbalances

**Ejemplo**:
```
T0: Ana=5.0, Luis=3.0, María=3.0  → Asigna a Luis (lastAssignedAt más antiguo)
T1: Ana=5.0, Luis=4.5, María=3.0  → Asigna a María
T2: Ana=5.0, Luis=4.5, María=4.5  → Asigna a Ana (lastAssignedAt más antiguo)
```

---

### 2. Picos de Carga (Traffic Spikes)

**Riesgo**: Múltiples tickets creados simultáneamente saturan a todos los agentes.

**Mitigación**:
- ✅ Monitorizar métrica de backlog (tickets sin asignar)
- ✅ Alertar cuando backlog > 20 tickets
- ✅ Escalar equipo agregando agentes (cambiar `IsActive=true`)
- ✅ Implementar throttling en API (rate limiting)

**Alerta sugerida**:
```
IF COUNT(Tickets WHERE AssignedTo IS NULL) > 20
THEN notify("High unassigned backlog")
```

---

### 3. Concurrencia Alta

**Riesgo**: Múltiples procesos intentan asignar el mismo ticket simultáneamente.

**Mitigación**:
- ✅ Optimistic locking en UPDATE (`WHERE AssignedTo IS NULL`)
- ✅ Reintentos con backoff exponencial (máx 2)
- ✅ Métrica `assign_conflicts_total` para monitorear frecuencia
- ✅ Si conflictos > 10% → Considerar locks distribuidos (Redis)

---

### 4. Agentes Inactivos Durante Asignación

**Riesgo**: Agente marcado `IsActive=true` pero realmente offline (no responde).

**Mitigación** (fuera del MVP):
- Implementar heartbeat de agentes (ping cada 5 minutos)
- Marcar `IsActive=false` si no responde en 10 minutos
- Re-asignar tickets automáticamente si agente no responde en 1 hora

---

## Extensiones Futuras

### 1. Prioridad por Habilidades (Skills)
Asignar tickets según tags y especialización del agente:

```
IF ticket.tags CONTAINS "pagos":
  → Filtrar agentes con skill="pagos"
  → Aplicar heurística de score
```

### 2. Horarios de Trabajo (Shifts)
Considerar zona horaria y horario laboral del agente:

```
IF agent.timezone == "America/Mexico_City" AND currentTime == 03:00 UTC:
  → Excluir agente (fuera de horario)
```

### 3. Política por Prioridad
Tickets `URGENT` siempre al agente con menor carga, ignorando rotación:

```
IF ticket.priority == "URGENT":
  → ORDER BY score ASC
  → Ignorar lastAssignedAt
```

### 4. Round-Robin Puro
Alternar entre agentes sin considerar carga (más simple):

```
SELECT Id FROM Users
WHERE Role = 'AGENT' AND IsActive = true
ORDER BY lastAssignedAt ASC NULLS FIRST, Id ASC
LIMIT 1
```

---

## Pseudocódigo de Implementación

```csharp
// Application/Services/AutoAssignmentService.cs

public async Task<Result> AssignTicketAsync(string ticketId, int maxRetries = 2) {
  int attempts = 0;
  
  while (attempts <= maxRetries) {
    try {
      // Paso 1: Filtrar agentes activos
      var agents = await _userRepo.GetActiveAgentsAsync();
      if (!agents.Any()) {
        return Result.Fail("NO_AGENTS_AVAILABLE");
      }
      
      // Paso 2: Calcular scores y ordenar
      var scores = await _ticketRepo.CalculateAgentScoresAsync(agents);
      var ordered = scores
        .OrderBy(s => s.Score)
        .ThenBy(s => s.LastAssignedAt ?? DateTime.MinValue)
        .ThenBy(s => s.AgentId)
        .ToList();
      
      // Paso 3: Seleccionar primer candidato
      var selected = ordered.First();
      
      // Paso 4: Persistir en transacción
      await _unitOfWork.BeginTransactionAsync();
      
      var affected = await _ticketRepo.AssignTicketAsync(
        ticketId, 
        selected.AgentId, 
        optimisticLock: true // WHERE AssignedTo IS NULL
      );
      
      if (affected == 0) {
        throw new ConcurrencyException("Ticket already assigned");
      }
      
      await _auditRepo.LogAsync(new AuditLog {
        ActorId = "SYSTEM",
        Action = "assigned",
        EntityId = ticketId,
        AfterJson = $"{{\"assignedTo\":\"{selected.AgentId}\"}}"
      });
      
      await _outboxRepo.PublishAsync(new OutboxEvent {
        Type = "ticket.assigned",
        Payload = BuildPayload(ticketId, selected)
      });
      
      await _unitOfWork.CommitAsync();
      
      // Métricas
      _metrics.IncrementCounter("assign_attempts_total", ("result", "success"));
      
      return Result.Ok(selected.AgentId);
      
    } catch (ConcurrencyException) {
      attempts++;
      _metrics.IncrementCounter("assign_attempts_total", ("result", "retry"));
      _metrics.IncrementCounter("assign_conflicts_total", ("ticket_id", ticketId));
      
      if (attempts > maxRetries) {
        _metrics.IncrementCounter("assign_attempts_total", ("result", "failure"));
        return Result.Fail("MAX_RETRIES_EXCEEDED");
      }
      
      await Task.Delay(100 * attempts); // Backoff exponencial
    }
  }
}
```

---

## Testing

### Casos de Prueba Esenciales

1. **Score básico**: 3 agentes con diferente carga → Asigna al de menor score
2. **Desempate por lastAssignedAt**: 2 agentes con mismo score → Asigna al más antiguo
3. **Desempate por AgentId**: 2 agentes sin lastAssignedAt → Asigna al ID menor
4. **Sin agentes activos**: IsActive=false para todos → Retorna NO_AGENTS_AVAILABLE
5. **Concurrencia**: 2 procesos asignan mismo ticket → Uno falla y reintenta exitosamente
6. **Máx reintentos**: Conflicto persiste después de 3 intentos → Retorna MAX_RETRIES_EXCEEDED
7. **Transacción atómica**: Falla en Outbox → Rollback completo (ticket no asignado)
8. **Rotación**: Crear 5 tickets consecutivos → Verificar que rotan entre todos los agentes

---

## Referencias

- Ver `docs/state-machine.md` para flujo de estados del ticket
- Ver `docs/data-model.md` para estructura de tablas Users y Tickets
- Ver `contracts/README.md` para formato del evento `ticket.assigned`
- [Load Balancing Algorithms](https://en.wikipedia.org/wiki/Load_balancing_(computing))
- [Optimistic Concurrency Control](https://en.wikipedia.org/wiki/Optimistic_concurrency_control)
