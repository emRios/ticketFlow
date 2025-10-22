# Persistencia del Outbox (Outbox Store)

Esta capa es responsable de la persistencia de los mensajes del Outbox usando Entity Framework Core.

### Esquema Conceptual

El esquema se basa en dos tablas principales para gestionar el estado de los eventos y garantizar el procesamiento "at-least-once".

1.  **`Outbox`**: Almacena los eventos de dominio que deben ser publicados.
2.  **`ProcessedEvents`**: Funciona como un registro de idempotencia para los consumidores, guardando los eventos que ya han sido procesados por un consumidor específico.

### Responsabilidades del Store

- **Leer**: Proveer métodos para que el Dispatcher lea los mensajes no procesados de la tabla `Outbox`.
- **Marcar**: Marcar mensajes como "procesados" o incrementar su contador de reintentos (`Attempts`).
- **Retry**: Implementar la lógica para seleccionar eventos que necesitan un reintento basado en su estado y número de intentos.

### Diagrama de Tablas

```ascii
+------------------+      +---------------------+
|   Outbox         |      |   ProcessedEvents   |
+------------------+      +---------------------+
| EventId (PK)     |      | EventId (PK, FK)    |
| EventType        |      | ConsumerId (PK)     |
| EventPayload     |      | ProcessedAt         |
| OccurredAt       |      +---------------------+
| IsProcessed      |
| Attempts         |
+------------------+
```
