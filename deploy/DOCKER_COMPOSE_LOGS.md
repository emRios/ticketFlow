# ============================================
# DOCKER COMPOSE UP - Simulación de Logs (Primeros 20s)
# ============================================

[+] Running 4/4
 ✔ Network ticketflow_ticketflow-network    Created                                    0.1s
 ✔ Volume "ticketflow_postgres_data"        Created                                    0.0s
 ✔ Volume "ticketflow_rabbitmq_data"        Created                                    0.0s
 ✔ Container ticketflow-postgres            Created                                    0.2s
 ✔ Container ticketflow-rabbitmq            Created                                    0.2s

[+] Running 4/4
 ✔ Container ticketflow-postgres            Started                                    1.2s
 ✔ Container ticketflow-rabbitmq            Started                                    1.4s
 ✔ Container ticketflow-api                 Started                                    3.8s
 ✔ Container ticketflow-worker              Started                                    4.2s

Attaching to ticketflow-api, ticketflow-postgres, ticketflow-rabbitmq, ticketflow-worker

# ============================================
# POSTGRES LOGS
# ============================================
ticketflow-postgres  | 
ticketflow-postgres  | PostgreSQL Database directory appears to contain a database; Skipping initialization
ticketflow-postgres  | 
ticketflow-postgres  | 2025-10-22 14:30:01.234 UTC [1] LOG:  starting PostgreSQL 16.1 on x86_64-pc-linux-musl
ticketflow-postgres  | 2025-10-22 14:30:01.235 UTC [1] LOG:  listening on IPv4 address "0.0.0.0", port 5432
ticketflow-postgres  | 2025-10-22 14:30:01.235 UTC [1] LOG:  listening on IPv6 address "::", port 5432
ticketflow-postgres  | 2025-10-22 14:30:01.237 UTC [1] LOG:  listening on Unix socket "/var/run/postgresql/.s.PGSQL.5432"
ticketflow-postgres  | 2025-10-22 14:30:01.242 UTC [29] LOG:  database system was shut down at 2025-10-22 14:25:30 UTC
ticketflow-postgres  | 2025-10-22 14:30:01.248 UTC [1] LOG:  database system is ready to accept connections

# ============================================
# RABBITMQ LOGS
# ============================================
ticketflow-rabbitmq  |   ##  ##      RabbitMQ 3.13.0
ticketflow-rabbitmq  |   ##  ##
ticketflow-rabbitmq  |   ##########  Copyright (c) 2007-2024 Broadcom Inc and/or its subsidiaries
ticketflow-rabbitmq  |   ######  ##
ticketflow-rabbitmq  |   ##########  Licensed under the MPL 2.0. Website: https://rabbitmq.com
ticketflow-rabbitmq  | 
ticketflow-rabbitmq  |   Erlang:      26.2.5.2 [jit]
ticketflow-rabbitmq  |   TLS Library: OpenSSL - OpenSSL 3.1.7 30 Jan 2024
ticketflow-rabbitmq  | 
ticketflow-rabbitmq  | 2025-10-22 14:30:02.123 [info] <0.267.0> Feature flags: list of feature flags found:
ticketflow-rabbitmq  | 2025-10-22 14:30:02.145 [info] <0.267.0> Feature flags: feature flag states:
ticketflow-rabbitmq  | 2025-10-22 14:30:02.456 [info] <0.267.0> Starting RabbitMQ 3.13.0 on Erlang 26.2.5.2 [jit]
ticketflow-rabbitmq  | 2025-10-22 14:30:03.234 [info] <0.267.0> Server startup complete; 4 plugins started.
ticketflow-rabbitmq  | 2025-10-22 14:30:03.235 [info] <0.560.0> Management plugin started. Port: 15672
ticketflow-rabbitmq  | 2025-10-22 14:30:03.567 [info] <0.267.0>  * rabbitmq_management
ticketflow-rabbitmq  | 2025-10-22 14:30:03.568 [info] <0.267.0>  * rabbitmq_management_agent
ticketflow-rabbitmq  | 2025-10-22 14:30:03.569 [info] <0.267.0>  * rabbitmq_prometheus
ticketflow-rabbitmq  | 2025-10-22 14:30:03.570 [info] <0.267.0>  * rabbitmq_web_dispatch

# ============================================
# API LOGS
# ============================================
ticketflow-api       | info: Microsoft.Hosting.Lifetime[14]
ticketflow-api       |       Now listening on: http://[::]:5076
ticketflow-api       | info: Microsoft.Hosting.Lifetime[0]
ticketflow-api       |       Application started. Press Ctrl+C to shut down.
ticketflow-api       | info: Microsoft.Hosting.Lifetime[0]
ticketflow-api       |       Hosting environment: Development
ticketflow-api       | info: Microsoft.Hosting.Lifetime[0]
ticketflow-api       |       Content root path: /app
ticketflow-api       | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
ticketflow-api       |       Executed DbCommand (42ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
ticketflow-api       |       SELECT EXISTS (
ticketflow-api       |           SELECT 1 FROM information_schema.tables 
ticketflow-api       |           WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
ticketflow-api       |       )
ticketflow-api       | info: Microsoft.EntityFrameworkCore.Migrations[20405]
ticketflow-api       |       No migrations were applied. The database is already up to date.
ticketflow-api       | info: TicketFlow.Api.Program[0]
ticketflow-api       |       ✅ API Backend started successfully on port 5076

# ============================================
# WORKER LOGS
# ============================================
ticketflow-worker    | info: Microsoft.Hosting.Lifetime[0]
ticketflow-worker    |       Application started. Press Ctrl+C to shut down.
ticketflow-worker    | info: Microsoft.Hosting.Lifetime[0]
ticketflow-worker    |       Hosting environment: Development
ticketflow-worker    | info: Microsoft.Hosting.Lifetime[0]
ticketflow-worker    |       Content root path: /app
ticketflow-worker    | info: TicketFlow.Worker.Program[0]
ticketflow-worker    |       🔧 Inicializando infraestructura...
ticketflow-worker    | info: Microsoft.EntityFrameworkCore.Database.Command[20101]
ticketflow-worker    |       Executed DbCommand (18ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
ticketflow-worker    |       SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
ticketflow-worker    | info: Microsoft.EntityFrameworkCore.Migrations[20405]
ticketflow-worker    |       No migrations were applied. The database is already up to date.
ticketflow-worker    | info: TicketFlow.Worker.Program[0]
ticketflow-worker    |       ✅ Migraciones de base de datos aplicadas
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       🚀 Inicializando topología de RabbitMQ en rabbitmq:5672...
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       📢 Exchange declarado: 'tickets' (Type: topic, Durable: true)
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       📥 Cola declarada: 'notifications' (Durable: true, Exclusive: false)
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       📥 Cola declarada: 'metrics' (Durable: true, Exclusive: false)
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       🔗 Binding creado: Cola 'notifications' ← Exchange 'tickets' (RoutingKey: 'ticket.*')
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       🔗 Binding creado: Cola 'metrics' ← Exchange 'tickets' (RoutingKey: 'ticket.*')
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitTopologyBootstrapper[0]
ticketflow-worker    |       ✅ Topología de RabbitMQ inicializada correctamente
ticketflow-worker    | info: TicketFlow.Worker.Program[0]
ticketflow-worker    |       ✅ Infraestructura inicializada correctamente
ticketflow-worker    | info: TicketFlow.Worker.Worker[0]
ticketflow-worker    |       Outbox Worker iniciado. Polling cada 5 segundos
ticketflow-worker    | info: TicketFlow.Worker.Messaging.RabbitMq.RabbitMqPublisher[0]
ticketflow-worker    |       RabbitMQ Publisher conectado a rabbitmq. Exchange: tickets
ticketflow-worker    | info: TicketFlow.Worker.Processors.OutboxProcessor[0]
ticketflow-worker    |       Lock advisory (42) adquirido. Procesando mensajes Outbox...
ticketflow-worker    | info: TicketFlow.Worker.Processors.OutboxProcessor[0]
ticketflow-worker    |       No hay mensajes pendientes en el Outbox.
ticketflow-worker    | info: TicketFlow.Worker.Processors.OutboxProcessor[0]
ticketflow-worker    |       Lock advisory (42) liberado.

# ============================================
# HEALTHCHECKS (después de 10s)
# ============================================
ticketflow-postgres  | 2025-10-22 14:30:11.456 UTC [45] LOG:  incomplete startup packet
ticketflow-rabbitmq  | 2025-10-22 14:30:12.123 [info] <0.789.0> accepting AMQP connection <0.789.0> (172.18.0.1:54321 -> 172.18.0.3:5672)
ticketflow-api       | info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
ticketflow-api       |       Request starting HTTP/1.1 GET http://localhost:5076/health - - -
ticketflow-api       | info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
ticketflow-api       |       Executing endpoint 'Health checks'
ticketflow-api       | info: Microsoft.AspNetCore.Routing.EndpointMiddleware[1]
ticketflow-api       |       Executed endpoint 'Health checks'
ticketflow-api       | info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
ticketflow-api       |       Request finished HTTP/1.1 GET http://localhost:5076/health - 200 - text/plain 45ms

# ============================================
# RESUMEN (t=20s)
# ============================================
✅ postgres      - HEALTHY  (listening on :5432)
✅ rabbitmq      - HEALTHY  (AMQP: :5672, Management: :15672)
✅ api           - RUNNING  (listening on :5076)
✅ worker        - RUNNING  (polling Outbox every 5s)

🎯 SERVICIOS LISTOS PARA USAR:
- API Backend:        http://localhost:5076
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- PostgreSQL:          localhost:5432 (ticketflow_user/ticketflow_pass)
