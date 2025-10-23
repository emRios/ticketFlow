# ⚙️ Configuración - TicketFlow

Guía para configurar el entorno antes de ejecutar el proyecto.

---

## 🔐 Variables de Entorno

### Backend API

Crear archivo: `src/server/backend/Api/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ticketflow;Username=postgres;Password=postgres123"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Worker

Crear archivo: `src/server/worker/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ticketflow;Username=postgres;Password=postgres123"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

### Frontend

Crear archivo: `src/web/.env.development`

```env
VITE_API_BASE_URL=http://localhost:5076
```

---

## 🐳 Docker Compose

El archivo `deploy/docker-compose.yml` ya está configurado con valores por defecto:

- **PostgreSQL**: puerto 5432
- **RabbitMQ**: puerto 5672 (AMQP), 15672 (UI)
- **API**: puerto 5076
- **Worker**: sin puertos expuestos

### Personalizar Puertos (Opcional)

Editar `deploy/docker-compose.yml`:

```yaml
services:
  api:
    ports:
      - "5076:5076"  # Cambiar primer número para usar otro puerto
```

---

## 🗄️ Base de Datos

### Aplicar Migraciones

```powershell
# Solo si corres backend en local (no en Docker)
cd src\server\backend\Infrastructure
dotnet ef database update --project ../Api
```

### Seeds Iniciales

Los datos de prueba se cargan automáticamente:
- **Ubicación**: `deploy/seeds/`
- **Archivos**: `USERS.csv`, `TAGS.csv`
- **Se cargan al**: iniciar contenedor de API por primera vez

---

## 🔑 Credenciales por Defecto

### PostgreSQL
```
Host: localhost
Port: 5432
Database: ticketflow
Usuario: postgres
Password: postgres123
```

### RabbitMQ Management
```
URL: http://localhost:15672
Usuario: guest
Password: guest
```

### Usuario Admin (Auto-creado)
```
Email: admin@ticketflow.com
Password: (generada automáticamente)
Rol: ADMIN
```

---

## 🌐 URLs del Sistema

Después de configurar y ejecutar:

| Servicio | URL |
|----------|-----|
| **Frontend** | http://localhost:5173 |
| **API** | http://localhost:5076 |
| **Swagger** | http://localhost:5076/swagger |
| **RabbitMQ UI** | http://localhost:15672 |

---

## ✅ Verificar Configuración

### 1. Verificar archivos creados
```powershell
# Backend
Test-Path "src\server\backend\Api\appsettings.Development.json"
Test-Path "src\server\worker\appsettings.Development.json"

# Frontend
Test-Path "src\web\.env.development"
```

### 2. Verificar Docker Compose
```powershell
cd deploy
docker-compose config
# Debe mostrar configuración sin errores
```

---

## 🔧 Configuración Avanzada (Opcional)

### Cambiar Puerto de API

**appsettings.Development.json**:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5080"  // Cambiar aquí
      }
    }
  }
}
```

**Frontend .env.development**:
```env
VITE_API_BASE_URL=http://localhost:5080  # Actualizar también
```

### Habilitar Logs Detallados

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## 📝 Notas

- Los archivos `appsettings.Development.json` NO se suben a Git (ignorados)
- Los archivos `.env.*` NO se suben a Git (ignorados)
- Las credenciales por defecto son solo para **desarrollo local**
- Para producción, usar variables de entorno seguras

---

**Siguiente paso**: Ver `EXECUTION.md` para ejecutar el proyecto.

**Última actualización**: Octubre 23, 2025
