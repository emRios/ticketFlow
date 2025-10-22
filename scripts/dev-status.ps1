#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Muestra el estado de la infraestructura local (PostgreSQL + RabbitMQ).

.DESCRIPTION
    Ejecuta docker compose ps desde deploy/ y verifica el health de postgres y rabbitmq.
    Si algún servicio no está healthy, imprime tips de troubleshooting.
    Retorna exit 0 si ambos healthy, exit 1 si alguno no lo está o hay error.

.EXAMPLE
    pwsh -File scripts/dev-status.ps1
#>

$ErrorActionPreference = "Stop"

# =============================================================================
# Funciones auxiliares
# =============================================================================

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Banner {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Cyan
    Write-Host ""
}

function Test-DockerRunning {
    try {
        $null = docker version 2>&1
        return $true
    } catch {
        return $false
    }
}

# =============================================================================
# Main
# =============================================================================

Write-Banner "Estado de Infraestructura Local"

# Verificar que Docker está corriendo
if (-not (Test-DockerRunning)) {
    Write-ColorMessage "[FAIL] Docker no está corriendo o no está instalado." "Red"
    Write-ColorMessage "[INFO] Inicia Docker Desktop y vuelve a intentarlo." "Yellow"
    exit 1
}

Write-ColorMessage "[INFO] Docker está corriendo. Verificando servicios..." "Cyan"
Write-Host ""

# Cambiar al directorio deploy
$deployPath = Join-Path $PSScriptRoot ".." "deploy"
if (-not (Test-Path $deployPath)) {
    Write-ColorMessage "[FAIL] No se encontró el directorio deploy/." "Red"
    exit 1
}

Push-Location $deployPath

try {
    # Ejecutar docker compose ps con formato personalizado
    Write-ColorMessage "[INFO] Ejecutando: docker compose ps --profile infra" "Cyan"
    Write-Host ""
    
    # Obtener listado de contenedores
    $composePs = docker compose ps --profile infra --format json 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorMessage "[FAIL] Error al ejecutar docker compose ps." "Red"
        Write-ColorMessage $composePs "Red"
        exit 1
    }

    # Parsear JSON (cada línea es un objeto JSON)
    $containers = @()
    if ($composePs) {
        $composePs -split "`n" | Where-Object { $_.Trim() -ne "" } | ForEach-Object {
            $containers += $_ | ConvertFrom-Json
        }
    }

    if ($containers.Count -eq 0) {
        Write-ColorMessage "[WARN] No hay contenedores de infraestructura corriendo." "Yellow"
        Write-Host ""
        Write-ColorMessage "Tips:" "Yellow"
        Write-ColorMessage "  - Ejecuta: pwsh -File scripts/dev-up.ps1" "White"
        Write-ColorMessage "  - Verifica que docker-compose.yml tenga servicios con profile 'infra'" "White"
        exit 1
    }

    # Mostrar tabla de servicios
    Write-ColorMessage "Servicios encontrados:" "Green"
    Write-Host ""
    
    $containers | ForEach-Object {
        $name = $_.Name
        $status = $_.Status
        $state = $_.State
        
        $stateColor = switch ($state) {
            "running" { "Green" }
            "exited" { "Red" }
            default { "Yellow" }
        }
        
        Write-Host "  Service: " -NoNewline
        Write-ColorMessage $_.Service "Cyan"
        Write-Host "    Name:   $name"
        Write-Host "    State:  " -NoNewline
        Write-ColorMessage "$state" $stateColor
        Write-Host "    Status: $status"
        Write-Host ""
    }

    # Verificar health de PostgreSQL y RabbitMQ
    $postgresHealthy = $false
    $rabbitmqHealthy = $false
    $allHealthy = $true

    Write-ColorMessage "Verificando health checks..." "Cyan"
    Write-Host ""

    # PostgreSQL
    $pgContainer = $containers | Where-Object { $_.Service -eq "postgres" }
    if ($pgContainer) {
        if ($pgContainer.State -eq "running") {
            # Verificar health con docker inspect
            $pgHealth = docker inspect --format='{{.State.Health.Status}}' $pgContainer.Name 2>&1
            
            if ($pgHealth -eq "healthy") {
                Write-ColorMessage "[OK] PostgreSQL: healthy" "Green"
                $postgresHealthy = $true
            } else {
                Write-ColorMessage "[FAIL] PostgreSQL: $pgHealth" "Red"
                $allHealthy = $false
                
                Write-Host ""
                Write-ColorMessage "Tips PostgreSQL:" "Yellow"
                Write-ColorMessage "  - Verifica que el puerto 5432 no esté ocupado:" "White"
                Write-ColorMessage "    netstat -ano | findstr :5432" "Gray"
                Write-ColorMessage "  - Revisa logs: docker compose logs postgres" "White"
                Write-ColorMessage "  - Credenciales: POSTGRES_USER=ticketflow, POSTGRES_PASSWORD=dev123" "White"
                Write-Host ""
            }
        } else {
            Write-ColorMessage "[FAIL] PostgreSQL: not running (state=$($pgContainer.State))" "Red"
            $allHealthy = $false
        }
    } else {
        Write-ColorMessage "[FAIL] PostgreSQL: contenedor no encontrado" "Red"
        $allHealthy = $false
    }

    # RabbitMQ
    $mqContainer = $containers | Where-Object { $_.Service -eq "rabbitmq" }
    if ($mqContainer) {
        if ($mqContainer.State -eq "running") {
            # Verificar health con docker inspect
            $mqHealth = docker inspect --format='{{.State.Health.Status}}' $mqContainer.Name 2>&1
            
            if ($mqHealth -eq "healthy") {
                Write-ColorMessage "[OK] RabbitMQ: healthy" "Green"
                $rabbitmqHealthy = $true
            } else {
                Write-ColorMessage "[FAIL] RabbitMQ: $mqHealth" "Red"
                $allHealthy = $false
                
                Write-Host ""
                Write-ColorMessage "Tips RabbitMQ:" "Yellow"
                Write-ColorMessage "  - Verifica que los puertos 5672 y 15672 no estén ocupados:" "White"
                Write-ColorMessage "    netstat -ano | findstr :5672" "Gray"
                Write-ColorMessage "    netstat -ano | findstr :15672" "Gray"
                Write-ColorMessage "  - Revisa logs: docker compose logs rabbitmq" "White"
                Write-ColorMessage "  - Credenciales: user=guest, password=guest" "White"
                Write-ColorMessage "  - Management UI: http://localhost:15672" "White"
                Write-Host ""
            }
        } else {
            Write-ColorMessage "[FAIL] RabbitMQ: not running (state=$($mqContainer.State))" "Red"
            $allHealthy = $false
        }
    } else {
        Write-ColorMessage "[FAIL] RabbitMQ: contenedor no encontrado" "Red"
        $allHealthy = $false
    }

    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Cyan
    
    if ($allHealthy -and $postgresHealthy -and $rabbitmqHealthy) {
        Write-ColorMessage "  STATUS: HEALTHY - Todos los servicios están operativos" "Green"
        Write-Host ("=" * 70) -ForegroundColor Cyan
        Write-Host ""
        
        Write-ColorMessage "Endpoints disponibles:" "Cyan"
        Write-ColorMessage "  - PostgreSQL: localhost:5432 (ticketflow/dev123)" "White"
        Write-ColorMessage "  - RabbitMQ AMQP: localhost:5672 (guest/guest)" "White"
        Write-ColorMessage "  - RabbitMQ UI: http://localhost:15672 (guest/guest)" "White"
        Write-Host ""
        
        exit 0
    } else {
        Write-ColorMessage "  STATUS: UNHEALTHY - Hay servicios con problemas" "Red"
        Write-Host ("=" * 70) -ForegroundColor Cyan
        Write-Host ""
        
        Write-ColorMessage "Comandos útiles:" "Yellow"
        Write-ColorMessage "  - Ver logs: docker compose logs -f [postgres|rabbitmq]" "White"
        Write-ColorMessage "  - Reiniciar: pwsh -File scripts/dev-down.ps1; pwsh -File scripts/dev-up.ps1" "White"
        Write-ColorMessage "  - Purgar datos: pwsh -File scripts/dev-down.ps1 -Purge" "White"
        Write-Host ""
        
        exit 1
    }

} finally {
    Pop-Location
}
