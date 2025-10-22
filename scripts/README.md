# Scripts de TicketFlow

Esta carpeta contiene scripts de automatización para el proyecto TicketFlow.

## 📜 Scripts Disponibles

### `New-TicketFlowProject.ps1`

Script para crear nuevos proyectos .NET con la estructura correcta, **evitando carpetas duplicadas**.

#### Uso Básico

```powershell
# Desde la raíz del proyecto
.\scripts\New-TicketFlowProject.ps1 -ProjectName "NewFeature" -ProjectType "classlib" -Category "backend"
```

#### Parámetros

| Parámetro | Requerido | Valores | Por defecto | Descripción |
|-----------|-----------|---------|-------------|-------------|
| `-ProjectName` | ✅ Sí | Cualquier string | - | Nombre del proyecto (sin prefijo "TicketFlow.") |
| `-ProjectType` | ❌ No | `classlib`, `web`, `webapi`, `worker`, `console` | `classlib` | Tipo de proyecto .NET |
| `-Category` | ❌ No | `backend`, `worker` | `backend` | Categoría del proyecto |

#### Ejemplos

**1. Crear una nueva capa en backend:**
```powershell
.\scripts\New-TicketFlowProject.ps1 -ProjectName "Contracts" -ProjectType "classlib" -Category "backend"
# Crea: src/server/backend/Contracts/TicketFlow.Contracts.csproj
```

**2. Crear un nuevo worker:**
```powershell
.\scripts\New-TicketFlowProject.ps1 -ProjectName "EmailWorker" -ProjectType "worker" -Category "worker"
# Crea: src/server/worker/EmailWorker/TicketFlow.EmailWorker.csproj
```

**3. Crear una nueva API:**
```powershell
.\scripts\New-TicketFlowProject.ps1 -ProjectName "AdminApi" -ProjectType "webapi" -Category "backend"
# Crea: src/server/backend/AdminApi/TicketFlow.AdminApi.csproj
```

#### Qué hace el script

1. ✅ **Valida** que estés en la raíz del proyecto
2. ✅ **Crea** la carpeta del proyecto en la ubicación correcta
3. ✅ **Ejecuta** `dotnet new` con `-o .` para evitar carpetas duplicadas
4. ✅ **Detecta** y corrige automáticamente carpetas duplicadas si se crean
5. ✅ **Verifica** que `Directory.Build.props` exista
6. ✅ **Compila** el proyecto para validar la configuración
7. ✅ **Verifica** que los binarios se generen en la ubicación correcta
8. ✅ **Muestra** instrucciones para los próximos pasos

#### Salida de Ejemplo

```
ℹ️  Creando proyecto: TicketFlow.NewFeature
ℹ️  Tipo: classlib
ℹ️  Categoría: backend
ℹ️  Ruta: src\server\backend\NewFeature

ℹ️  Creando carpeta: src\server\backend\NewFeature
ℹ️  Ejecutando: dotnet new classlib -n TicketFlow.NewFeature -o .
✅ Proyecto creado exitosamente
ℹ️  Compilando proyecto para verificar...
✅ Proyecto compilado correctamente
✅ Binarios generados en la ubicación correcta: src\server\backend\NewFeature\bin

✅ ======================================
✅ Proyecto creado exitosamente!
✅ ======================================
ℹ️  Ruta del proyecto: src\server\backend\NewFeature
ℹ️  Archivo .csproj: src\server\backend\NewFeature\TicketFlow.NewFeature.csproj

ℹ️  Próximos pasos:
  1. Agregar el proyecto a TicketFlow.sln:
     dotnet sln add "src\server\backend\NewFeature\TicketFlow.NewFeature.csproj"
  2. Agregar referencias a otros proyectos si es necesario:
     dotnet add "src\server\backend\NewFeature\TicketFlow.NewFeature.csproj" reference "src\server\backend\Domain\TicketFlow.Domain.csproj"
  3. Implementar la lógica del proyecto
```

#### Ventajas sobre `dotnet new` directo

| Aspecto | `dotnet new` manual | Script automatizado |
|---------|-------------------|---------------------|
| Carpetas duplicadas | ❌ Se pueden crear fácilmente | ✅ Detecta y corrige automáticamente |
| Validación | ❌ No hay | ✅ Compila y verifica binarios |
| Estructura correcta | ⚠️ Depende del usuario | ✅ Garantizada |
| Instrucciones | ❌ No hay | ✅ Muestra próximos pasos |
| Errores | ⚠️ Se descubren al compilar | ✅ Se detectan inmediatamente |

---

## 🚀 Mejores Prácticas

### ✅ USAR el script automatizado

```powershell
# Recomendado
.\scripts\New-TicketFlowProject.ps1 -ProjectName "MyFeature" -Category "backend"
```

### ❌ EVITAR crear proyectos manualmente

```powershell
# No recomendado - puede crear carpetas duplicadas
cd src\server\backend
dotnet new classlib -n TicketFlow.MyFeature
```

---

## 📚 Documentación Relacionada

- **Guía completa:** `docs/CREAR_PROYECTOS_SIN_DUPLICACION.md`
- **Historial de correcciones:** `docs/CORRECCION_CARPETAS_DUPLICADAS.md`
- **Configuración global:** `src/server/Directory.Build.props`

---

## 🆘 Solución de Problemas

### Problema: "Debe ejecutar este script desde la raíz del proyecto"

**Solución:** Navega a la raíz del proyecto donde está `TicketFlow.sln`:
```powershell
cd "C:\Users\HP\Documents\PRUEBAS\SLC TRADE\TicketFlow"
.\scripts\New-TicketFlowProject.ps1 -ProjectName "MyFeature"
```

### Problema: "La carpeta ya existe"

**Solución:** El script preguntará si deseas continuar. Responde `s` para sobrescribir o `n` para cancelar.

### Problema: Script muestra "❌ ¡PROBLEMA! Los binarios se están generando en una carpeta duplicada"

**Solución:** Esto indica que algo salió mal. Revisa manualmente:
1. El archivo `.csproj` del proyecto
2. Asegúrate de que `Directory.Build.props` existe en `src/server/`
3. Ejecuta `dotnet clean` y vuelve a compilar

---

## 🔄 Actualizaciones Futuras

Scripts planeados:

- [ ] `Validate-ProjectStructure.ps1` - Validar que no haya carpetas duplicadas
- [ ] `Fix-DuplicateFolders.ps1` - Corregir automáticamente carpetas duplicadas existentes
- [ ] `Add-ProjectReference.ps1` - Agregar referencias entre proyectos fácilmente
- [ ] `Update-AllProjects.ps1` - Actualizar propiedades en todos los .csproj

---

**Última actualización:** 22 de octubre de 2025
