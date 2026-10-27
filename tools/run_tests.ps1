# ==============================================================================
# Local Verification Runner for RadianceStudio
# ==============================================================================
# Runs:
# 1. Clean-Room Anti-Contamination Linter (zero proprietary tokens or banned APIs)
# 2. Complete Automated xUnit Test Suite (1,471 tests in Release /warnaserror)
# 3. Headless MCP E2E Agent Pipeline (9-stage jewelry lifecycle and STL export verification)
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$SlnPath = Join-Path $RootDir "RadianceStudio.sln"
$ScannerPath = Join-Path $ScriptDir "check_clean_room.ps1"
$McpAgentPath = Join-Path $ScriptDir "run_e2e_mcp_agent.ps1"

Write-Host "`n>>> [1/3] Running Clean-Room Anti-Contamination Linter..." -ForegroundColor Cyan
& powershell -File $ScannerPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Clean-Room violations found. Fix before committing." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [2/3] Running Automated Unit Tests ($Configuration)..." -ForegroundColor Cyan
$DotnetCmd = if (Test-Path "C:\Users\omarp\.dotnet\dotnet.exe") { "C:\Users\omarp\.dotnet\dotnet.exe" } else { "dotnet" }
& $DotnetCmd test $SlnPath --configuration $Configuration /warnaserror --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] One or more unit tests failed." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [3/3] Running Headless MCP E2E Agent Pipeline..." -ForegroundColor Cyan
& powershell -File $McpAgentPath -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Headless MCP E2E Agent Verification failed." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [SUCCESS] All Clean-Room checks, Unit Tests, and MCP E2E Agent passed! Ready to commit/push branch." -ForegroundColor Green
exit 0
