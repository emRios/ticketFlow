# Script para crear nuevos proyectos .NET sin duplicar carpetas
# Uso: .\New-TicketFlowProject.ps1 -ProjectName "NewFeature" -ProjectType "classlib" -Category "backend"

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("classlib", "web", "webapi", "worker", "console")]
    [string]$ProjectType = "classlib",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("backend", "worker")]
    [string]$Category = "backend"
)

$ErrorActionPreference = "Stop"

# Colores para output
function Write-Success { Write-Host "✅ $args" -ForegroundColor Green }
function Write-Info { Write-Host "ℹ️  $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠️  $args" -ForegroundColor Yellow }
function Write-Error { Write-Host "❌ $args" -ForegroundColor Red }

# Validar que estamos en la raíz del proyecto
$rootMarker = "TicketFlow.sln"
if (-not (Test-Path $rootMarker)) {
    Write-Error "Debe ejecutar este script desde la raíz del proyecto (donde está $rootMarker)"
    exit 1
}

# Determinar la ruta base según la categoría
$basePath = switch ($Category) {
    "backend" { "src\server\backend" }
    "worker"  { "src\server\worker" }
}

# Construir el nombre completo del proyecto
$fullProjectName = "TicketFlow.$ProjectName"
$projectPath = Join-Path $basePath $ProjectName

Write-Info "Creando proyecto: $fullProjectName"
Write-Info "Tipo: $ProjectType"
Write-Info "Categoría: $Category"
Write-Info "Ruta: $projectPath"
Write-Host ""

# Verificar si la carpeta ya existe
if (Test-Path $projectPath) {
    Write-Warning "La carpeta $projectPath ya existe."
    $response = Read-Host "¿Desea continuar de todos modos? (s/N)"
    if ($response -ne "s" -and $response -ne "S") {
        Write-Info "Operación cancelada."
        exit 0
    }
}

# Crear la carpeta del proyecto
Write-Info "Creando carpeta: $projectPath"
New-Item -Path $projectPath -ItemType Directory -Force | Out-Null

# Crear el proyecto usando dotnet new con -o . (carpeta actual)
Write-Info "Ejecutando: dotnet new $ProjectType -n $fullProjectName -o ."
Push-Location $projectPath
try {
    $output = dotnet new $ProjectType -n $fullProjectName -o . 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Error al crear el proyecto: $output"
        Pop-Location
        exit 1
    }
    Write-Success "Proyecto creado exitosamente"
} finally {
    Pop-Location
}

# Verificar que NO se haya creado una carpeta duplicada
$duplicatePath = Join-Path $projectPath $fullProjectName
if (Test-Path $duplicatePath) {
    Write-Warning "Se detectó carpeta duplicada: $duplicatePath"
    Write-Info "Moviendo archivos a la carpeta correcta..."
    
    Get-ChildItem -Path $duplicatePath | ForEach-Object {
        Move-Item -Path $_.FullName -Destination $projectPath -Force
    }
    
    Remove-Item -Path $duplicatePath -Recurse -Force
    Write-Success "Carpeta duplicada corregida"
}

# Verificar que Directory.Build.props existe
$directoryBuildProps = "src\server\Directory.Build.props"
if (-not (Test-Path $directoryBuildProps)) {
    Write-Warning "No se encontró $directoryBuildProps"
    Write-Info "Creando archivo de configuración global..."
    
    $propsContent = @"
<Project>
  <PropertyGroup>
    <BaseOutputPath Condition="'`$(BaseOutputPath)' == ''">bin\</BaseOutputPath>
    <BaseIntermediateOutputPath Condition="'`$(BaseIntermediateOutputPath)' == ''">obj\</BaseIntermediateOutputPath>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
"@
    Set-Content -Path $directoryBuildProps -Value $propsContent
    Write-Success "Archivo Directory.Build.props creado"
}

# Compilar el proyecto para verificar
Write-Info "Compilando proyecto para verificar..."
$csprojPath = Join-Path $projectPath "$fullProjectName.csproj"
$buildOutput = dotnet build $csprojPath --nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Error al compilar el proyecto: $buildOutput"
} else {
    Write-Success "Proyecto compilado correctamente"
}

# Verificar ubicación de binarios
$binPath = Join-Path $projectPath "bin"
$duplicateBinPath = Join-Path $projectPath "$fullProjectName\bin"

if (Test-Path $duplicateBinPath) {
    Write-Error "¡PROBLEMA! Los binarios se están generando en una carpeta duplicada: $duplicateBinPath"
    Write-Info "Por favor revise el archivo .csproj manualmente"
} elseif (Test-Path $binPath) {
    Write-Success "Binarios generados en la ubicación correcta: $binPath"
} else {
    Write-Warning "No se encontraron binarios (esto es normal si la compilación falló)"
}

Write-Host ""
Write-Success "======================================"
Write-Success "Proyecto creado exitosamente!"
Write-Success "======================================"
Write-Info "Ruta del proyecto: $projectPath"
Write-Info "Archivo .csproj: $csprojPath"
Write-Host ""
Write-Info "Próximos pasos:"
Write-Host "  1. Agregar el proyecto a TicketFlow.sln:"
Write-Host "     dotnet sln add `"$csprojPath`""
Write-Host "  2. Agregar referencias a otros proyectos si es necesario:"
Write-Host "     dotnet add `"$csprojPath`" reference `"src\server\backend\Domain\TicketFlow.Domain.csproj`""
Write-Host "  3. Implementar la lógica del proyecto"
Write-Host ""
