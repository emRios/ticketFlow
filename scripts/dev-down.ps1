# dev-down.ps1
# Detiene la infraestructura de desarrollo (PostgreSQL + RabbitMQ)
# Uso: powershell -File scripts/dev-down.ps1 [-Purge]
#
# -Purge: Elimina tambien los volumenes (datos persistentes)

param(
    [switch]$Purge
)

$ErrorActionPreference = "Stop"

function Write-OK { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-FAIL { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-INFO { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-WARN { param($msg) Write-Host "[WARN] $msg" -ForegroundColor Yellow }

# Banner
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TicketFlow - Detener Infraestructura" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Obtener raiz del repositorio
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$deployDir = Join-Path $repoRoot "deploy"

Write-INFO "Repositorio: $repoRoot"
Write-INFO "Deploy dir: $deployDir"

if ($Purge) {
    Write-WARN "Flag -Purge detectado: Se eliminaran los volumenes (datos persistentes)"
}

Write-Host ""

# Verificar que existe docker-compose.yml
$composeFile = Join-Path $deployDir "docker-compose.yml"
if (-not (Test-Path $composeFile)) {
    Write-FAIL "No se encontro docker-compose.yml en $deployDir"
    exit 1
}

# Verificar que Docker esta corriendo
Write-INFO "Verificando Docker..."
try {
    $dockerVersion = docker version --format '{{.Server.Version}}' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker no esta corriendo"
    }
    Write-OK "Docker version: $dockerVersion"
} catch {
    Write-FAIL "Docker Desktop no esta corriendo o no esta instalado"
    exit 1
}

Write-Host ""

# Cambiar al directorio deploy
Push-Location $deployDir

try {
    # Construir comando
    $downCmd = "docker compose --profile infra down"
    
    if ($Purge) {
        $downCmd += " --volumes"
        Write-INFO "Deteniendo servicios y eliminando volumenes..."
    } else {
        Write-INFO "Deteniendo servicios (volumenes se mantienen)..."
    }
    
    Write-Host ""
    
    # Ejecutar comando
    Invoke-Expression $downCmd
    
    if ($LASTEXITCODE -ne 0) {
        Write-FAIL "Error al detener servicios con docker compose"
        Pop-Location
        exit 1
    }
    
    Write-Host ""
    
    # Verificar que los contenedores se detuvieron
    $runningContainers = docker ps --filter "name=ticketflow-" --format "{{.Names}}" 2>$null
    
    if ($runningContainers) {
        Write-WARN "Algunos contenedores todavia estan corriendo:"
        $runningContainers | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
        Write-Host ""
    } else {
        Write-OK "Todos los contenedores de TicketFlow fueron detenidos"
    }
    
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Infra detenida" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    
    if ($Purge) {
        Write-WARN "Los volumenes fueron eliminados (datos perdidos)"
        Write-Host ""
        Write-Host "Siguiente inicio creara bases de datos vacias" -ForegroundColor Yellow
    } else {
        Write-INFO "Los volumenes fueron preservados (datos intactos)"
        Write-Host ""
        Write-Host "Para eliminar tambien los volumenes, ejecuta:" -ForegroundColor Cyan
        Write-Host "  powershell -File scripts/dev-down.ps1 -Purge" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "Para volver a levantar la infraestructura:" -ForegroundColor Cyan
    Write-Host "  powershell -File scripts/dev-up.ps1" -ForegroundColor Cyan
    Write-Host ""
    
    Pop-Location
    exit 0
    
} catch {
    Write-FAIL "Error inesperado: $_"
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    Write-Host ""
    Pop-Location
    exit 1
}
