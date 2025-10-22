# 📦 Outbox Pattern - Configuración EF Core

## 📁 Archivos Creados

### Entidades (`Infrastructure/Outbox/`)

1. **OutboxMessage.cs**
   - Almacena eventos de dominio para procesamiento asíncrono
   - Propiedades:
     - `Id`: GUID único
     - `Type`: Tipo de evento (ej: "TicketCreated")
     - `PayloadJson`: Evento serializado en JSON
     - `OccurredAt`: Timestamp del evento
     - `CorrelationId`: ID para rastreo distribuido
     - `DispatchedAt`: NULL si pendiente, timestamp si despachado
     - `Attempts`: Contador de reintentos
     - `Error`: Último mensaje de error

2. **ProcessedEvent.cs**
   - Registro de eventos procesados (prevención de duplicados)
   - Propiedades:
     - `EventId`: ID del evento procesado (PK)
     - `ProcessedAt`: Timestamp de procesamiento

### Configuraciones EF Core (`Persistence/EfCore/OutboxStore/`)

1. **OutboxMessageConfiguration.cs**
   - Tabla: `OutboxMessages`
   - Índices optimizados:
     - `IX_OutboxMessages_Pending`: Buscar mensajes sin despachar
     - `IX_OutboxMessages_Type`: Filtrar por tipo de evento
     - `IX_OutboxMessages_CorrelationId`: Rastreo de flujos
     - `IX_OutboxMessages_DispatchedAt`: Auditoría de despachados

2. **ProcessedEventConfiguration.cs**
   - Tabla: `ProcessedEvents`
   - Índice:
     - `IX_ProcessedEvents_ProcessedAt`: Limpieza y auditoría

## 🔧 Integración con DbContext

Agregar en tu `AppDbContext`:

```csharp
public DbSet<OutboxMessage> OutboxMessages { get; set; }
public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
}
```

## 🚀 Próximos Pasos

1. ✅ Crear migración: `dotnet ef migrations add AddOutboxTables`
2. ✅ Aplicar migración: `dotnet ef database update`
3. ⏳ Implementar OutboxRepository
4. ⏳ Crear background worker para despachar mensajes
5. ⏳ Integrar con eventos de dominio

## 📊 Esquema de Base de Datos

### OutboxMessages
```sql
CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Type NVARCHAR(200) NOT NULL,
    PayloadJson TEXT NOT NULL,
    OccurredAt DATETIME2 NOT NULL,
    CorrelationId NVARCHAR(100),
    DispatchedAt DATETIME2,
    Attempts INT NOT NULL DEFAULT 0,
    Error NVARCHAR(2000)
);

-- Índice para mensajes pendientes
CREATE INDEX IX_OutboxMessages_Pending 
ON OutboxMessages(DispatchedAt, OccurredAt) 
WHERE DispatchedAt IS NULL;
```

### ProcessedEvents
```sql
CREATE TABLE ProcessedEvents (
    EventId UNIQUEIDENTIFIER PRIMARY KEY,
    ProcessedAt DATETIME2 NOT NULL
);

CREATE INDEX IX_ProcessedEvents_ProcessedAt 
ON ProcessedEvents(ProcessedAt);
```

