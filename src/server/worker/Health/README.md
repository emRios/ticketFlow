# Monitoreo de Salud del Worker (Health)

Este módulo provee mecanismos para monitorear el estado y la actividad del worker, permitiendo a los sistemas de observabilidad y a los operadores verificar que está funcionando correctamente.

### Información Expuesta

Aunque el worker no exponga necesariamente un endpoint HTTP `/health`, **loguea periódicamente** (o bajo demanda) la siguiente información clave para demostrar que está operativo:

- **Heartbeat**: Un mensaje de log simple a intervalos regulares (ej. `[Health] Worker is alive and listening for messages.`).

- **Métricas de Consumo**: 
  - **Mensajes pendientes en cola**: El número de mensajes actualmente en la cola (si es accesible vía API de RabbitMQ).
  - **Último consumo exitoso**: El `timestamp` y el `eventId` del último mensaje procesado con éxito. Esto es crucial para detectar si el consumo se ha detenido o está "atascado".
