# dev-up.ps1
# Levanta la infraestructura de desarrollo (PostgreSQL + RabbitMQ)
# Uso: powershell -File scripts/dev-up.ps1

$ErrorActionPreference = "Stop"

function Write-OK { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-FAIL { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-INFO { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-WAIT { param($msg) Write-Host "[WAIT] $msg" -ForegroundColor Yellow }

# Banner
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TicketFlow - Development Environment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Obtener raiz del repositorio
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
$deployDir = Join-Path $repoRoot "deploy"

Write-INFO "Repositorio: $repoRoot"
Write-INFO "Deploy dir: $deployDir"
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
    Write-Host ""
    Write-Host "Solucion:" -ForegroundColor Yellow
    Write-Host "  1. Instalar Docker Desktop: https://docs.docker.com/desktop/install/windows-install/" -ForegroundColor Yellow
    Write-Host "  2. Asegurar que Docker Desktop esta corriendo" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host ""

# Cambiar al directorio deploy
Push-Location $deployDir

try {
    # Levantar servicios de infraestructura
    Write-INFO "Levantando servicios de infraestructura (PostgreSQL + RabbitMQ)..."
    Write-Host ""
    
    docker compose --profile infra up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-FAIL "Error al levantar servicios con docker compose"
        Pop-Location
        exit 1
    }
    
    Write-Host ""
    Write-OK "Servicios iniciados"
    Write-Host ""
    
    # Esperar a que los servicios esten healthy
    Write-INFO "Esperando a que los servicios esten listos..."
    Write-Host ""
    
    $maxAttempts = 30
    $attemptDelay = 2
    $services = @("ticketflow-postgres", "ticketflow-rabbitmq")
    $healthyServices = @{}
    
    foreach ($service in $services) {
        $healthyServices[$service] = $false
    }
    
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $allHealthy = $true
        
        foreach ($service in $services) {
            if ($healthyServices[$service]) {
                continue
            }
            
            # Verificar estado del contenedor
            $state = docker inspect --format='{{.State.Health.Status}}' $service 2>$null
            
            if ($state -eq "healthy") {
                if (-not $healthyServices[$service]) {
                    Write-OK "$service esta healthy"
                    $healthyServices[$service] = $true
                }
            } elseif ($state -eq "unhealthy") {
                Write-FAIL "$service esta unhealthy"
                Write-Host ""
                Write-Host "Ver logs con:" -ForegroundColor Yellow
                Write-Host "  docker compose logs $service" -ForegroundColor Yellow
                Write-Host ""
                Pop-Location
                exit 1
            } else {
                $allHealthy = $false
            }
        }
        
        if ($allHealthy -and ($healthyServices.Values | Where-Object { $_ -eq $true }).Count -eq $services.Count) {
            break
        }
        
        if ($attempt -lt $maxAttempts) {
            Write-WAIT "Esperando health checks... (intento $attempt/$maxAttempts)"
            Start-Sleep -Seconds $attemptDelay
        }
    }
    
    # Verificar que todos esten healthy
    $notHealthy = $healthyServices.GetEnumerator() | Where-Object { $_.Value -eq $false }
    if ($notHealthy.Count -gt 0) {
        Write-Host ""
        Write-FAIL "Los siguientes servicios no estan healthy despues de $maxAttempts intentos:"
        foreach ($item in $notHealthy) {
            Write-Host "  - $($item.Key)" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "Ver logs con:" -ForegroundColor Yellow
        Write-Host "  docker compose logs" -ForegroundColor Yellow
        Write-Host ""
        Pop-Location
        exit 1
    }
    
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  Infraestructura lista!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    
    # Mostrar URLs y credenciales
    Write-Host "Servicios disponibles:" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "PostgreSQL:" -ForegroundColor Yellow
    Write-Host "  Host:     localhost"
    Write-Host "  Port:     5432"
    Write-Host "  Database: ticketflow"
    Write-Host "  User:     ticketflow_user"
    Write-Host "  Password: ticketflow_pass"
    Write-Host ""
    
    Write-Host "RabbitMQ:" -ForegroundColor Yellow
    Write-Host "  AMQP:     amqp://localhost:5672"
    Write-Host "  UI:       http://localhost:15672"
    Write-Host "  User:     ticketflow_user"
    Write-Host "  Password: ticketflow_pass"
    Write-Host ""
    
    Write-Host "Comandos utiles:" -ForegroundColor Cyan
    Write-Host "  Ver logs:       docker compose logs -f"
    Write-Host "  Ver estado:     docker compose ps"
    Write-Host "  Detener:        docker compose --profile infra down"
    Write-Host "  Reiniciar todo: docker compose --profile infra restart"
    Write-Host ""
    
    # Verificar conectividad basica
    Write-INFO "Verificando conectividad..."
    
    # Test PostgreSQL
    $pgTest = docker exec ticketflow-postgres pg_isready -U ticketflow_user -d ticketflow 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-OK "PostgreSQL: Aceptando conexiones"
    } else {
        Write-FAIL "PostgreSQL: No responde"
    }
    
    # Test RabbitMQ
    $rabbitTest = docker exec ticketflow-rabbitmq rabbitmq-diagnostics ping 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-OK "RabbitMQ: Operacional"
    } else {
        Write-FAIL "RabbitMQ: No responde"
    }
    
    Write-Host ""
    Write-Host "Siguiente paso:" -ForegroundColor Cyan
    Write-Host "  1. (Futuro) Levantar API: cd backend/src/Api && dotnet run"
    Write-Host "  2. (Futuro) Levantar Worker: cd worker/src && dotnet run"
    Write-Host "  3. (Futuro) Levantar Frontend: cd frontend && npm run dev"
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
