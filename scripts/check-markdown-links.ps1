# check-markdown-links.ps1
# Verifica enlaces Markdown en documentacion del monorepo TicketFlow
# Uso: powershell -File scripts/check-markdown-links.ps1

$ErrorActionPreference = "Stop"

function Write-OK { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-FAIL { param($msg) Write-Host "[FAIL] $msg" -ForegroundColor Red }
function Write-INFO { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-WARN { param($msg) Write-Host "[WARN] $msg" -ForegroundColor Yellow }

# Convertir texto de encabezado a slug estilo GitHub
function ConvertTo-GithubSlug {
    param([string]$text)
    
    # Eliminar caracteres especiales y convertir a minusculas
    $slug = $text.ToLower()
    
    # Reemplazar espacios por guiones
    $slug = $slug -replace '\s+', '-'
    
    # Eliminar caracteres no permitidos: . , / ( ) [ ] : ' "
    $slug = $slug -replace '[.,/\(\)\[\]:''"]', ''
    
    # Eliminar guiones multiples
    $slug = $slug -replace '-+', '-'
    
    # Eliminar guiones al inicio y final
    $slug = $slug.Trim('-')
    
    return $slug
}

# Extraer encabezados de un archivo Markdown
function Get-MarkdownHeadings {
    param([string]$filePath)
    
    if (-not (Test-Path $filePath)) {
        return @()
    }
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $headings = @()
    
    # Regex para encabezados: # Titulo, ## Titulo, etc.
    $pattern = '(?m)^(#{1,6})\s+(.+)$'
    $matches = [regex]::Matches($content, $pattern)
    
    foreach ($match in $matches) {
        $level = $match.Groups[1].Value.Length
        $text = $match.Groups[2].Value.Trim()
        $slug = ConvertTo-GithubSlug $text
        
        $headings += @{
            Level = $level
            Text = $text
            Slug = $slug
        }
    }
    
    return $headings
}

# Verificar si un anchor existe en un archivo
function Test-AnchorExists {
    param(
        [string]$filePath,
        [string]$anchor
    )
    
    $headings = Get-MarkdownHeadings $filePath
    
    # Comparar con slug generado
    $anchorSlug = ConvertTo-GithubSlug $anchor
    
    foreach ($heading in $headings) {
        if ($heading.Slug -eq $anchorSlug) {
            return $true
        }
    }
    
    return $false
}

# Extraer enlaces Markdown de un archivo
function Get-MarkdownLinks {
    param([string]$filePath)
    
    $content = Get-Content $filePath -Raw -Encoding UTF8
    $links = @()
    
    # Regex para enlaces: [texto](url)
    $pattern = '\[([^\]]+)\]\(([^)]+)\)'
    $matches = [regex]::Matches($content, $pattern)
    
    foreach ($match in $matches) {
        $text = $match.Groups[1].Value
        $url = $match.Groups[2].Value
        
        $links += @{
            Text = $text
            Url = $url
            LineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        }
    }
    
    return $links
}

# Obtener raiz del repositorio
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath

Write-INFO "Verificando enlaces Markdown en: $repoRoot"
Write-Host ""

Push-Location $repoRoot

# Buscar todos los archivos .md en docs/ y contracts/
$markdownFiles = @()
$markdownFiles += Get-ChildItem -Path "docs" -Filter "*.md" -Recurse -ErrorAction SilentlyContinue
$markdownFiles += Get-ChildItem -Path "contracts" -Filter "*.md" -Recurse -ErrorAction SilentlyContinue

if ($markdownFiles.Count -eq 0) {
    Write-WARN "No se encontraron archivos Markdown en docs/ o contracts/"
    Pop-Location
    exit 0
}

Write-INFO "Encontrados $($markdownFiles.Count) archivos Markdown"
Write-Host ""

$totalErrors = 0
$filesWithErrors = @()

foreach ($mdFile in $markdownFiles) {
    $relativePath = $mdFile.FullName.Replace($repoRoot, "").TrimStart('\').TrimStart('/')
    $links = Get-MarkdownLinks $mdFile.FullName
    
    if ($links.Count -eq 0) {
        continue
    }
    
    $fileErrors = @()
    
    foreach ($link in $links) {
        $url = $link.Url
        
        # Ignorar enlaces externos (http, https, mailto)
        if ($url -match '^https?://' -or $url -match '^mailto:') {
            continue
        }
        
        # Separar ruta y anchor
        $parts = $url -split '#', 2
        $linkPath = $parts[0]
        $anchor = if ($parts.Count -gt 1) { $parts[1] } else { $null }
        
        # Si linkPath esta vacio, es solo un anchor en el mismo archivo
        if ([string]::IsNullOrWhiteSpace($linkPath)) {
            if ($anchor) {
                $exists = Test-AnchorExists $mdFile.FullName $anchor
                if (-not $exists) {
                    $fileErrors += @{
                        Link = $url
                        Line = $link.LineNumber
                        Reason = "missing anchor '#$anchor' in same file"
                    }
                }
            }
            continue
        }
        
        # Resolver ruta relativa
        $mdDir = Split-Path -Parent $mdFile.FullName
        $targetPath = Join-Path $mdDir $linkPath
        $targetPath = [System.IO.Path]::GetFullPath($targetPath)
        
        # Verificar si el archivo existe
        if (-not (Test-Path $targetPath -PathType Leaf)) {
            $fileErrors += @{
                Link = $url
                Line = $link.LineNumber
                Reason = "missing file '$linkPath'"
            }
            continue
        }
        
        # Si tiene anchor, verificar que exista
        if ($anchor) {
            $exists = Test-AnchorExists $targetPath $anchor
            if (-not $exists) {
                $fileErrors += @{
                    Link = $url
                    Line = $link.LineNumber
                    Reason = "missing anchor '#$anchor' in file '$linkPath'"
                }
            }
        }
    }
    
    # Reportar errores del archivo
    if ($fileErrors.Count -gt 0) {
        Write-FAIL "$relativePath ($($fileErrors.Count) enlaces invalidos)"
        foreach ($error in $fileErrors) {
            Write-Host "  Linea $($error.Line): [$($error.Link)] - $($error.Reason)" -ForegroundColor Red
        }
        Write-Host ""
        
        $filesWithErrors += $relativePath
        $totalErrors += $fileErrors.Count
    } else {
        Write-OK "$relativePath (todos los enlaces validos)"
    }
}

Pop-Location

Write-Host ""
Write-Host "============================================" -ForegroundColor Yellow
Write-Host "RESUMEN DE VERIFICACION" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Archivos verificados: $($markdownFiles.Count)"
Write-Host "Archivos con errores: $($filesWithErrors.Count)"
Write-Host "Total de enlaces invalidos: $totalErrors"
Write-Host ""

if ($totalErrors -gt 0) {
    Write-Host "============================================" -ForegroundColor Red
    Write-Host "LINKS VERIFICATION FAILED" -ForegroundColor Red
    Write-Host "============================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Archivos con errores:" -ForegroundColor Red
    foreach ($file in $filesWithErrors) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    exit 2
}

Write-Host "============================================" -ForegroundColor Green
Write-Host "LINKS OK" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Todos los enlaces Markdown son validos." -ForegroundColor Green
exit 0
