# Procesadores de Eventos (Processors)

Los procesadores contienen la lógica de negocio específica para actuar sobre un evento recibido por un consumidor. Son el último eslabón de la cadena de consumo y realizan el "trabajo real".

### Contrato de Procesador

Todos los procesadores siguen un contrato simple y consistente definido por una interfaz (ej. `IProcessor`):

- **Entrada**: Reciben el `envelope` completo del evento. Esto incluye tanto el **payload** (los datos del evento, ej. `TicketCreated`) como los **metadatos** (headers como `x-event-id` y `x-correlation-id`).

- **Salida**: Devuelven un resultado de la operación (ej. `Success`, `Failure`). Opcionalmente, pueden generar sus propias métricas sobre el tiempo de ejecución o el resultado.

Esta abstracción permite que los consumidores (como `NotificationsConsumer`) no necesiten conocer los detalles de si una notificación se envía por email, SMS o simplemente se loguea en un entorno de desarrollo.
