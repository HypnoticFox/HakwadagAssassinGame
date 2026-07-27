# Stop all remote development services.
# Kills zrok2 shares, backend, frontend, and dependencies (Redis).

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

function Write-Status($msg) { Write-Host "[remote] $msg" -ForegroundColor Cyan }
function Write-Ok($msg) { Write-Host "[remote] $msg" -ForegroundColor Green }

# --- Stop background PowerShell jobs (includes zrok2 shares, backend, frontend) ---
Write-Status "Stopping background jobs..."
$jobs = Get-Job | Where-Object { $_.State -ne "Completed" }
foreach ($job in $jobs) {
    Stop-Job -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -ErrorAction SilentlyContinue
}
Write-Ok "Background jobs stopped."

# --- Delete active zrok2 shares using zrok2 commands ---
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
} else {
    Write-Status "No active shares found."
}

# --- Delete reserved zrok2 names (optional) ---
if ($DeleteReservedNames) {
    $ApiShareName = if ($env:ZROK_API_NAME) { $env:ZROK_API_NAME } else { "hakwadag-api" }
    $AppShareName = if ($env:ZROK_APP_NAME) { $env:ZROK_APP_NAME } else { "hakwadag-app" }

    Write-Status "Deleting reserved zrok2 names..."
    zrok2 delete name $ApiShareName 2>&1 | Out-Null
    zrok2 delete name $AppShareName 2>&1 | Out-Null
    Write-Ok "Reserved names deleted. URLs will change on next start."
} else {
    Write-Status "Reserved names kept. Use -DeleteReservedNames to remove them."
}

# --- Stop backend (dotnet) ---
Write-Status "Stopping backend..."
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Ok "Backend stopped."

# --- Stop frontend (node) ---
Write-Status "Stopping frontend..."
Get-Process -Name "node" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Ok "Frontend stopped."

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
