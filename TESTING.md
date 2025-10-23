# 🧪 Guía de Pruebas - TicketFlow

Instrucciones rápidas para ejecutar pruebas en cada módulo.

---

## 📦 Backend Tests (.NET)

### Ubicación
```
tests/Domain/TicketFlow.Domain.Tests/
```

### Ejecutar Tests

```powershell
# Desde la raíz del proyecto
cd tests\Domain
dotnet test

# Con detalles
dotnet test --verbosity normal

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"
```

### Resultado Esperado
- ✅ **79 tests** en ~3-4 segundos
- ✅ **~95% cobertura** del Domain layer

### Qué se prueba
- ✅ Entidades: `Ticket`, `User`, `TicketActivity`
- ✅ Value Objects: `TicketStatus`
- ✅ Enums: `TicketPriority`
- ✅ Eventos de dominio
- ✅ Validaciones y reglas de negocio

---

## 🌐 Frontend Tests (TypeScript)

### Ubicación
```
src/web/apps/demo-vanilla/__tests__/
```

### Ejecutar Tests

```powershell
# Desde src/web
cd src\web
npm test

# Modo watch (desarrollo)
npm test -- --watch

# Con cobertura
npm run test:coverage

# UI interactiva
npm run test:ui
```

### Resultado Esperado
- ✅ **52 tests** en ~1-2 segundos
- ✅ **~94% cobertura** del código frontend

### Qué se prueba
- ✅ API Client: HTTP requests, auth tokens, error handling
- ✅ Tickets API: CRUD operations, status normalization
- ✅ Session: localStorage, token/user management
- ✅ Helpers: Formateo de fechas y utilidades

---

## 🚀 Ejecutar Todas las Pruebas

### Opción 1: Manualmente

```powershell
# Terminal 1: Backend tests
cd tests\Domain
dotnet test

# Terminal 2: Frontend tests
cd src\web
npm test -- --run
```

### Opción 2: Script PowerShell

```powershell
# Crear y ejecutar script
cd "c:\Users\HP\Documents\PRUEBAS\SLC TRADE\TicketFlow"

# Backend
Write-Host "=== Backend Tests ===" -ForegroundColor Cyan
cd tests\Domain
dotnet test

# Frontend
Write-Host "`n=== Frontend Tests ===" -ForegroundColor Cyan
cd ..\..\src\web
npm test -- --run
```

---

## 📊 Ver Cobertura

### Backend
```powershell
cd tests\Domain
dotnet test --collect:"XPlat Code Coverage"

# Reporte en: TestResults/*/coverage.cobertura.xml
```

### Frontend
```powershell
cd src\web
npm run test:coverage

# Reporte HTML en: coverage/index.html
start coverage\index.html
```

---

## 🐛 Troubleshooting

### Backend: "dotnet: command not found"
```powershell
# Instalar .NET 8 SDK
winget install Microsoft.DotNet.SDK.8
```

### Frontend: Tests fallan
```powershell
# Reinstalar dependencias
cd src\web
Remove-Item node_modules -Recurse -Force
npm install
npm test
```

### Limpiar caché de tests
```powershell
# Backend
dotnet clean
dotnet build

# Frontend
cd src\web
npm run test -- --clearCache
```

---

## 📈 Métricas Actuales

| Módulo | Tests | Cobertura | Tiempo |
|--------|-------|-----------|--------|
| **Backend** | 79 | ~95% | 3.6s |
| **Frontend** | 52 | ~94% | 1.6s |
| **Total** | **131** | **~94.5%** | **~5s** |

---

## ✅ Verificación Rápida

```powershell
# Verificar que todo funciona
cd tests\Domain ; dotnet test ; cd ..\..\src\web ; npm test -- --run
```

Si ambos muestran ✅, las pruebas están correctas.

---

**Última actualización**: Octubre 23, 2025
