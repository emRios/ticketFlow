# Consumer de Notificaciones

Este consumidor es responsable de procesar eventos de negocio para enviar notificaciones a los usuarios finales (ej. por email).

### Suscripciones

Se suscribe a los siguientes `routing keys` del exchange `tickets`:
- `ticket.created`
- `ticket.status.changed`
- `ticket.assigned`

### Lógica de Procesamiento

Al recibir un evento, este consumidor no contiene la lógica de envío directamente. En su lugar, **delega la acción** a un `IProcessor` específico (como `EmailProcessor` o `MockProcessor` para entornos de prueba), desacoplando el consumo del mensaje de la acción de notificación.

### Gestión de Errores

Implementa una política de reintentos robusta con una estrategia de **backoff exponencial** para gestionar fallos transitorios (ej. el servicio de email no responde).

- **Tiempos de espera**: 1 minuto, 5 minutos, 15 minutos.
- **DLQ (Dead Letter Queue)**: Si todos los reintentos fallan, el mensaje se envía a una cola de mensajes muertos (DLQ) para su análisis manual, asegurando que ningún evento se pierda.
