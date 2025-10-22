# Flujo del Patrón Outbox

Este proceso garantiza la entrega confiable de eventos de dominio a un bus de mensajes (message bus) utilizando el patrón Outbox. El flujo principal es unidireccional y está diseñado para ser resiliente.

- **Flujo**: Un evento de dominio generado en la lógica de negocio se guarda como una fila en la tabla `Outbox` dentro de la misma transacción de la base de datos. Un proceso en segundo plano (Dispatcher) lee esta tabla y envía los eventos a RabbitMQ.
- **Idempotencia**: Se garantiza a través de un `eventId` único. Los consumidores pueden rastrear los eventos que ya han procesado para evitar duplicados.
- **Reintentos**: El dispatcher gestiona los reintentos (`Attempts`) en caso de fallo en la comunicación con el broker, aplicando una estrategia de `backoff` para no sobrecargar los recursos.

### Diagrama de Flujo

```ascii
(Genera)
[Domain Event] -> (Guarda en la misma TX) -> [Tabla Outbox]
                                                 |
                                                 |
(Lee periódicamente)                             |
[Background Dispatcher] -------------------------+
         |
         | (Publica en Broker)
         V
    [RabbitMQ]
```
