# ==============================================================================
# Local Verification Runner for RadianceStudio
# ==============================================================================
# Runs the Clean-Room Anti-Contamination check and the complete xUnit test suite.
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$SlnPath = Join-Path $RootDir "RadianceStudio\RadianceStudio.sln"
$ScannerPath = Join-Path $ScriptDir "check_clean_room.ps1"

Write-Host "`n>>> [1/2] Running Clean-Room Anti-Contamination Linter..." -ForegroundColor Cyan
& powershell -File $ScannerPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Clean-Room violations found. Fix before committing." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [2/2] Running Automated Unit Tests ($Configuration)..." -ForegroundColor Cyan
& "C:\Users\omarp\.dotnet\dotnet.exe" test $SlnPath --configuration $Configuration --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] One or more unit tests failed." -ForegroundColor Red
    exit 1
}

Write-Host "`n>>> [SUCCESS] All Clean-Room checks and Unit Tests passed! Ready to commit/push branch." -ForegroundColor Green
exit 0
