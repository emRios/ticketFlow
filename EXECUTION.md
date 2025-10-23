# 🚀 Ejecución - TicketFlow

Guía para ejecutar el proyecto en diferentes modos.

---

## 🐳 Modo 1: Docker Compose (Recomendado)

### Iniciar Todo el Sistema

```powershell
cd deploy
docker-compose up -d
```

Esto inicia:
- ✅ PostgreSQL
- ✅ RabbitMQ
- ✅ API Backend
- ✅ Worker

### Iniciar Solo Frontend

```powershell
cd src\web
npm run dev
```

🌐 **Abrir**: http://localhost:5173

### Detener Todo

```powershell
cd deploy
docker-compose down
```

---

## 💻 Modo 2: Desarrollo Local

Útil para debugging del backend.

### Terminal 1: Infraestructura
```powershell
cd deploy
docker-compose up -d db rabbitmq
```

### Terminal 2: API Backend
```powershell
cd src\server\backend\Api
dotnet run
```

### Terminal 3: Worker
```powershell
cd src\server\worker
dotnet run
```

### Terminal 4: Frontend
```powershell
cd src\web
npm run dev
```

---

## 🔄 Comandos Útiles

### Ver Estado de Contenedores
```powershell
cd deploy
docker-compose ps
```

### Ver Logs en Tiempo Real
```powershell
# Todos los servicios
docker-compose logs -f

# Solo API
docker-compose logs -f api

# Solo Worker
docker-compose logs -f worker
```

### Reiniciar un Servicio
```powershell
docker-compose restart api
docker-compose restart worker
```

### Reconstruir Contenedores
```powershell
# Con cambios en código
docker-compose up --build -d

# Fresh start (borra volúmenes)
docker-compose down -v
docker-compose up --build -d
```

---

## 🧪 Ejecutar Tests

### Backend
```powershell
cd tests\Domain
dotnet test
```

### Frontend
```powershell
cd src\web
npm test
```

**Detalle completo**: Ver `TESTING.md`

---

## 🌐 Acceder a las Aplicaciones

Después de ejecutar:

| Aplicación | URL | Descripción |
|------------|-----|-------------|
| **Frontend** | http://localhost:5173 | Interfaz web principal |
| **API** | http://localhost:5076 | REST API |
| **Swagger** | http://localhost:5076/swagger | Documentación API |
| **RabbitMQ UI** | http://localhost:15672 | Monitoreo de colas |

**Credenciales RabbitMQ**: guest / guest

---

## ✅ Verificar que Todo Funciona

### Health Check Automático
```powershell
# API
curl http://localhost:5076/health

# Frontend (abrir navegador)
start http://localhost:5173
```

### Verificar Logs
```powershell
# Backend debe mostrar:
# "Application started. Press Ctrl+C to shut down."

# Frontend debe mostrar:
# "Local: http://localhost:5173/"
```

---

## 🛑 Detener Servicios

### Docker
```powershell
cd deploy
docker-compose down

# Con eliminación de volúmenes (datos)
docker-compose down -v
```

### Locales
```powershell
# Presionar Ctrl+C en cada terminal
# O cerrar las ventanas de terminal
```

---

## 🔥 Inicio Rápido (Todo en Uno)

```powershell
# 1. Levantar backend con Docker
cd deploy
docker-compose up -d

# 2. Esperar 10 segundos para que DB inicie
Start-Sleep -Seconds 10

# 3. Levantar frontend
cd ..\src\web
npm run dev

# ✅ Listo! Abrir http://localhost:5173
```

---

## 🐛 Troubleshooting

### Puerto ya en uso
```powershell
# Ver qué proceso usa el puerto
netstat -ano | findstr :5076

# Matar proceso
taskkill /PID <numero> /F
```

### Contenedor no inicia
```powershell
# Ver logs con error
docker-compose logs api

# Reiniciar
docker-compose restart api
```

### Base de datos no conecta
```powershell
# Verificar que PostgreSQL está corriendo
docker-compose ps db

# Ver logs de base de datos
docker-compose logs db

# Reconstruir volumen
docker-compose down -v
docker-compose up -d db
```

### Frontend no conecta con API
```powershell
# Verificar que API está corriendo
curl http://localhost:5076/health

# Verificar variable de entorno
cat src\web\.env.development
# Debe tener: VITE_API_BASE_URL=http://localhost:5076
```

---

## 📋 Checklist de Inicio

Antes de ejecutar, verificar:

- [ ] Docker Desktop está corriendo
- [ ] Puerto 5432 (PostgreSQL) está libre
- [ ] Puerto 5076 (API) está libre
- [ ] Puerto 5173 (Frontend) está libre
- [ ] Puerto 5672 (RabbitMQ) está libre
- [ ] Dependencias instaladas (`dotnet restore`, `npm install`)

---

## 📝 Notas

- **Primera ejecución** puede tardar más (descarga imágenes Docker)
- **Worker** procesa mensajes en background (no tiene UI)
- **Hot reload** está habilitado en frontend (auto-refresh al guardar)
- **API en desarrollo** usa Swagger para probar endpoints

---

**Ver también**:
- `INSTALLATION.md` - Instalar dependencias
- `CONFIGURATION.md` - Configurar entorno
- `TESTING.md` - Ejecutar pruebas

**Última actualización**: Octubre 23, 2025
