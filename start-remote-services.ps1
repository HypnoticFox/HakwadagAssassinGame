# Remote development setup using a cloudflared tunnel.
# Starts Redis, backend, frontend, and a single named cloudflared tunnel with
# ingress rules for both the API and frontend hostnames.

param(
    [switch]$SkipDependencies,
    [switch]$SkipCleanup,
    [switch]$Detach
)

$ErrorActionPreference = "Stop"

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

# Track background jobs for cleanup
$jobs = @()

function Write-Status($msg) { Write-Host "[remote] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[remote] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[remote] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[remote] $msg" -ForegroundColor Red }

# Runs a native command silently, ignoring stderr and exit codes.
# (In PowerShell 5.1, native stderr redirected with 2>&1 becomes error records
# that are subject to $ErrorActionPreference; temporarily relaxing it prevents
# false failures from commands like "cloudflared tunnel create" on an existing tunnel.)
function Invoke-Silent([scriptblock]$Command) {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & $Command 2>&1 | Out-Null
    } finally {
        $ErrorActionPreference = $prevEap
    }
}

# Captures native stdout+stderr as a single string without throwing.
function Get-NativeOutput([scriptblock]$Command) {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        return (& $Command 2>&1 | Out-String)
    } finally {
        $ErrorActionPreference = $prevEap
    }
}

function Stop-AllJobs {
    Write-Status "Stopping all services..."
    foreach ($job in $jobs) {
        if ($job -and $job.State -ne "Completed") {
            Stop-Job -Job $job -ErrorAction SilentlyContinue
            Remove-Job -Job $job -ErrorAction SilentlyContinue
        }
    }

    # Stop cloudflared processes for this tunnel
    Write-Status "Stopping cloudflared tunnel..."
    $cfConfigPath = Join-Path $LogDir "cloudflared.yml"
    Get-CimInstance Win32_Process -Filter "Name='cloudflared.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match [regex]::Escape($TunnelName) -or $_.CommandLine -match [regex]::Escape($cfConfigPath) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

    # Stop backend and frontend processes (only those related to this project)
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match [regex]::Escape($BackendDir) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Get-CimInstance Win32_Process -Filter "Name='node.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match [regex]::Escape($FrontendDir) } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

    # Stop dependencies
    Write-Status "Stopping dependencies..."
    try {
        docker compose -f (Join-Path $BackendDir "docker-compose.yml") down 2>&1 | Out-Null
    } catch {
        # Ignore errors if docker compose fails
    }

    Write-Ok "All services stopped."
}

# --- Prerequisite checks ---
Write-Status "Checking prerequisites..."

if (-not (Get-Command "cloudflared" -ErrorAction SilentlyContinue)) {
    Write-Err "cloudflared is not installed."
    Write-Host ""
    Write-Host 'Install cloudflared:' -ForegroundColor White
    Write-Host '  1. Download from https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/'
    Write-Host '  2. Add cloudflared.exe to your PATH'
    Write-Host ""
    exit 1
}

$certFile = Join-Path $env:USERPROFILE ".cloudflared\cert.pem"
if (-not (Test-Path $certFile)) {
    Write-Err "cloudflared is not authorized for your Cloudflare account."
    Write-Host 'Run: cloudflared tunnel login'
    exit 1
}

if (-not (Get-Command "docker" -ErrorAction SilentlyContinue)) {
    Write-Err "Docker is not installed. Required for Redis."
    exit 1
}

if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Err ".NET SDK is not installed."
    exit 1
}

if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Err "Node.js is not installed."
    exit 1
}

Write-Ok "All prerequisites met."

# --- Register cleanup on Ctrl+C (skip if detaching) ---
if (-not $SkipCleanup -and -not $Detach) {
    Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-AllJobs } -ErrorAction SilentlyContinue
    $null = Register-EngineEvent -SourceIdentifier "PowerShell.ProcessExit" -Action { Stop-AllJobs } -ErrorAction SilentlyContinue
}

# --- Start dependencies ---
if (-not $SkipDependencies) {
    Write-Status "Starting dependencies..."
    docker compose -f (Join-Path $BackendDir "docker-compose.yml") up -d
    Write-Ok "Dependencies started (Redis on localhost:6379)"
} else {
    Write-Warn "Skipping dependencies (use -SkipDependencies to skip)"
}

# --- Set up the cloudflared tunnel (idempotent) ---
Write-Status "Setting up cloudflared tunnel '$TunnelName'..."

# Create the tunnel if it doesn't exist yet (fails silently if it already exists)
Invoke-Silent { cloudflared tunnel create $TunnelName }

# Route the API and app hostnames to the tunnel (safe to run repeatedly)
Invoke-Silent { cloudflared tunnel route dns $TunnelName $ApiHost }
Invoke-Silent { cloudflared tunnel route dns $TunnelName $AppHost }

# Determine the tunnel ID from `cloudflared tunnel info` output...
$TunnelId = $null
$tunnelInfo = Get-NativeOutput { cloudflared tunnel info $TunnelName }
if ($tunnelInfo -match '([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})') {
    $TunnelId = $matches[1]
}

# ...falling back to scanning the credentials files in ~/.cloudflared
if (-not $TunnelId) {
    $credsDir = Join-Path $env:USERPROFILE ".cloudflared"
    $credsFiles = Get-ChildItem -Path (Join-Path $credsDir "*.json") -ErrorAction SilentlyContinue
    foreach ($file in $credsFiles) {
        try {
            $creds = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
            if ($creds.TunnelName -eq $TunnelName -and $creds.TunnelID) {
                $TunnelId = $creds.TunnelID
                break
            }
        } catch {
            # Ignore files that aren't tunnel credentials
        }
    }
}

if (-not $TunnelId) {
    Write-Err "Could not determine the tunnel ID for '$TunnelName'."
    Write-Host 'Run "cloudflared tunnel list" and check ~/.cloudflared for the credentials file.'
    exit 1
}

Write-Ok "Tunnel ID: $TunnelId"

# --- Generate the cloudflared config file ---
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$configPath = Join-Path $LogDir "cloudflared.yml"
$credsDir = Join-Path $env:USERPROFILE ".cloudflared"
$credsFile = "$($credsDir -replace '\\','/')/$TunnelId.json"

$config = @"
tunnel: $TunnelName
credentials-file: $credsFile

ingress:
  - hostname: $ApiHost
    service: http://localhost:$BackendPort
  - hostname: $AppHost
    service: http://localhost:$FrontendPort
  - service: http_status:404
"@
Set-Content -Path $configPath -Value $config -Encoding UTF8
Write-Ok "cloudflared config written to $configPath"

# --- Start backend ---
Write-Status "Starting backend on port $BackendPort..."

if ($Detach) {
    # Use cmd /c with shell redirection to avoid file handle issues
    $env:ASPNETCORE_HTTP_PORTS = $BackendPort
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:CLOUDFLARED_FRONTEND_URL = $CloudflaredAppUrl
    $backendStdout = Join-Path $LogDir "backend-stdout.log"
    $backendStderr = Join-Path $LogDir "backend-stderr.log"
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", "dotnet watch run --project `"$BackendProject`" --no-launch-profile > `"$backendStdout`" 2> `"$backendStderr`"" `
        -WindowStyle Hidden
    Write-Ok "Backend started on port $BackendPort (logs: .logs/backend-*.log)"
} else {
    $env:CLOUDFLARED_FRONTEND_URL = $CloudflaredAppUrl
    $env:ASPNETCORE_HTTP_PORTS = $BackendPort

    $backendJob = Start-Job -ScriptBlock {
        param($project, $port, $frontendUrl)
        $env:CLOUDFLARED_FRONTEND_URL = $frontendUrl
        $env:ASPNETCORE_HTTP_PORTS = $port
        dotnet watch run --project $project --no-launch-profile 2>&1
    } -ArgumentList $BackendProject, $BackendPort, $CloudflaredAppUrl
    $jobs += $backendJob
    Write-Ok "Backend starting..."
}

# --- Start frontend ---
Write-Status "Starting frontend on port $FrontendPort..."

if ($Detach) {
    $env:VITE_API_URL = $CloudflaredApiUrl
    $env:VITE_ALLOWED_HOST = $AppHost
    $frontendStdout = Join-Path $LogDir "frontend-stdout.log"
    $frontendStderr = Join-Path $LogDir "frontend-stderr.log"
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", "cd /d `"$FrontendDir`" && npm run dev -- --port $FrontendPort --host > `"$frontendStdout`" 2> `"$frontendStderr`"" `
        -WindowStyle Hidden
    Write-Ok "Frontend started on port $FrontendPort (logs: .logs/frontend-*.log)"
} else {
    $env:VITE_API_URL = $CloudflaredApiUrl
    $env:VITE_ALLOWED_HOST = $AppHost

    $frontendJob = Start-Job -ScriptBlock {
        param($dir, $apiUrl, $port, $allowedHost)
        $env:VITE_API_URL = $apiUrl
        $env:VITE_ALLOWED_HOST = $allowedHost
        Set-Location $dir
        npm run dev -- --port $port --host 2>&1
    } -ArgumentList $FrontendDir, $CloudflaredApiUrl, $FrontendPort, $AppHost
    $jobs += $frontendJob
    Write-Ok "Frontend starting..."
}

# --- Wait for services to be ready ---
Write-Status "Waiting for services to start..."
Start-Sleep -Seconds 5

# --- Start cloudflared tunnel ---
Write-Status "Starting cloudflared tunnel..."

if ($Detach) {
    # Use cmd /c with shell redirection to avoid file handle issues
    $cfStdout = Join-Path $LogDir "cloudflared-stdout.log"
    $cfStderr = Join-Path $LogDir "cloudflared-stderr.log"
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList "/c", "cloudflared tunnel --config `"$configPath`" run > `"$cfStdout`" 2> `"$cfStderr`"" `
        -WindowStyle Hidden
    Write-Ok "cloudflared tunnel started (logs: .logs/cloudflared-*.log)"
} else {
    $tunnelJob = Start-Job -ScriptBlock {
        param($config)
        cloudflared tunnel --config $config run 2>&1
    } -ArgumentList $configPath
    $jobs += $tunnelJob
    Write-Ok "cloudflared tunnel starting..."
}

Start-Sleep -Seconds 3

# --- Print URLs ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Remote development environment ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Backend API:  $CloudflaredApiUrl" -ForegroundColor White
Write-Host "  Frontend App: $CloudflaredAppUrl" -ForegroundColor White
Write-Host ""
Write-Host "  Local URLs:" -ForegroundColor Gray
Write-Host "    Backend:  http://localhost:$BackendPort" -ForegroundColor Gray
Write-Host "    Frontend: http://localhost:$FrontendPort" -ForegroundColor Gray
Write-Host "    Redis:    localhost:6379" -ForegroundColor Gray
Write-Host ""

# --- Wait for Ctrl+C or exit if detached ---
if ($Detach) {
    Write-Ok "Services started in background. Use stop-remote-services.ps1 to stop them."
    exit 0
}

Write-Host "  Press Ctrl+C to stop all services." -ForegroundColor Yellow
Write-Host ""

try {
    while ($true) {
        Start-Sleep -Seconds 1
        # Check if jobs are still running
        $allStopped = $true
        foreach ($job in $jobs) {
            if ($job.State -eq "Running") { $allStopped = $false; break }
        }
        if ($allStopped) {
            Write-Warn "All background jobs have stopped unexpectedly."
            break
        }
    }
} finally {
    if (-not $SkipCleanup) {
        Stop-AllJobs
    }
}
