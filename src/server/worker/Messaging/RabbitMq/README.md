# Módulo de Mensajería (RabbitMQ)

Este módulo encapsula toda la configuración y la lógica para la comunicación con RabbitMQ, proveyendo una abstracción sobre la cual se construyen los consumidores.

### Características Clave

- **Gestión de Conexión**: Maneja el ciclo de vida de la conexión y los canales con el broker, incluyendo reconexiones automáticas.

- **QoS (Quality of Service)**: Configura un `prefetch count` (ej. 10 mensajes a la vez). Esto evita que un solo worker acapare todos los mensajes de la cola y permite un balanceo de carga más equitativo cuando hay múltiples instancias del worker.

- **Deduplicación (Idempotencia)**: Antes de entregar un mensaje a un consumidor, utiliza el `x-event-id` de los headers para verificar si el evento ya ha sido procesado. Esto previene el procesamiento duplicado en caso de que RabbitMQ re-entregue un mensaje.

- **Lógica de Retry y DLQ**: Orquesta la lógica de reintentos y el envío final a una Dead Letter Queue (DLQ) si el procesamiento del consumidor falla de manera persistente.
