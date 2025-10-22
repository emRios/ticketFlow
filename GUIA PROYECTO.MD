TicketFlow — Guía de Arquitectura y Avance (MVP)

Propósito: Documentar en un único lugar el qué, cómo y en qué orden construiremos el MVP de TicketFlow con enfoque simple, escalable, POO y buenas prácticas.
Ámbito: Sistema de tickets con tablero tipo pipeline, auto-asignación, auditoría y mensajería asíncrona.

1) Visión y Alcance (MVP)

Actores: Admin, Agent, Client.

Funcionalidad clave:

CRUD de tickets + estados (NEW, IN_PROGRESS, ON_HOLD, RESOLVED)

Auto-asignación por carga (heurística simple)

Tags en tickets

Tablero Kanban con drag & drop

Auditoría (quién/cómo/cuándo: before/after)

Eventos a broker (RabbitMQ) y workers (notificaciones/métricas)

Entregables: repo monorepo, docs, contratos (API+events), envs, compose infra.

2) Arquitectura (visión de alto nivel)

Monorepo con tres apps: backend/ (API), worker/ (consumidores), frontend/ (Kanban).

Clean/Hexagonal + DDD ligero: Domain, Application, Infrastructure, Api.

Outbox Pattern para publicar eventos a RabbitMQ (exchange tickets, colas notifications, metrics, DLQ).

DB: PostgreSQL 16 (o SQL Server si se decide).

Observabilidad: health checks (DB + RabbitMQ), logs estructurados, métricas mínimas.

Decisiones registradas: ver docs/adr/.

3) Estructura del Monorepo
/backend
  /src
    /Api        /Application     /Domain        /Infrastructure
  /tests        # Domain, Application, Integration

/worker
  /src
  /tests

/frontend       # ya existente (vanilla + Vite)

/contracts      # events + API (OpenAPI)
  README.md
  openapi.yaml  # stub

/docs           # health, topology, data-model, state-machine, metrics, ADRs, runbook
/deploy         # docker-compose (infra), env/*.example, seeds/

/scripts        # verify-structure.ps1, check-markdown-links.ps1, (opc) dev-*.ps1

4) Contratos (Fuente de Verdad)

Eventos (RabbitMQ, topic tickets):

ticket.created, ticket.status.changed, ticket.assigned

Envelope común: eventId, eventType, occurredAt, correlationId, actor{id,role}, ticket{...}, changes?{from,to}

Ver ejemplos en contracts/README.md.

API (REST mínima):

POST /tickets, GET /tickets, PATCH /tickets/{id}/status, PATCH /tickets/{id}

POST /tickets/{id}/tags, DELETE /tickets/{id}/tags/{tag}

GET /tickets/{id}/audit-logs, GET /users?role=AGENT, GET /health

Esqueleto OpenAPI en contracts/openapi.yaml.

5) Topología RabbitMQ (mínima)

Exchange: tickets (tipo topic)

Queues:

notifications ← ticket.created, ticket.status.changed, ticket.assigned

metrics ← ticket.*

notifications.dlq (DLQ)

Headers recomendados: x-event-id, x-correlation-id

Reintentos: backoff 1m/5m/15m vía DLX

Detalle: docs/rabbitmq-topology.md.

6) Modelo de Datos (conceptual)

Users: Id (PK), Name, Email (UQ), Role [ADMIN|AGENT|CLIENT], IsActive, CreatedAt

Tickets: Id (PK), Code (UQ), Title, Description, Status, Priority, CreatorId (FK Users), AssignedTo (FK Users, null), CreatedAt, UpdatedAt, ClosedAt?

Tags: Id (PK), Name (UQ), Color?

TicketTags: PK (TicketId, TagId)

AuditLogs: Id (PK), ActorId (FK Users|null), EntityType, EntityId, Action, BeforeJson, AfterJson, At, CorrelationId

Outbox: Id (PK), Type, PayloadJson, OccurredAt, CorrelationId, DispatchedAt?, Attempts

ProcessedEvents: EventId (PK), ProcessedAt

Índices sugeridos en docs/data-model.md.

7) Máquina de Estados (reglas)

Estados: NEW → IN_PROGRESS ↔ ON_HOLD → RESOLVED (terminal)

Reglas:

RESOLVED requiere AssignedTo no nulo

Cada transición: AuditLog + evento ticket.status.changed

Asignación manual → ticket.assigned

Creación: intenta auto-asignación; si falla, AssignedTo=null y reintentos asíncronos

Detalle: docs/state-machine.md.

8) Auto-asignación (heurística MVP)

Score agente: openCount + (inProgressCount * 1.5)

Desempate: lastAssignedAt más antiguo, luego AgentId ascendente

Flujo: filtra activos → ordena → asigna → persiste + Outbox → reintenta en colisión

Métricas: agent_load_score, assign_attempts_total, assign_conflicts_total

Detalle: docs/assignment-strategy.md.

9) Envs y Puertos (dev)

Backend (deploy/env/backend.env.example):

DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASS
RABBIT_HOST, RABBIT_PORT, RABBIT_USER, RABBIT_PASS
JWT_SECRET, APP_URL, LOG_LEVEL
OUTBOX_ENABLED, OUTBOX_DISPATCH_INTERVAL_MS


Worker (deploy/env/worker.env.example):

RABBIT_HOST, RABBIT_PORT, RABBIT_USER, RABBIT_PASS
EMAIL_FROM, EMAIL_SMTP_HOST, EMAIL_SMTP_PORT, EMAIL_USER, EMAIL_PASS
APP_URL, LOG_LEVEL


Frontend (deploy/env/frontend.env.example):

VITE_API_BASE_URL


Puertos sugeridos: DB 5432, Rabbit 5672/15672 UI, API 8080, Frontend 5173.

10) Runbook Local (infra)

Infra: docker compose --profile infra up -d (desde /deploy)

Health: ver docs/health.md → checks DB+RabbitMQ

Seeds (solo archivos): deploy/seeds/USERS.csv, deploy/seeds/TAGS.csv

Apagar: docker compose --profile infra down (usar --volumes si limpieza total)

(Scripts PowerShell opcionales scripts/dev-*.ps1 documentados pero no obligatorios.)

11) CI — Guardrails

Jobs: verify-structure y check-markdown-links (Windows)

Scripts: scripts/verify-structure.ps1, scripts/check-markdown-links.ps1

Objetivo: fallar si faltan docs/envs/contratos o hay links rotos.

ci.yml ya actualizado en .github/workflows/.

12) Roadmap de Implementación (solo orden, sin código)

Backend — Domain & Application (contratos internos)

Entidades, VOs, eventos; use cases: CreateTicket, ChangeStatus, AssignTicket, ListTickets, ManageTags, GetAuditLogs.

Puertos: repos, outbox, bus, clock, id-gen, assignment strategy.

Infrastructure — Persistence & Outbox

Repos + OutboxStore (sin lógica de negocio); migraciones.

Dispatcher Outbox → Rabbit (confirmaciones, idempotencia).

API — Endpoints & Health

Endpoints del contrato (validaciones, mapeos, errores)

/health (DB + RabbitMQ)

Worker — Consumers

notifications (mock de envío), metrics (contadores).

Reintentos + DLQ + heartbeat básico.

Frontend — Kanban

Columnas por estado; drag & drop → PATCH estado; filtros.

Vista de auditoría del ticket.

Pruebas

Unit (dominio/use cases), integración (repos/outbox/eventbus).

End-to-end mínimo (API↔Rabbit↔Worker en local).

13) Definition of Done (MVP)

Crear ticket auto-asignado (si hay agentes activos) y evento ticket.created publicado.

Cambiar estado a IN_PROGRESS → AuditLog + evento ticket.status.changed.

Worker consume ticket.created y simula notificación (log/contador).

Métricas básicas expuestas/contabilizadas.

CI pasa (estructura + links). Health OK en local (DB+Rabbit).

Docs actualizadas (state machine, topology, data model, metrics, ADRs).

14) No Objetivos (fuera del MVP)

OAuth/SSO, multitenancy avanzada, adjuntos, SLA/relojes laborales, notificaciones multicanal reales, caching distribuido, búsqueda full-text, dashboards complejos.

15) Convenciones y Nombres

Eventos: ticket.created, ticket.status.changed, ticket.assigned

Headers: x-event-id, x-correlation-id

Estados: NEW, IN_PROGRESS, ON_HOLD, RESOLVED

Prioridad: LOW, MEDIUM, HIGH, URGENT

Repositorios: TicketRepository, UserRepository, etc.

Métricas: snake_case, etiquetas cortas ({queue}, {status}, {code}).

16) Enlaces útiles del repo

Contracts (events + API): contracts/README.md

OpenAPI stub: contracts/openapi.yaml

RabbitMQ Topology: docs/rabbitmq-topology.md

Health: docs/health.md

Modelo de Datos: docs/data-model.md

State Machine: docs/state-machine.md

Assignment Strategy: docs/assignment-strategy.md

Métricas: docs/metrics.md

ADRs: docs/adr/

Runbook Dev: docs/runbook-dev.md