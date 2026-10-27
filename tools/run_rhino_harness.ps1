# ==============================================================================
# RadianceStudio Real In-Process Rhino 8 Test Harness
# ==============================================================================
# Executes tests directly inside the live Rhino 8 process under the
# isolated RadianceStudio scheme, auditing actual geometry, viewport entities,
# units, tolerances, and watertight STL generation.
# ==============================================================================

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  Phase 4: Real Headless Rhino 8 Integration Harness" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Locate Rhino 8
$RhinoExe = "C:\Program Files\Rhino 8\System\Rhino.exe"
if (-not (Test-Path $RhinoExe)) {
    $RhinoExe = Join-Path $env:ProgramFiles "Rhino 8\System\Rhino.exe"
    if (-not (Test-Path $RhinoExe)) {
        Write-Error "Rhino 8 executable not found at: $RhinoExe"
    }
}

# 2. Deploy latest Release package and verify SHA-256 match
Write-Host "[1/4] Ensuring latest binaries are deployed and unlocked..." -ForegroundColor Gray
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ScriptDir "deploy_to_rhino_packages.ps1") -Configuration Release

# 3. Clean prior audit token
$TempDir = [System.IO.Path]::GetTempPath()
$AuditReportPath = Join-Path $TempDir "radiance_harness_audit.json"
if (Test-Path $AuditReportPath) {
    Remove-Item -Path $AuditReportPath -Force
}

# 4. Launch Rhino 8 with live audit script
Write-Host "[2/4] Launching Rhino 8 under RadianceStudio scheme to execute _RadHarnessAudit..." -ForegroundColor Gray
$runScript = "-_RadHarnessAudit -_Exit N"
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $RhinoExe
$psi.Arguments = "/scheme=`"RadianceStudio`" /nosplash /runscript=`"$runScript`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $false

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$proc = [System.Diagnostics.Process]::Start($psi)

$timeoutSeconds = 35
$auditFound = $false
for ($i = 0; $i -lt ($timeoutSeconds * 2); $i++) {
    Start-Sleep -Milliseconds 500
    if (Test-Path $AuditReportPath) {
        Start-Sleep -Milliseconds 500
        $auditFound = $true
        break
    }
    if ($proc.HasExited) {
        break
    }
}

$elapsed = $stopwatch.Elapsed.TotalSeconds

# Cleanly terminate the headless test instance
try {
    if (-not $proc.HasExited) {
        $proc.Kill()
    }
} catch { }

if (-not $auditFound -and -not (Test-Path $AuditReportPath)) {
    throw "Rhino 8 test harness timed out after $timeoutSeconds seconds! Possible deadlock in Rhino UI thread."
}

Write-Host "[3/4] Rhino in-process audit completed in $([math]::Round($elapsed, 2))s." -ForegroundColor Gray

# 5. Read and validate audit report
Write-Host "[4/4] Reading and verifying in-process audit report..." -ForegroundColor Gray
if (-not (Test-Path $AuditReportPath)) {
    throw "Audit report not found at '$AuditReportPath'. _RadHarnessAudit did not complete execution inside Rhino."
}

$auditJson = Get-Content -Path $AuditReportPath -Raw -Encoding UTF8
$report = $auditJson | ConvertFrom-Json

Write-Host "`n--- In-Process Rhino 8 Audit Results ---" -ForegroundColor DarkCyan
foreach ($stg in $report.stages) {
    $color = if ($stg.status -eq "PASS") { "Green" } else { "Red" }
    Write-Host "  [$($stg.status)] $($stg.stage): $($stg.details)" -ForegroundColor $color
}

if ($report.overallStatus -ne "PASS") {
    throw "Real Rhino 8 integration harness failed one or more stages! See details above."
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  [PASS] Real In-Process Rhino 8 Harness Verified 100%!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
