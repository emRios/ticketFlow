# 📦 Instalación - TicketFlow

Guía para instalar todas las dependencias del proyecto.

---

## 🔧 Prerrequisitos

Instala el software base necesario:

### 1. Docker Desktop
```powershell
# Descargar e instalar desde:
https://www.docker.com/products/docker-desktop/

# Verificar instalación
docker --version
docker-compose --version
```

### 2. .NET 8 SDK
```powershell
# Opción 1: WinGet
winget install Microsoft.DotNet.SDK.8

# Opción 2: Descargar desde
https://dotnet.microsoft.com/download/dotnet/8.0

# Verificar instalación
dotnet --version
# Debe mostrar: 8.x.x
```

### 3. Node.js (v20+)
```powershell
# Opción 1: WinGet
winget install OpenJS.NodeJS.LTS

# Opción 2: Descargar desde
https://nodejs.org/

# Verificar instalación
node --version  # v20.x.x o superior
npm --version   # 10.x.x o superior
```

---

## 📥 Instalar Dependencias del Proyecto

### Backend (.NET)
```powershell
# Desde la raíz del proyecto
cd src\server\backend\Api
dotnet restore

cd ..\..\..\worker
dotnet restore

# Verificar
dotnet build
```

### Frontend (Node.js)
```powershell
# Desde la raíz del proyecto
cd src\web
npm install

# Verificar
npm run build
```

### Tests
```powershell
# Backend tests
cd tests\Domain
dotnet restore

# Frontend tests (ya incluido en npm install anterior)
cd src\web
npm install
```

---

## 🐳 Verificar Docker

```powershell
# Iniciar Docker Desktop (si no está corriendo)
# Luego verificar:

docker ps
# Debe mostrar lista vacía o contenedores corriendo

docker-compose version
# Debe mostrar versión 2.x o superior
```

---

## ✅ Verificación Completa

Ejecuta estos comandos para confirmar que todo está instalado:

```powershell
# .NET
dotnet --version

# Node.js y npm
node --version
npm --version

# Docker
docker --version
docker-compose --version

# Dependencias del proyecto
cd src\server\backend\Api
dotnet build

cd ..\..\..\web
npm run build
```

Si todos los comandos funcionan sin errores, ✅ **la instalación está completa**.

---

## 🔄 Reinstalar Dependencias

### Si hay problemas con Backend:
```powershell
cd src\server\backend\Api
dotnet clean
dotnet restore
dotnet build
```

### Si hay problemas con Frontend:
```powershell
cd src\web
Remove-Item node_modules -Recurse -Force
Remove-Item package-lock.json -Force
npm install
```

---

## 📝 Notas

- **Docker Desktop** debe estar corriendo antes de usar `docker-compose`
- **.NET 8 SDK** (no solo Runtime) es necesario para compilar
- **Node.js v20+** es requerido para las últimas features de Vite
- Las dependencias de **node_modules** pueden ocupar ~500MB

---

**Siguiente paso**: Ver `CONFIGURATION.md` para configurar el proyecto.

**Última actualización**: Octubre 23, 2025
