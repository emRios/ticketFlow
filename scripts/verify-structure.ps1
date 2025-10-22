# verify-structure.ps1
# Verifica que la estructura del monorepo TicketFlow este completa
# Uso: powershell -File scripts/verify-structure.ps1

$ErrorActionPreference = "Stop"

function Write-OK { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-FAIL { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-INFO { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath

Write-INFO "Verificando estructura del repositorio en: $repoRoot"
Write-Host ""

Push-Location $repoRoot

$requiredFiles = @(
    "contracts/README.md",
    "docs/health.md",
    "docs/rabbitmq-topology.md",
    "docs/data-model.md",
    "docs/state-machine.md",
    "docs/assignment-strategy.md",
    "docs/metrics.md",
    "deploy/docker-compose.yml",
    "deploy/env/backend.env.example",
    "deploy/env/worker.env.example",
    "deploy/env/frontend.env.example"
)

$requiredFolders = @(
    "backend/src",
    "backend/src/Api",
    "backend/src/Application",
    "backend/src/Domain",
    "backend/src/Infrastructure",
    "worker/src",
    "frontend"
)

$missingFiles = @()
$missingFolders = @()
$foundFiles = @()
$foundFolders = @()

Write-INFO "Verificando archivos requeridos..."
foreach ($file in $requiredFiles) {
    if (Test-Path $file -PathType Leaf) {
        Write-OK $file
        $foundFiles += $file
    } else {
        Write-FAIL $file
        $missingFiles += $file
    }
}

Write-Host ""

Write-INFO "Verificando carpetas requeridas..."
foreach ($folder in $requiredFolders) {
    if (Test-Path $folder -PathType Container) {
        Write-OK $folder
        $foundFolders += $folder
    } else {
        Write-FAIL $folder
        $missingFolders += $folder
    }
}

Write-Host ""
Pop-Location

Write-Host "============================================" -ForegroundColor Yellow
Write-Host "RESUMEN DE VERIFICACION" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""

$totalFiles = $requiredFiles.Count
$totalFolders = $requiredFolders.Count
$foundFilesCount = $foundFiles.Count
$foundFoldersCount = $foundFolders.Count
$missingFilesCount = $missingFiles.Count
$missingFoldersCount = $missingFolders.Count

if ($missingFilesCount -eq 0) {
    Write-Host "Archivos: $foundFilesCount/$totalFiles encontrados" -ForegroundColor Green
} else {
    Write-Host "Archivos: $foundFilesCount/$totalFiles encontrados" -ForegroundColor Yellow
}

if ($missingFoldersCount -eq 0) {
    Write-Host "Carpetas: $foundFoldersCount/$totalFolders encontradas" -ForegroundColor Green
} else {
    Write-Host "Carpetas: $foundFoldersCount/$totalFolders encontradas" -ForegroundColor Yellow
}

Write-Host ""

if ($missingFilesCount -gt 0 -or $missingFoldersCount -gt 0) {
    Write-Host "ELEMENTOS FALTANTES:" -ForegroundColor Red
    Write-Host ""
    
    if ($missingFilesCount -gt 0) {
        Write-Host "Archivos faltantes ($missingFilesCount):" -ForegroundColor Red
        foreach ($file in $missingFiles) {
            Write-Host "  - $file" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    if ($missingFoldersCount -gt 0) {
        Write-Host "Carpetas faltantes ($missingFoldersCount):" -ForegroundColor Red
        foreach ($folder in $missingFolders) {
            Write-Host "  - $folder" -ForegroundColor Red
        }
        Write-Host ""
    }
    
    Write-Host "============================================" -ForegroundColor Red
    Write-Host "VERIFICACION FALLIDA" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    exit 1
}

Write-Host "============================================" -ForegroundColor Green
Write-Host "STRUCTURE OK" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Todos los archivos y carpetas requeridos estan presentes." -ForegroundColor Green
exit 0
