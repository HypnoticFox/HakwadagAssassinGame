# Restart frontend and backend services without stopping dependencies or the cloudflared tunnel.
# Use this when dotnet watch or npm run dev don't pick up changes correctly.

param(
    [switch]$BackendOnly,
    [switch]$FrontendOnly
)

$ErrorActionPreference = "Continue"

# --- Load .env.remote if it exists ---
$envFile = Join-Path $PSScriptRoot ".env.remote"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]*)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            # Remove quotes if present
            $value = $value -replace '^["'']|["'']$', ''
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
        }
    }
}

# --- Configuration ---
$TunnelName = if ($env:CLOUDFLARED_TUNNEL) { $env:CLOUDFLARED_TUNNEL } else { "hakwadag" }
$ApiHost = if ($env:CLOUDFLARED_API_HOST) { $env:CLOUDFLARED_API_HOST } else { "hakwadag-api.jarnovos.com" }
$AppHost = if ($env:CLOUDFLARED_APP_HOST) { $env:CLOUDFLARED_APP_HOST } else { "hakwadag-app.jarnovos.com" }
$BackendPort = 5000
$FrontendPort = 5173
$CloudflaredApiUrl = "https://$ApiHost"
$CloudflaredAppUrl = "https://$AppHost"

$RepoRoot = $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "backend"
$FrontendDir = Join-Path $RepoRoot "frontend"
$BackendProject = Join-Path $BackendDir "src\HakwadagAssassinGame.Web\HakwadagAssassinGame.Web.csproj"
$LogDir = Join-Path $RepoRoot ".logs"

function Write-Status($msg) { Write-Host "[restart] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[restart] $msg" -ForegroundColor Green }

# --- Stop backend ---
if (-not $FrontendOnly) {
    Write-Status "Stopping backend..."
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match [regex]::Escape($BackendDir) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

    # Also kill any process on the backend port
    $backendProc = Get-NetTCPConnection -LocalPort $BackendPort -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
    if ($backendProc) {
        $backendProc | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    }
    Write-Ok "Backend stopped."
}

# --- Stop frontend ---
if (-not $BackendOnly) {
    Write-Status "Stopping frontend..."
    Get-CimInstance Win32_Process -Filter "Name='node.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match [regex]::Escape($FrontendDir) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

    # Also kill any process on the frontend port
    $frontendProc = Get-NetTCPConnection -LocalPort $FrontendPort -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
    if ($frontendProc) {
        $frontendProc | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    }
    Write-Ok "Frontend stopped."
}

Start-Sleep -Seconds 2

# --- Restart backend ---
if (-not $FrontendOnly) {
    Write-Status "Starting backend..."
    $env:CLOUDFLARED_FRONTEND_URL = $CloudflaredAppUrl
    $env:ASPNETCORE_HTTP_PORTS = $BackendPort
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    $backendStdout = Join-Path $LogDir "backend-stdout.log"
    $backendStderr = Join-Path $LogDir "backend-stderr.log"
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", "dotnet watch run --project `"$BackendProject`" --no-launch-profile > `"$backendStdout`" 2> `"$backendStderr`"" `
        -WindowStyle Hidden
    Write-Ok "Backend started on port $BackendPort (logs: .logs/backend-*.log)"
}

# --- Restart frontend ---
if (-not $BackendOnly) {
    Write-Status "Starting frontend..."
    $env:VITE_API_URL = $CloudflaredApiUrl
    $env:VITE_ALLOWED_HOST = $AppHost

    $frontendStdout = Join-Path $LogDir "frontend-stdout.log"
    $frontendStderr = Join-Path $LogDir "frontend-stderr.log"
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", "cd /d `"$FrontendDir`" && npm run dev -- --port $FrontendPort --host > `"$frontendStdout`" 2> `"$frontendStderr`"" `
        -WindowStyle Hidden
    Write-Ok "Frontend started on port $FrontendPort (logs: .logs/frontend-*.log)"
}

Write-Host ""
Write-Ok "Services restarted."
Write-Host ""
Write-Host "  Backend API:  $CloudflaredApiUrl" -ForegroundColor White
Write-Host "  Frontend App: $CloudflaredAppUrl" -ForegroundColor White
Write-Host ""
