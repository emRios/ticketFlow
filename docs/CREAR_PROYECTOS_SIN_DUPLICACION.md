# Guía: Crear Nuevos Proyectos .NET sin Duplicar Carpetas

## ⚠️ Problema a Evitar

Al crear proyectos con `dotnet new`, si no se especifica correctamente la opción `-o` (output), se crea una estructura duplicada:

```
❌ INCORRECTO:
worker/
└── TicketFlow.Worker/          ← Carpeta duplicada creada automáticamente
    ├── TicketFlow.Worker.csproj
    └── Program.cs
```

## ✅ Soluciones Implementadas

### 1. **Directory.Build.props** (Aplicado Automáticamente)

Hemos creado `src/server/Directory.Build.props` que establece estas propiedades para TODOS los proyectos:

```xml
<BaseOutputPath>bin\</BaseOutputPath>
<BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
```

Esto garantiza que los archivos compilados (`bin/`, `obj/`) siempre se generen en la carpeta raíz del proyecto, no en subcarpetas duplicadas.

### 2. **Propiedades en .csproj** (Ya Configurado)

Todos los archivos `.csproj` existentes tienen estas líneas:

```xml
<PropertyGroup>
  <!-- Evitar crear carpetas duplicadas con el nombre del proyecto -->
  <BaseOutputPath>bin\</BaseOutputPath>
  <BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
</PropertyGroup>
```

### 3. **Comandos Correctos para Crear Proyectos**

## 📋 Cómo Crear Nuevos Proyectos Correctamente

### Opción A: Especificar carpeta de salida como actual (`.`)

```powershell
# 1. Crear la carpeta manualmente
New-Item -Path "src\server\backend\NewProject" -ItemType Directory

# 2. Navegar a la carpeta
cd src\server\backend\NewProject

# 3. Crear el proyecto en la carpeta actual (.)
dotnet new classlib -n TicketFlow.NewProject -o .
```

✅ Resultado:
```
src/server/backend/NewProject/
├── TicketFlow.NewProject.csproj  ✅ Correcto
└── Class1.cs
```

### Opción B: Especificar la ruta completa al crear

```powershell
# Crear el proyecto directamente especificando la carpeta de salida
dotnet new classlib -n TicketFlow.NewProject -o src\server\backend\NewProject
```

✅ Resultado:
```
src/server/backend/NewProject/
├── TicketFlow.NewProject.csproj  ✅ Correcto
└── Class1.cs
```

### Opción C: Usar template con estructura existente

```powershell
# Copiar un proyecto existente como plantilla
Copy-Item -Path "src\server\backend\Domain" -Destination "src\server\backend\NewProject" -Recurse
cd src\server\backend\NewProject

# Renombrar el .csproj
Rename-Item "TicketFlow.Domain.csproj" "TicketFlow.NewProject.csproj"

# Limpiar binarios
Remove-Item -Path "bin","obj" -Recurse -Force -ErrorAction SilentlyContinue

# Editar el .csproj y actualizar referencias
```

## ❌ Comandos a EVITAR

### ❌ NO hacer esto:

```powershell
# INCORRECTO - Crea carpeta duplicada
cd src\server\backend
dotnet new classlib -n TicketFlow.NewProject
# Resultado: src/server/backend/TicketFlow.NewProject/TicketFlow.NewProject.csproj ❌
```

```powershell
# INCORRECTO - Nombre de carpeta no coincide con nombre de proyecto
New-Item -Path "src\server\backend\MyProject" -ItemType Directory
cd src\server\backend\MyProject
dotnet new classlib -n TicketFlow.DifferentName
# Resultado: src/server/backend/MyProject/TicketFlow.DifferentName/ ❌
```

## 🔍 Verificación Post-Creación

Después de crear un proyecto, verifica:

### 1. Estructura de carpetas correcta

```powershell
cd src\server\backend\NewProject
Get-ChildItem

# ✅ Debería mostrar:
# TicketFlow.NewProject.csproj
# Class1.cs (o archivos .cs)
# bin/ (después de compilar)
# obj/ (después de compilar)

# ❌ NO debería mostrar:
# TicketFlow.NewProject/ (carpeta adicional)
```

### 2. Compilar y verificar ubicación de binarios

```powershell
dotnet build

# ✅ Los binarios deberían estar en:
# src/server/backend/NewProject/bin/Debug/net8.0/

# ❌ NO en:
# src/server/backend/NewProject/TicketFlow.NewProject/bin/
```

### 3. Verificar propiedades del .csproj

Abre el archivo `.csproj` y asegúrate de que tenga:

```xml
<PropertyGroup>
  <BaseOutputPath>bin\</BaseOutputPath>
  <BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
</PropertyGroup>
```

**NOTA:** Esto debería aplicarse automáticamente gracias a `Directory.Build.props`, pero verifica por si acaso.

## 🚨 Si Ya Se Creó una Carpeta Duplicada

Si accidentalmente creaste una carpeta duplicada, sigue estos pasos:

```powershell
# Ejemplo: src/server/backend/NewProject/TicketFlow.NewProject/ existe

cd src\server\backend\NewProject

# 1. Mover todos los archivos a la carpeta padre
Move-Item -Path "TicketFlow.NewProject\*" -Destination "." -Force

# 2. Eliminar la carpeta duplicada
Remove-Item -Path "TicketFlow.NewProject" -Recurse -Force

# 3. Verificar
Get-ChildItem
```

## 📚 Referencias Adicionales

- Documentación oficial: [Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)
- Archivo de configuración: `src/server/Directory.Build.props`
- Historial de correcciones: `docs/CORRECCION_CARPETAS_DUPLICADAS.md`

## ✅ Checklist al Crear Proyectos

- [ ] Crear carpeta manualmente PRIMERO con el nombre correcto
- [ ] Navegar a la carpeta con `cd`
- [ ] Usar `dotnet new -o .` para crear en la carpeta actual
- [ ] Verificar que NO se haya creado una subcarpeta duplicada
- [ ] Compilar con `dotnet build` y verificar que `bin/` esté en la raíz del proyecto
- [ ] Agregar el proyecto a `TicketFlow.sln` con la ruta correcta
- [ ] Actualizar referencias en otros proyectos si es necesario

---

**Última actualización:** 22 de octubre de 2025
