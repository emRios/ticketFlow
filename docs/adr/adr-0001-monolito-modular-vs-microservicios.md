# ADR-0001: Monolito Modular vs Microservicios

**Fecha**: 2025-10-21  
**Estado**: Aceptado  
**Contexto**: MVP de TicketFlow

---

## Contexto

En la fase MVP de TicketFlow, necesitamos definir la arquitectura del sistema para gestionar tickets con tablero Kanban, notificaciones y métricas. El equipo es pequeño (2-3 desarrolladores) y el tiempo de desarrollo debe ser corto (8-12 semanas). Las opciones principales son: (1) Monolito modular con Clean Architecture, o (2) Microservicios distribuidos desde el inicio.

---

## Decisión

**Adoptamos un monolito modular** con separación clara de responsabilidades mediante Clean Architecture (DDD + Hexagonal). El backend API y el Worker de mensajería correrán como procesos independientes, pero ambos compartirán el mismo código base (monorepo) y base de datos. Esta arquitectura permite:

- **API Backend**: Expone endpoints HTTP para CRUD de tickets, autenticación y consultas
- **Worker**: Consume eventos de RabbitMQ para procesar notificaciones y métricas de forma asíncrona
- **Shared Domain**: Entidades, Value Objects y Domain Services compartidos entre API y Worker

Ambos servicios se comunican mediante RabbitMQ usando el patrón Transactional Outbox para garantizar consistencia eventual.

---

## Consecuencias

### Positivas
- **Desarrollo rápido**: Un solo codebase reduce complejidad de coordinación
- **Despliegue simple**: Dos contenedores Docker (`api`, `worker`) más fáciles de orquestar
- **Debugging eficiente**: Errores más fáciles de rastrear sin llamadas HTTP inter-servicios
- **Transacciones ACID**: Operaciones críticas (crear ticket + outbox) en misma transacción de DB
- **Refactoring seguro**: Cambios en el dominio se propagan automáticamente a ambos procesos
- **Testing integrado**: Tests de integración más simples sin mocks de red

### Negativas
- **Escalabilidad acoplada**: API y Worker escalan juntos (mitigado: procesos separados permiten escalado independiente con múltiples instancias)
- **Deployments coordinados**: Ambos servicios deben desplegarse juntos si hay cambios en el dominio compartido
- **Riesgo de acoplamiento**: Sin disciplina, los módulos pueden volverse interdependientes (mitigado: Clean Architecture con interfaces y DDD)

---

## Alternativas Consideradas

### 1. Microservicios desde el inicio
**Pros**: Escalabilidad independiente, tecnologías heterogéneas, boundaries claros  
**Contras**: Complejidad operacional (Kubernetes, service mesh), debugging distribuido, consistencia eventual compleja, overhead de red, tiempo de desarrollo 2-3x mayor

**Razón de rechazo**: Sobrecarga de infraestructura innecesaria para MVP con < 1000 usuarios concurrentes. La complejidad no justifica los beneficios en esta fase.

### 2. Monolito puro (API con background jobs)
**Pros**: Simplicidad máxima, un solo proceso  
**Contras**: Consumo de mensajes bloquea threads HTTP, reinicio del proceso afecta consumo de cola, mezcla de responsabilidades

**Razón de rechazo**: Separar Worker permite escalado independiente del procesamiento de mensajes (ej: escalar Worker en picos de notificaciones sin escalar API).

### 3. Serverless (AWS Lambda + SQS)
**Pros**: Escalado automático, pago por uso  
**Contras**: Vendor lock-in, cold starts, debugging complejo, costos impredecibles en producción

**Razón de rechazo**: Preferimos portabilidad y control sobre la infraestructura. Docker Compose permite desarrollo local idéntico a producción.

---

## Migración Futura

Si el sistema crece (> 10,000 usuarios, equipos de 10+ desarrolladores), podemos extraer módulos a microservicios:

1. **Fase 1 (actual)**: Monolito modular con API + Worker
2. **Fase 2 (6-12 meses)**: Separar Worker en servicio independiente con su propia DB (event-driven)
3. **Fase 3 (1-2 años)**: Extraer módulos de dominio como microservicios (ej: `TicketService`, `NotificationService`, `AnalyticsService`)

La arquitectura actual con Clean Architecture + RabbitMQ facilita esta migración gradual sin reescribir todo el sistema.

---

## Referencias

- [Monolith First - Martin Fowler](https://martinfowler.com/bliki/MonolithFirst.html)
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Modular Monoliths - Simon Brown](https://www.youtube.com/watch?v=5OjqD-ow8GE)
