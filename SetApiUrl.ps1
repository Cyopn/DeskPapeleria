# Script para configurar la URL de la API de DeskApp
# Uso: .\SetApiUrl.ps1 -Url "https://tu-url-ngrok.ngrok-free.dev/api"

param(
    [Parameter(Mandatory=$true)]
    [string]$Url
)

Write-Host "Configurando URL de la API..." -ForegroundColor Cyan

# Validar que la URL sea válida
if (-not ($Url -match '^https?://')) {
    Write-Host "Error: La URL debe comenzar con http:// o https://" -ForegroundColor Red
    exit 1
}

# Opción 1: Variable de entorno para la sesión actual
$env:DESK_API_URL = $Url
Write-Host "? Variable de entorno configurada para la sesión actual" -ForegroundColor Green
Write-Host "  DESK_API_URL = $Url" -ForegroundColor Gray

# Opción 2: Actualizar appsettings.json
$appSettingsPath = Join-Path $PSScriptRoot "DeskApp\appsettings.json"

if (Test-Path $appSettingsPath) {
    try {
        $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
        $json.ApiSettings.BaseUrl = $Url
        $json | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
        Write-Host "? appsettings.json actualizado" -ForegroundColor Green
    } catch {
        Write-Host "? No se pudo actualizar appsettings.json: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "? No se encontró appsettings.json en: $appSettingsPath" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Configuración completada. URL configurada:" -ForegroundColor Cyan
Write-Host "  $Url" -ForegroundColor White
Write-Host ""
Write-Host "Nota: La variable de entorno de sesión solo estará disponible en esta ventana de PowerShell." -ForegroundColor Yellow
Write-Host "Para configurarla permanentemente, ejecuta: .\SetApiUrl.ps1 -Url '$Url' -Permanent" -ForegroundColor Yellow
