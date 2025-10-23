# 🚀 Guía de Inicio Rápido - TicketFlow

Instrucciones paso a paso para levantar el sistema completo de TicketFlow.

---

## 📋 Prerrequisitos

Antes de comenzar, asegúrate de tener instalado:

- ✅ **Docker Desktop** (para contenedores)
- ✅ **.NET 8 SDK** (para backend)
- ✅ **Node.js 20+** y **npm** (para frontend)
- ✅ **PostgreSQL** (se levanta con Docker)
- ✅ **RabbitMQ** (se levanta con Docker)

---

## 🏗️ Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                        TicketFlow                           │
├─────────────────┬───────────────┬──────────────┬───────────┤
│   Frontend      │   API         │   Worker     │   Infra   │
│   (Vite)        │   (.NET 8)    │   (.NET 8)   │  (Docker) │
│   Port: 5173    │   Port: 5076  │   Background │  Various  │
└─────────────────┴───────────────┴──────────────┴───────────┘
```

**Componentes**:
- **Frontend**: Aplicación web TypeScript/Vite
- **API**: Backend REST API en .NET 8
- **Worker**: Procesador de colas en segundo plano
- **PostgreSQL**: Base de datos (puerto 5432)
- **RabbitMQ**: Message broker (puerto 5672, UI: 15672)

---

## 🐳 Opción 1: Inicio Completo con Docker (Recomendado)

### Levantar TODO con un solo comando

```powershell
# Desde la raíz del proyecto
cd deploy
docker-compose up --build -d
```

Esto levanta:
- ✅ PostgreSQL (puerto 5432)
- ✅ RabbitMQ (puerto 5672, UI en http://localhost:15672)
- ✅ API Backend (puerto 5076)
- ✅ Worker de colas (background)

### Verificar que los contenedores están corriendo

```powershell
docker-compose ps
```

Deberías ver:
```
NAME              STATUS          PORTS
ticketflow-api    Up             0.0.0.0:5076->5076/tcp
ticketflow-db     Up             0.0.0.0:5432->5432/tcp
ticketflow-rabbitmq Up           0.0.0.0:5672->5672/tcp, 0.0.0.0:15672->15672/tcp
ticketflow-worker Up             (no ports exposed)
```

### Levantar Frontend (separado)

```powershell
# En otra terminal
cd src\web
npm install    # Solo la primera vez
npm run dev
```

🌐 **Acceder a la aplicación**: http://localhost:5173

### Detener todos los contenedores

```powershell
cd deploy
docker-compose down
```

### Ver logs en tiempo real

```powershell
# Todos los servicios
docker-compose logs -f

# Solo API
docker-compose logs -f api

# Solo Worker
docker-compose logs -f worker

# Solo RabbitMQ
docker-compose logs -f rabbitmq
```

---

## 💻 Opción 2: Desarrollo Local (Sin Docker)

Útil para debugging y desarrollo activo del backend.

### Paso 1: Levantar Infraestructura (PostgreSQL + RabbitMQ)

```powershell
cd deploy
docker-compose up -d db rabbitmq
```

Esto levanta SOLO:
- ✅ PostgreSQL (puerto 5432)
- ✅ RabbitMQ (puerto 5672)

### Paso 2: Configurar Base de Datos

```powershell
# Aplicar migraciones (solo primera vez o después de cambios en BD)
cd src\server\backend\Infrastructure
dotnet ef database update --project ../Api
```

### Paso 3: Levantar API Backend

```powershell
# Desde la raíz del proyecto
cd src\server\backend\Api
dotnet run
```

🔗 **API corriendo en**: http://localhost:5076  
📄 **Swagger UI**: http://localhost:5076/swagger

### Paso 4: Levantar Worker (Segunda Terminal)

```powershell
cd src\server\worker
dotnet run
```

El worker procesa mensajes de RabbitMQ en segundo plano.

### Paso 5: Levantar Frontend (Tercera Terminal)

```powershell
cd src\web
npm install    # Solo la primera vez
npm run dev
```

🌐 **Frontend corriendo en**: http://localhost:5173

---

## 🧪 Ejecutar Tests

### Tests Backend (.NET)

```powershell
# Todos los tests
cd tests\Domain
dotnet test

# Con verbosidad
dotnet test --verbosity normal

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

**Resultado esperado**: ✅ 79/79 tests pasando

### Tests Frontend (TypeScript)

```powershell
cd src\web

# Ejecutar tests
npm test

# Modo watch (desarrollo)
npm test -- --watch

# Con cobertura
npm run test:coverage

# UI interactiva
npm run test:ui
```

**Resultado esperado**: ✅ 52/52 tests pasando

---

## 🔑 Acceso y Credenciales

### Usuario Admin (Auto-creado)

```
Email: admin@ticketflow.com
Contraseña: (generada automáticamente en primer arranque)
Rol: ADMIN
```

**Nota**: En desarrollo, el sistema usa autenticación simplificada. El admin se crea automáticamente al reconstruir contenedores.

### RabbitMQ Management UI

```
URL: http://localhost:15672
Usuario: guest
Contraseña: guest
```

### PostgreSQL

```
Host: localhost
Puerto: 5432
Base de datos: ticketflow
Usuario: postgres
Contraseña: postgres123
```

---

## 📦 Puertos Utilizados

| Servicio | Puerto | URL |
|----------|--------|-----|
| **Frontend (Vite)** | 5173 | http://localhost:5173 |
| **API Backend** | 5076 | http://localhost:5076 |
| **PostgreSQL** | 5432 | localhost:5432 |
| **RabbitMQ** | 5672 | (AMQP protocol) |
| **RabbitMQ UI** | 15672 | http://localhost:15672 |

---

## 🔧 Comandos Útiles

### Reconstruir Contenedores (Fresh Start)

```powershell
cd deploy
docker-compose down -v              # Baja contenedores y borra volúmenes
docker-compose up --build -d        # Reconstruye y levanta
```

### Ver Estado de RabbitMQ

```powershell
# Acceder al contenedor
docker exec -it ticketflow-rabbitmq bash

# Listar colas
rabbitmqctl list_queues
```

### Limpiar Base de Datos

```powershell
cd deploy
docker-compose down -v db           # Borra el volumen de PostgreSQL
docker-compose up -d db             # Levanta BD limpia
```

### Build Frontend para Producción

```powershell
cd src\web
npm run build

# Los archivos quedan en: src/web/apps/demo-vanilla/dist/
```

### Build Backend para Producción

```powershell
cd src\server\backend\Api
dotnet publish -c Release -o ./publish
```

---

## 🐛 Troubleshooting

### Puerto ya en uso

```powershell
# Verificar qué proceso usa un puerto
netstat -ano | findstr :5076
netstat -ano | findstr :5173

# Matar proceso por PID
taskkill /PID <numero_pid> /F
```

### Contenedor no inicia

```powershell
# Ver logs del contenedor con error
docker-compose logs api
docker-compose logs worker

# Reiniciar contenedor específico
docker-compose restart api
```

### Migraciones no aplicadas

```powershell
cd src\server\backend\Infrastructure
dotnet ef database update --project ../Api

# Ver migraciones pendientes
dotnet ef migrations list --project ../Api
```

### RabbitMQ no procesa mensajes

1. Verificar que el worker está corriendo
2. Acceder a RabbitMQ UI: http://localhost:15672
3. Revisar las colas: `tickets.metrics`, `tickets.notifications`
4. Ver logs del worker: `docker-compose logs -f worker`

### Frontend no conecta con API

Verificar variable de entorno en `src/web/apps/demo-vanilla/`:
```typescript
// En apiClient.ts
const API_BASE_URL = import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5076';
```

---

## 🚦 Flujo de Desarrollo Típico

### Inicio de Jornada

```powershell
# Terminal 1: Infraestructura
cd deploy
docker-compose up -d

# Terminal 2: Backend API
cd src\server\backend\Api
dotnet run

# Terminal 3: Frontend
cd src\web
npm run dev
```

### Trabajar con Migraciones

```powershell
# Crear nueva migración
cd src\server\backend\Infrastructure
dotnet ef migrations add NombreDeMigracion --project ../Api

# Aplicar migraciones
dotnet ef database update --project ../Api

# Revertir última migración
dotnet ef migrations remove --project ../Api
```

### Antes de Commit

```powershell
# Ejecutar tests backend
cd tests\Domain
dotnet test

# Ejecutar tests frontend
cd src\web
npm test -- --run

# Build para verificar
cd src\server\backend\Api
dotnet build

cd src\web
npm run build
```

---

## 📚 Más Información

- **Tests**: Ver `TESTS_SUMMARY.md` para detalles completos de pruebas
- **Arquitectura**: Ver `docs/` para diagramas y documentación técnica
- **API Docs**: http://localhost:5076/swagger (cuando API está corriendo)

---

## ✅ Verificación Rápida (Health Check)

Ejecuta estos comandos para verificar que todo funciona:

```powershell
# 1. Verificar contenedores Docker
docker-compose ps

# 2. Verificar API (debe retornar 200)
curl http://localhost:5076/health

# 3. Verificar RabbitMQ
curl http://localhost:15672/

# 4. Verificar Frontend (abrir en navegador)
start http://localhost:5173
```

Si todos responden ✅, el sistema está operativo.

---

**Última actualización**: Octubre 23, 2025  
**Versión**: 1.0.0  
**Soporte**: Equipo TicketFlow
