# Script para reorganizar TicketFlow como monorepo
# Ejecutar desde la raíz del proyecto

$root = "c:\Users\HP\Documents\PRUEBAS\SLC TRADE\TicketFlow"
Set-Location $root

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Reorganizando TicketFlow como Monorepo" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Mover archivos del frontend a frontend/
Write-Host "[1/9] Moviendo index.html..." -ForegroundColor Yellow
Move-Item -Path "index.html" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[2/9] Moviendo package.json y package-lock.json..." -ForegroundColor Yellow
Move-Item -Path "package.json" -Destination "frontend\" -Force -ErrorAction SilentlyContinue
Move-Item -Path "package-lock.json" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[3/9] Moviendo vite.config.ts..." -ForegroundColor Yellow
Move-Item -Path "vite.config.ts" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[4/9] Moviendo tsconfig.json..." -ForegroundColor Yellow
Move-Item -Path "tsconfig.json" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[5/9] Moviendo script.js..." -ForegroundColor Yellow
Move-Item -Path "script.js" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[6/9] Moviendo style.css..." -ForegroundColor Yellow
Move-Item -Path "style.css" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[7/9] Moviendo carpeta apps/..." -ForegroundColor Yellow
Move-Item -Path "apps" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[8/9] Moviendo carpeta packages/..." -ForegroundColor Yellow
Move-Item -Path "packages" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host "[9/9] Moviendo node_modules/ (puede tardar)..." -ForegroundColor Yellow
Move-Item -Path "node_modules" -Destination "frontend\" -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Movimiento completado!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Cyan
Write-Host "1. cd frontend" -ForegroundColor White
Write-Host "2. npm install (si es necesario)" -ForegroundColor White
Write-Host "3. npm run dev" -ForegroundColor White
Write-Host ""
Write-Host "Ver frontend\README.move-notes.md para TODOs" -ForegroundColor Yellow
