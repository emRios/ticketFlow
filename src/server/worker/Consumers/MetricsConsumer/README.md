# Consumer de Métricas

Este consumidor se encarga de recolectar métricas de negocio en tiempo real a partir de los eventos de dominio, alimentando dashboards y sistemas de alerta.

### Suscripción

Se suscribe a **todos los eventos** del exchange `tickets` usando el `routing key` de comodín `ticket.*`. Esto le permite tener una visión completa de la actividad del sistema.

### Funcionalidad

Al recibir cualquier evento, actualiza contadores en un sistema de monitoreo (ej. Prometheus, InfluxDB). Por ejemplo:
- Incrementa el contador `tickets_created_total`.
- Actualiza el gauge `tickets_in_progress_current`.

La lista completa de nombres de las métricas generadas, su tipo (counter, gauge) y sus etiquetas se encuentra documentada en: `docs/metrics.md`.
