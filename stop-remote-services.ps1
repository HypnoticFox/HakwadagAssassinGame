# Stop all remote development services.
# Stops cloudflared tunnel, backend, frontend, and dependencies (Redis).

param(
    [switch]$ExcludeDependencies,
    [switch]$DeleteReservedNames
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
$RepoRoot = $PSScriptRoot
$BackendDir = Join-Path $RepoRoot "backend"
$FrontendDir = Join-Path $RepoRoot "frontend"
$LogDir = Join-Path $RepoRoot ".logs"

function Write-Status($msg) { Write-Host "[remote] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[remote] $msg" -ForegroundColor Green }

# --- Stop background PowerShell jobs (includes cloudflared tunnel, backend, frontend) ---
Write-Status "Stopping background jobs..."
$jobs = Get-Job | Where-Object { $_.State -ne "Completed" }
foreach ($job in $jobs) {
    Stop-Job -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -ErrorAction SilentlyContinue
}
Write-Ok "Background jobs stopped."

# --- Delete the cloudflared tunnel (optional) ---
if ($DeleteReservedNames) {
    Write-Status "Deleting cloudflared tunnel '$TunnelName'..."
    cloudflared tunnel delete $TunnelName 2>&1 | Out-Null
    Write-Ok "Tunnel deleted. URLs will change on next start."
} else {
    Write-Status "Tunnel kept. Use -DeleteReservedNames to delete it."
}

# --- Stop backend (dotnet) ---
Write-Status "Stopping backend..."
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match [regex]::Escape($BackendDir) } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Write-Ok "Backend stopped."

# --- Stop frontend (node) ---
Write-Status "Stopping frontend..."
Get-CimInstance Win32_Process -Filter "Name='node.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match [regex]::Escape($FrontendDir) } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Write-Ok "Frontend stopped."

# --- Stop cloudflared processes for this tunnel ---
Write-Status "Stopping cloudflared processes..."
$cfConfigPath = Join-Path $LogDir "cloudflared.yml"
Get-CimInstance Win32_Process -Filter "Name='cloudflared.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match [regex]::Escape($TunnelName) -or $_.CommandLine -match [regex]::Escape($cfConfigPath) } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Write-Ok "cloudflared processes stopped."

# --- Stop dependencies (default) ---
if (-not $ExcludeDependencies) {
    Write-Status "Stopping dependencies..."
    $backendDir = Join-Path $PSScriptRoot "backend"
    try {
        docker compose -f (Join-Path $backendDir "docker-compose.yml") down 2>&1 | Out-Null
    } catch {
        # Ignore errors if docker compose fails
    }
    Write-Ok "Dependencies stopped."
} else {
    Write-Status "Dependencies kept running. Use without -ExcludeDependencies to stop them."
}

Write-Host ""
Write-Ok "All services stopped."
