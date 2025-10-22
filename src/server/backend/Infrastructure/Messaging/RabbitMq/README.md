# Topología de RabbitMQ para Tickets

Este documento describe la configuración de RabbitMQ para el `bounded context` de Tickets.

- **Exchange**: Se utiliza un único `topic exchange` llamado `tickets`. Esto permite un enrutamiento flexible basado en patrones de routing keys.
- **Bindings**: Las colas de los diferentes consumidores se vinculan al exchange usando routing keys que describen el evento de negocio. Ejemplos:
  - `ticket.created`
  - `ticket.status.changed`
  - `ticket.assigned`
- **Headers**: Cada mensaje incluye metadatos importantes para el rastreo y la idempotencia:
  - `x-event-id`: Identificador único del evento (para idempotencia).
  - `x-correlation-id`: ID para rastrear una solicitud a través de múltiples servicios.

Para una descripción más detallada y visual, referirse a `docs/rabbitmq-topology.md`.

### Diagrama de Topología

```ascii
(Mensaje)
     |
 routing_key: ticket.created
     |
     V
+-----------------------------+
|   Exchange: "tickets" (topic)   |
+-----------------------------+
     |
     |------------------------------------|
     | (rk: ticket.created)               | (rk: ticket.status.*)
     V                                    V
[Cola: Notificaciones]              [Cola: Métricas]
```
