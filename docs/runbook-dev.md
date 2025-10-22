# Runbook - Desarrollo Local (TicketFlow)

Guía rápida para levantar el entorno de desarrollo local del monorepo TicketFlow.

---

## Prerrequisitos

Antes de comenzar, asegúrate de tener instalado:

### 1. Docker Desktop
- **Windows**: [Docker Desktop para Windows](https://docs.docker.com/desktop/install/windows-install/)
- **Versión mínima**: 4.20 o superior
- **Configuración recomendada**:
  - WSL 2 backend habilitado
  - 4 GB RAM asignados a Docker
  - 2 CPUs asignados

**Verificar instalación**:
```powershell
docker --version
# Docker version 24.0.0 o superior

docker compose version
# Docker Compose version v2.20.0 o superior
```

### 2. PowerShell
- **Windows**: PowerShell 5.1 (incluido en Windows 10/11) o PowerShell 7+
- **Verificar versión**:
```powershell
$PSVersionTable.PSVersion
# Major  Minor  Build  Revision
# -----  -----  -----  --------
# 5      1      ...
```

### 3. Git
- Para clonar el repositorio y control de versiones
- **Verificar**:
```powershell
git --version
# git version 2.40.0 o superior
```

### 4. Editor de Código (Opcional)
- **Visual Studio Code** con extensiones:
  - Docker
  - PowerShell
  - Markdown All in One
- **Visual Studio 2022** (para backend C# cuando esté implementado)
- **Rider** (alternativa JetBrains)

---

## Clonar el Repositorio

```powershell
# Clonar desde GitHub
git clone https://github.com/chiorji/kanban-board.git TicketFlow
cd TicketFlow

# Verificar estructura
powershell -File scripts/verify-structure.ps1
# Debe retornar: STRUCTURE OK
```

---

## Levantar Infraestructura

### Paso 1: Iniciar PostgreSQL y RabbitMQ

```powershell
# Navegar a carpeta de deployment
cd deploy

# Levantar solo servicios de infraestructura (profile: infra)
docker compose --profile infra up -d

# Verificar que los contenedores están corriendo
docker compose ps
```

**Output esperado**:
```
NAME                    IMAGE                              STATUS       PORTS
ticketflow-postgres     postgres:16-alpine                 Up           0.0.0.0:5432->5432/tcp
ticketflow-rabbitmq     rabbitmq:3.13-management-alpine    Up           0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
```

### Paso 2: Verificar Health de Servicios

**PostgreSQL**:
```powershell
# Desde PowerShell
docker exec ticketflow-postgres pg_isready -U ticketflow_user -d ticketflow

# Output esperado:
# /var/run/postgresql:5432 - accepting connections
```

**RabbitMQ**:
```powershell
# Verificar que RabbitMQ responde
docker exec ticketflow-rabbitmq rabbitmq-diagnostics ping

# Output esperado:
# Ping succeeded
```

**Acceso a UIs**:
- **PostgreSQL**: Conectar con cliente (DBeaver, pgAdmin, etc.)
  - Host: `localhost`
  - Port: `5432`
  - Database: `ticketflow`
  - User: `ticketflow_user`
  - Password: `ticketflow_pass`

- **RabbitMQ Management UI**: [http://localhost:15672](http://localhost:15672)
  - User: `ticketflow_user`
  - Password: `ticketflow_pass`

### Paso 3: Ver Logs (si hay problemas)

```powershell
# Ver logs de PostgreSQL
docker compose logs postgres

# Ver logs de RabbitMQ
docker compose logs rabbitmq

# Seguir logs en tiempo real
docker compose logs -f postgres
docker compose logs -f rabbitmq
```

---

## Variables de Entorno Locales

### Backend API (Cuando esté implementado)

Copiar el archivo de ejemplo y editar valores:

```powershell
# Desde la raíz del repo
cd deploy/env
cp backend.env.example backend.env.local
```

**Variables sugeridas para desarrollo** (`backend.env.local`):

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=ticketflow
DB_USER=ticketflow_user
DB_PASS=ticketflow_pass

# RabbitMQ
RABBIT_HOST=localhost
RABBIT_PORT=5672
RABBIT_USER=ticketflow_user
RABBIT_PASS=ticketflow_pass

# JWT (generar secreto con: openssl rand -base64 32)
JWT_SECRET=your-development-secret-key-at-least-32-chars

# Application
APP_URL=http://localhost:5000
LOG_LEVEL=Debug
OUTBOX_ENABLED=true
OUTBOX_DISPATCH_INTERVAL_MS=500
```

### Worker (Cuando esté implementado)

```powershell
cd deploy/env
cp worker.env.example worker.env.local
```

**Variables sugeridas** (`worker.env.local`):

```env
# RabbitMQ
RABBIT_HOST=localhost
RABBIT_PORT=5672
RABBIT_USER=ticketflow_user
RABBIT_PASS=ticketflow_pass

# Email SMTP (usar servicio de pruebas como Mailtrap)
SMTP_HOST=smtp.mailtrap.io
SMTP_PORT=2525
SMTP_USER=your-mailtrap-user
SMTP_PASS=your-mailtrap-pass
SMTP_FROM=noreply@ticketflow.local

# Logging
LOG_LEVEL=Debug
LOG_OUTPUT=Console

# Metrics
METRICS_ENABLED=true
METRICS_PORT=9090
```

### Frontend (Cuando esté implementado)

```powershell
cd deploy/env
cp frontend.env.example frontend.env.local
```

**Variables sugeridas** (`frontend.env.local`):

```env
# API Base URL
VITE_API_BASE_URL=http://localhost:5000
```

---

## Flujo de Trabajo Completo

### Fase 1: Solo Infraestructura (Actual)

```powershell
# 1. Levantar PostgreSQL + RabbitMQ
cd deploy
docker compose --profile infra up -d

# 2. Verificar health checks
docker compose ps
# Ambos servicios deben mostrar "Up (healthy)"

# 3. (Opcional) Explorar servicios
# - Conectar a PostgreSQL con DBeaver
# - Abrir RabbitMQ UI: http://localhost:15672
```

### Fase 2: Backend + Worker (Futuro)

```powershell
# 1. Levantar infraestructura (si no está corriendo)
docker compose --profile infra up -d

# 2. Compilar backend (desde raíz del repo)
cd backend/src/Api
dotnet build
dotnet ef database update  # Aplicar migraciones

# 3. Ejecutar seeding (datos de prueba)
dotnet run -- seed --environment Development

# 4. Correr API en IDE (Visual Studio, Rider) o terminal
dotnet run
# API disponible en: http://localhost:5000

# 5. En otra terminal, correr Worker
cd ../../worker/src
dotnet run
# Worker comienza a consumir mensajes de RabbitMQ

# 6. Verificar health checks
curl http://localhost:5000/health
# Debe retornar: { "status": "Healthy", ... }
```

### Fase 3: Frontend (Futuro)

```powershell
# 1. Instalar dependencias (primera vez)
cd frontend
npm install

# 2. Configurar variables de entorno
# Crear frontend/.env.local con:
# VITE_API_BASE_URL=http://localhost:5000

# 3. Levantar dev server
npm run dev
# Frontend disponible en: http://localhost:5174

# 4. El frontend ahora consume la API real (no mocks)
```

### Fase 4: Todo en Docker (Futuro)

```powershell
# Levantar todo (infra + aplicaciones)
docker compose --profile infra --profile app up -d

# Verificar todos los servicios
docker compose ps

# Servicios disponibles:
# - API: http://localhost:5000
# - Frontend: http://localhost:8080 (nginx)
# - PostgreSQL: localhost:5432
# - RabbitMQ UI: http://localhost:15672
```

---

## Health Checks

Para verificar el estado de salud de los servicios, consulta la documentación completa en:

📄 **[docs/health.md](../docs/health.md)**

### Quick Check

**Infraestructura**:
```powershell
# PostgreSQL
docker exec ticketflow-postgres pg_isready -U ticketflow_user -d ticketflow

# RabbitMQ
docker exec ticketflow-rabbitmq rabbitmq-diagnostics ping
```

**API (cuando esté implementado)**:
```powershell
# Health check endpoint
curl http://localhost:5000/health | ConvertFrom-Json

# Output esperado:
# {
#   "status": "Healthy",
#   "checks": [
#     { "name": "db", "status": "Healthy", "latencyMs": 12 },
#     { "name": "rabbitmq", "status": "Healthy", "latencyMs": 8 }
#   ],
#   "version": "api-0.1.0",
#   "uptimeSec": 12345
# }
```

**Worker (cuando esté implementado)**:
```powershell
# Verificar logs del worker
docker compose logs worker --tail 50

# Buscar heartbeat reciente (último minuto)
docker compose logs worker | Select-String "heartbeat"
```

---

## Troubleshooting

### Problema: Puerto 5432 ya está en uso

**Síntoma**:
```
Error: bind: address already in use
```

**Causa**: Ya tienes PostgreSQL instalado localmente corriendo en puerto 5432

**Soluciones**:

1. **Opción A: Detener PostgreSQL local**:
```powershell
# Detener servicio de Windows
Stop-Service postgresql-x64-14  # Ajustar versión

# O detener manualmente desde Services.msc
```

2. **Opción B: Cambiar puerto en docker-compose.yml**:
```yaml
postgres:
  ports:
    - "5433:5432"  # Usar puerto 5433 externamente
```

Luego actualizar variables de entorno: `DB_PORT=5433`

---

### Problema: Puerto 15672 (RabbitMQ UI) ocupado

**Causa**: Otro servicio usa el puerto 15672

**Solución**:
```yaml
rabbitmq:
  ports:
    - "15673:15672"  # Cambiar puerto externo
```

Acceder a UI en: http://localhost:15673

---

### Problema: Credenciales inválidas

**Síntoma**:
```
FATAL: password authentication failed for user "ticketflow_user"
```

**Solución**:

1. Verificar credenciales en `deploy/docker-compose.yml`:
```yaml
environment:
  POSTGRES_USER: ticketflow_user
  POSTGRES_PASSWORD: ticketflow_pass
```

2. Verificar que coincidan con tus archivos `.env.local`

3. Si cambiaste credenciales, recrear contenedores:
```powershell
docker compose down -v  # Elimina volúmenes
docker compose --profile infra up -d
```

---

### Problema: Contenedor no inicia (unhealthy)

**Diagnóstico**:
```powershell
# Ver estado detallado
docker compose ps

# Ver logs completos
docker compose logs postgres --tail 100
docker compose logs rabbitmq --tail 100
```

**Causas comunes**:

1. **Falta de recursos**: Docker necesita al menos 4GB RAM
   - Solución: Ajustar en Docker Desktop → Settings → Resources

2. **Volúmenes corruptos**:
```powershell
docker compose down -v  # Elimina volúmenes
docker compose --profile infra up -d
```

3. **Puerto bloqueado por firewall**:
   - Solución: Permitir puertos 5432, 5672, 15672 en Windows Firewall

---

### Problema: Backend no conecta a RabbitMQ

**Síntoma**:
```
Connection refused: localhost:5672
```

**Solución**:

1. Verificar que RabbitMQ está corriendo:
```powershell
docker compose ps rabbitmq
```

2. Verificar credenciales en `backend.env.local`:
```env
RABBIT_HOST=localhost  # No usar "rabbitmq" fuera de Docker
RABBIT_USER=ticketflow_user
RABBIT_PASS=ticketflow_pass
```

3. Si API corre en Docker, usar nombre del servicio:
```env
RABBIT_HOST=rabbitmq  # Nombre del servicio en docker-compose.yml
```

---

### Problema: Frontend no encuentra API

**Síntoma**:
```
Failed to fetch: http://localhost:5000/api/tickets
```

**Solución**:

1. Verificar que API está corriendo:
```powershell
curl http://localhost:5000/health
```

2. Verificar variable de entorno en frontend:
```env
# frontend/.env.local
VITE_API_BASE_URL=http://localhost:5000
```

3. Si hay CORS, verificar configuración en backend:
```csharp
// Backend startup
services.AddCors(options => {
  options.AddPolicy("Development", builder => {
    builder.WithOrigins("http://localhost:5174")  // Puerto de Vite
           .AllowAnyMethod()
           .AllowAnyHeader();
  });
});
```

---

## Comandos Útiles

### Docker Compose

```powershell
# Levantar servicios
docker compose --profile infra up -d

# Detener servicios (mantiene volúmenes)
docker compose --profile infra down

# Detener y eliminar volúmenes (datos persistentes)
docker compose down -v

# Ver logs en tiempo real
docker compose logs -f

# Ver logs de un servicio específico
docker compose logs postgres --tail 50

# Reiniciar un servicio
docker compose restart postgres

# Reconstruir imágenes (si cambiaste Dockerfile)
docker compose build --no-cache

# Ver recursos consumidos
docker stats
```

### PostgreSQL

```powershell
# Conectar a psql dentro del contenedor
docker exec -it ticketflow-postgres psql -U ticketflow_user -d ticketflow

# Ejecutar query desde fuera
docker exec ticketflow-postgres psql -U ticketflow_user -d ticketflow -c "SELECT COUNT(*) FROM tickets;"

# Hacer backup de DB
docker exec ticketflow-postgres pg_dump -U ticketflow_user ticketflow > backup.sql

# Restaurar backup
docker exec -i ticketflow-postgres psql -U ticketflow_user ticketflow < backup.sql
```

### RabbitMQ

```powershell
# Listar colas
docker exec ticketflow-rabbitmq rabbitmqctl list_queues

# Listar exchanges
docker exec ticketflow-rabbitmq rabbitmqctl list_exchanges

# Purgar una cola (eliminar mensajes)
docker exec ticketflow-rabbitmq rabbitmqctl purge_queue notifications

# Ver bindings
docker exec ticketflow-rabbitmq rabbitmqctl list_bindings
```

---

## Limpieza Completa

Si necesitas empezar desde cero:

```powershell
# 1. Detener todos los servicios
docker compose down -v

# 2. Eliminar imágenes (opcional)
docker rmi postgres:16-alpine rabbitmq:3.13-management-alpine

# 3. Limpiar Docker system
docker system prune -a --volumes

# 4. Volver a levantar
docker compose --profile infra up -d
```

---

## Scripts de Verificación

### Verificar Estructura del Repo

```powershell
# Desde la raíz del repo
powershell -File scripts/verify-structure.ps1

# Debe retornar: STRUCTURE OK
```

### Verificar Enlaces Markdown

```powershell
# Desde la raíz del repo
powershell -File scripts/check-markdown-links.ps1

# Debe retornar: LINKS OK
```

---

## Integración E2E (sin lógica)

Esta sección describe cómo ejecutar un flujo end-to-end básico para validar la integración de la infraestructura, incluso antes de implementar la lógica de negocio en el backend.

### Paso 1: Levantar Infraestructura

Inicia PostgreSQL y RabbitMQ usando el script de desarrollo:

```powershell
# Opción 1: Usar script automatizado (recomendado)
pwsh -File scripts/dev-up.ps1

# Opción 2: Comando Docker Compose directo
cd deploy
docker compose --profile infra up -d
cd ..
```

**Resultado esperado:**
```
[INFO] Iniciando infraestructura local...
[OK] PostgreSQL healthy en 15 segundos
[OK] RabbitMQ healthy en 20 segundos

=====================================================================
  INFRA LEVANTADA - Servicios disponibles
=====================================================================

Endpoints:
  - PostgreSQL: localhost:5432 (ticketflow/dev123)
  - RabbitMQ AMQP: localhost:5672 (guest/guest)
  - RabbitMQ Management: http://localhost:15672 (guest/guest)
```

### Paso 2: Verificar Health de Infraestructura

Usa el script de status para validar que ambos servicios están healthy:

```powershell
pwsh -File scripts/dev-status.ps1
```

**Salida esperada:**
```
[OK] PostgreSQL: healthy
[OK] RabbitMQ: healthy

=====================================================================
  STATUS: HEALTHY - Todos los servicios están operativos
=====================================================================
```

**Verificaciones adicionales:**

1. **PostgreSQL** - Conectar con cliente SQL:
   ```powershell
   # Con psql (si está instalado)
   psql -h localhost -p 5432 -U ticketflow -d ticketflow
   # Password: dev123
   
   # Listar bases de datos
   \l
   
   # Salir
   \q
   ```

2. **RabbitMQ Management UI** - Abrir en navegador:
   ```
   http://localhost:15672
   Username: guest
   Password: guest
   ```
   
   Verificar:
   - Dashboard muestra "Node running"
   - Sección "Queues" está vacía (sin queues todavía)
   - Sección "Exchanges" muestra exchanges predeterminados

**Referencia completa de health checks:**  
📄 Ver [docs/health.md](health.md) para detalles de contratos de health.

### Paso 3: Levantar API y Worker (A Futuro)

⚠️ **Pendiente de implementación**

Una vez que el backend esté implementado, se agregarán al `docker-compose.yml` con profile `app`:

```powershell
# Levantar infraestructura + aplicaciones
docker compose --profile infra --profile app up -d

# O usar script que levante todo
pwsh -File scripts/dev-up.ps1 -IncludeApp
```

**Servicios adicionales esperados:**
- **API**: `http://localhost:5000` (ASP.NET Core)
- **Worker**: Procesa tabla Outbox y publica a RabbitMQ

**Verificar health de aplicaciones:**
```powershell
# Health check del API
curl http://localhost:5000/health

# Respuesta esperada:
# {
#   "status": "healthy",
#   "dependencies": {
#     "database": "healthy",
#     "rabbitmq": "healthy"
#   }
# }
```

### Paso 4: Disparar Evento Simulado y Observar Consumers (A Futuro)

⚠️ **Pendiente de implementación**

Una vez que el Worker esté implementado, podrás simular eventos para validar el flujo completo:

**Opción 1: Insertar evento manualmente en tabla Outbox**

```sql
-- Conectar a PostgreSQL
psql -h localhost -p 5432 -U ticketflow -d ticketflow

-- Insertar evento simulado
INSERT INTO "Outbox" ("Id", "EventType", "AggregateId", "Payload", "CreatedAt", "ProcessedAt")
VALUES (
  gen_random_uuid(),
  'ticket.created',
  'tk-202501-9999',
  '{"ticketId":"tk-202501-9999","status":"NEW","priority":"HIGH","assignedTo":"u002"}',
  NOW(),
  NULL
);

-- Verificar inserción
SELECT * FROM "Outbox" WHERE "ProcessedAt" IS NULL;
```

**Resultado esperado:**
1. Worker detecta evento pendiente (polling cada 5 segundos)
2. Worker publica evento a RabbitMQ exchange `ticketflow.events`
3. Worker marca evento como procesado (`ProcessedAt` != NULL)

**Opción 2: Crear ticket vía API**

```powershell
# POST /tickets (crear ticket)
curl -X POST http://localhost:5000/tickets `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer <JWT_TOKEN>" `
  -d '{
    "title": "Test E2E",
    "description": "Ticket de prueba para flujo E2E",
    "priority": "HIGH",
    "tagIds": ["t001"]
  }'

# Respuesta esperada:
# {
#   "id": "tk-202501-0001",
#   "status": "NEW",
#   "assignedTo": "u002",
#   ...
# }
```

**Observar en RabbitMQ Management UI:**
1. Ir a `http://localhost:15672/#/exchanges`
2. Click en exchange `ticketflow.events`
3. Ver estadísticas de mensajes publicados (message rate > 0)
4. Ir a `http://localhost:15672/#/queues`
5. Ver queues creadas (ej: `ticketflow.notifications`, `ticketflow.analytics`)
6. Ver mensajes encolados y consumidos

**Logs del Worker:**
```powershell
docker compose logs -f worker

# Salida esperada:
# [INFO] Processing 1 pending outbox messages...
# [INFO] Published event ticket.created to RabbitMQ (aggregateId=tk-202501-9999)
# [INFO] Marked outbox message as processed (id=...)
```

### Paso 5: Apagar Infraestructura

Detener servicios usando el script de shutdown:

```powershell
# Opción 1: Detener sin eliminar datos (recomendado para desarrollo)
pwsh -File scripts/dev-down.ps1

# Opción 2: Detener y purgar volúmenes (elimina datos de PostgreSQL)
pwsh -File scripts/dev-down.ps1 -Purge

# Opción 3: Comando Docker Compose directo
cd deploy
docker compose --profile infra down
# O con volúmenes:
docker compose --profile infra down --volumes
cd ..
```

**Resultado esperado:**
```
[INFO] Deteniendo infraestructura local...
[OK] Contenedores detenidos exitosamente

=====================================================================
  INFRA DETENIDA
=====================================================================

Próximos pasos:
  - Levantar nuevamente: pwsh -File scripts/dev-up.ps1
  - Ver status: pwsh -File scripts/dev-status.ps1
```

**Verificar que servicios están detenidos:**
```powershell
docker compose ps

# Debe mostrar:
# NAME                IMAGE               STATUS
# (vacío o servicios con estado "exited")
```

---

## Próximos Pasos

Una vez que la infraestructura está corriendo:

1. **Implementar Backend API** en `backend/src/Api`
2. **Implementar Worker** en `worker/src`
3. **Configurar migraciones de EF Core** para crear tablas
4. **Ejecutar seeding** con datos de `deploy/seeds/`
5. **Conectar frontend** a API real (reemplazar mocks)
6. **Levantar todo con Docker Compose** usando `--profile app`

---

## Referencias

- **[docs/health.md](../docs/health.md)** - Health checks y monitoreo
- **[docs/data-model.md](../docs/data-model.md)** - Estructura de base de datos
- **[docs/rabbitmq-topology.md](../docs/rabbitmq-topology.md)** - Configuración de mensajería
- **[deploy/docker-compose.yml](../deploy/docker-compose.yml)** - Configuración de servicios
- **[deploy/seeds/README.md](../deploy/seeds/README.md)** - Datos iniciales de prueba
- **[contracts/openapi.yaml](../contracts/openapi.yaml)** - Especificación de API

---

## Soporte

Si encuentras problemas no cubiertos en este runbook:

1. Revisar logs de Docker: `docker compose logs`
2. Consultar [Docker Desktop Troubleshooting](https://docs.docker.com/desktop/troubleshoot/overview/)
3. Verificar issues en el repositorio de GitHub
4. Contactar al equipo de desarrollo
