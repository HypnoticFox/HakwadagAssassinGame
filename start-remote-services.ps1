# Remote development setup using zrok2 tunnels.
# Starts Redis, backend, frontend, and creates public zrok2 shares.

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
$ApiShareName = if ($env:ZROK_API_NAME) { $env:ZROK_API_NAME } else { "hakwadag-api" }
$AppShareName = if ($env:ZROK_APP_NAME) { $env:ZROK_APP_NAME } else { "hakwadag-app" }
$BackendPort = 5000
$FrontendPort = 5173
$ZrokApiUrl = "https://$ApiShareName.shares.zrok.io"
$ZrokAppUrl = "https://$AppShareName.shares.zrok.io"

$RepoRoot = $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "backend"
$FrontendDir = Join-Path $RepoRoot "frontend"
$BackendProject = Join-Path $BackendDir "src\HakwadagAssassinGame.Web\HakwadagAssassinGame.Web.csproj"

# Track background jobs for cleanup
$jobs = @()

function Write-Status($msg) { Write-Host "[remote] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[remote] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[remote] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[remote] $msg" -ForegroundColor Red }

function Stop-AllJobs {
    Write-Status "Stopping all services..."
    foreach ($job in $jobs) {
        if ($job -and $job.State -ne "Completed") {
            Stop-Job -Job $job -ErrorAction SilentlyContinue
            Remove-Job -Job $job -ErrorAction SilentlyContinue
        }
    }

    # Delete active zrok2 shares using zrok2 commands
    Write-Status "Deleting active zrok2 shares..."
    $sharesJson = zrok2 list shares --json 2>&1
    if ($sharesJson) {
        try {
            $data = $sharesJson | ConvertFrom-Json
            foreach ($share in $data.shares) {
                if ($share.shareToken) {
                    Write-Status "  Deleting share $($share.shareToken)..."
                    zrok2 delete share $share.shareToken 2>&1 | Out-Null
                }
            }
            Write-Ok "Active shares deleted."
        } catch {
            Write-Status "Could not parse shares list."
        }
    }

    # Stop backend and frontend processes
    Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

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

if (-not (Get-Command "zrok2" -ErrorAction SilentlyContinue)) {
    Write-Err "zrok2 is not installed."
    Write-Host ""
    Write-Host 'Install zrok2:' -ForegroundColor White
    Write-Host '  1. Download from https://github.com/openziti/zrok/releases'
    Write-Host '  2. Extract zrok2.exe to a folder on your PATH'
    Write-Host '  3. Run: zrok2 invite'
    Write-Host '  4. Run: zrok2 enable <your-token>'
    Write-Host ""
    exit 1
}

$zrokDir = Join-Path $env:USERPROFILE ".zrok2"
if (-not (Test-Path $zrokDir)) {
    Write-Err "zrok2 is not enabled. Run:"
    Write-Host '  zrok2 invite'
    Write-Host '  zrok2 enable <your-account-token>'
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

# --- Create zrok2 reserved names (if they don't exist) ---
Write-Status "Checking zrok2 reserved names..."
$existingNames = zrok2 list names --json 2>&1
$existingNamesList = @()
if ($existingNames) {
    try {
        $namesData = $existingNames | ConvertFrom-Json
        $existingNamesList = $namesData | ForEach-Object { $_.name }
    } catch {
        Write-Status "Could not parse names list, will attempt to create..."
    }
}

if ($existingNamesList -notcontains $ApiShareName) {
    Write-Status "Creating reserved name: $ApiShareName"
    zrok2 create name $ApiShareName 2>&1 | Out-Null
} else {
    Write-Status "Reserved name already exists: $ApiShareName"
}

if ($existingNamesList -notcontains $AppShareName) {
    Write-Status "Creating reserved name: $AppShareName"
    zrok2 create name $AppShareName 2>&1 | Out-Null
} else {
    Write-Status "Reserved name already exists: $AppShareName"
}

Write-Ok "Reserved names: $ApiShareName, $AppShareName"

# --- Start backend ---
Write-Status "Starting backend on port $BackendPort..."

if ($Detach) {
    # Use Start-Process for true background execution that survives script exit
    $env:ZROK_FRONTEND_URL = $ZrokAppUrl
    $env:ASPNETCORE_HTTP_PORTS = $BackendPort
    Start-Process -FilePath "dotnet" -ArgumentList "watch", "run", "--project", $BackendProject, "--no-launch-profile" -WindowStyle Hidden
    Write-Ok "Backend started on port $BackendPort"
} else {
    $env:ZROK_FRONTEND_URL = $ZrokAppUrl
    $env:ASPNETCORE_HTTP_PORTS = $BackendPort

    $backendJob = Start-Job -ScriptBlock {
        param($project, $port, $frontendUrl)
        $env:ZROK_FRONTEND_URL = $frontendUrl
        $env:ASPNETCORE_HTTP_PORTS = $port
        dotnet watch run --project $project --no-launch-profile 2>&1
    } -ArgumentList $BackendProject, $BackendPort, $ZrokAppUrl
    $jobs += $backendJob
    Write-Ok "Backend starting..."
}

# --- Start frontend ---
Write-Status "Starting frontend on port $FrontendPort..."

if ($Detach) {
    $env:VITE_API_URL = $ZrokApiUrl
    $env:VITE_ALLOWED_HOST = "$AppShareName.shares.zrok.io"
    Start-Process -FilePath "npm.cmd" -ArgumentList "run", "dev", "--", "--port", $FrontendPort, "--host" -WorkingDirectory $FrontendDir -WindowStyle Hidden
    Write-Ok "Frontend started on port $FrontendPort"
} else {
    $env:VITE_API_URL = $ZrokApiUrl
    $env:VITE_ALLOWED_HOST = "$AppShareName.shares.zrok.io"

    $frontendJob = Start-Job -ScriptBlock {
        param($dir, $apiUrl, $port, $allowedHost)
        $env:VITE_API_URL = $apiUrl
        $env:VITE_ALLOWED_HOST = $allowedHost
        Set-Location $dir
        npm run dev -- --port $port --host 2>&1
    } -ArgumentList $FrontendDir, $ZrokApiUrl, $FrontendPort, "$AppShareName.shares.zrok.io"
    $jobs += $frontendJob
    Write-Ok "Frontend starting..."
}

# --- Wait for services to be ready ---
Write-Status "Waiting for services to start..."
Start-Sleep -Seconds 5

# --- Start zrok2 shares ---
Write-Status "Creating zrok2 public shares..."

if ($Detach) {
    # Use Start-Process for true background execution
    Start-Process -FilePath "zrok2" -ArgumentList "share", "public", "localhost:$BackendPort", "-n", "public:$ApiShareName" -WindowStyle Hidden
    Start-Process -FilePath "zrok2" -ArgumentList "share", "public", "localhost:$FrontendPort", "-n", "public:$AppShareName" -WindowStyle Hidden
    Write-Ok "zrok2 shares created"
} else {
    $apiShareJob = Start-Job -ScriptBlock {
        param($port, $name)
        zrok2 share public "localhost:$port" -n "public:$name" 2>&1
    } -ArgumentList $BackendPort, $ApiShareName
    $jobs += $apiShareJob

    $appShareJob = Start-Job -ScriptBlock {
        param($port, $name)
        zrok2 share public "localhost:$port" -n "public:$name" 2>&1
    } -ArgumentList $FrontendPort, $AppShareName
    $jobs += $appShareJob
}

Start-Sleep -Seconds 3

# --- Print URLs ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Remote development environment ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Backend API:  $ZrokApiUrl" -ForegroundColor White
Write-Host "  Frontend App: $ZrokAppUrl" -ForegroundColor White
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
