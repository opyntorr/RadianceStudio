# ==============================================================================
# Deploy RadianceStudio to Rhino 8 via Yak Package Manager
# ==============================================================================
# 1. Terminates any running Rhino instances to prevent file locking on .rhp/.dll
# 2. Configures Scheme__RadianceStudio window layout & top ribbon
# 3. Builds and installs official .yak package
# 4. Verifies deployment integrity with cryptographic SHA-256 comparison
# ==============================================================================

param(
    [string]$Configuration = "Release",
    [switch]$ForceKillRhino = $true
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$SourceBin = Join-Path $RootDir "src\RadianceStudio.Rhino\bin\$Configuration\net8.0"

if (-not (Test-Path $SourceBin)) {
    Write-Error "Source directory not found: $SourceBin. Please compile in $Configuration mode first."
}

# 1. Terminate running Rhino processes if file locks exist
if ($ForceKillRhino) {
    $runningRhino = Get-Process -Name Rhino* -ErrorAction SilentlyContinue
    if ($runningRhino) {
        Write-Host "Closing $($runningRhino.Count) active Rhino process(es) to release binary file locks..." -ForegroundColor Yellow
        $runningRhino | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}

# 2. Configure Scheme workspace (hide standard toolbars, dock ribbon at top)
$configScript = Join-Path $ScriptDir "configure_scheme_workspace.ps1"
if (Test-Path $configScript) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $configScript
}

# 3. Locate Yak.exe
$YakExe = "C:\Program Files\Rhino 8\System\Yak.exe"
if (-not (Test-Path $YakExe)) {
    $YakCandidate = Join-Path $env:ProgramFiles "Rhino 8\System\Yak.exe"
    if (Test-Path $YakCandidate) {
        $YakExe = $YakCandidate
    } else {
        Write-Error "Yak.exe could not be found at: $YakExe"
    }
}

$StagingDir = Join-Path $env:TEMP "radiance_yak_staging"
if (Test-Path $StagingDir) {
    Remove-Item -Path $StagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null

$NetDir = New-Item -ItemType Directory -Path (Join-Path $StagingDir "net8.0") -Force

# 4. Write official manifest.yml
$ManifestContent = @"
---
name: RadianceStudio
version: 1.0.0
authors:
- RadianceStudio
description: Commercial-Grade Parametric Jewelry CAD Suite for Rhino 8
url: https://github.com/opyntorr/RadianceStudio
keywords:
- jewelry
- cad
- parametric
- gems
- pave
- cutters
"@
Set-Content -Path (Join-Path $StagingDir "manifest.yml") -Value $ManifestContent -Encoding UTF8

# 5. Copy compiled assemblies, rui, and rhp into net8.0 staging
$FilesToCopy = Get-ChildItem -Path "$SourceBin\*" -Include *.rhp, *.dll, *.pdb, *.deps.json, *.rui
foreach ($file in $FilesToCopy) {
    Copy-Item -Path $file.FullName -Destination $NetDir -Force
}

# Also copy rui to staging root
$ruiSource = Join-Path $SourceBin "RadianceStudioToolbar.rui"
if (-not (Test-Path $ruiSource)) {
    $ruiSource = Join-Path $RootDir "src\RadianceStudio.Rhino\RadianceStudioToolbar.rui"
}
if (Test-Path $ruiSource) {
    Copy-Item -Path $ruiSource -Destination $StagingDir -Force
    Copy-Item -Path $ruiSource -Destination $NetDir -Force
}

# 6. Build .yak package and clean existing package destination
$DeployedDir = Join-Path $env:APPDATA "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0"
if (Test-Path $DeployedDir) {
    try {
        Remove-Item -Path $DeployedDir -Recurse -Force -ErrorAction Stop
    } catch {
        Write-Warning "Failed to delete old package directory: $_"
    }
}

$OldLocation = Get-Location
try {
    Set-Location $StagingDir
    & $YakExe build
    $YakPackage = Get-ChildItem -Path $StagingDir -Filter "*.yak" | Select-Object -First 1
    if (-not $YakPackage) {
        throw "Yak build failed to produce a .yak package file."
    }

    # Install .yak package officially into Rhino 8
    Write-Host "Installing $($YakPackage.Name) into Rhino 8..." -ForegroundColor Cyan
    & $YakExe install $YakPackage.FullName
}
finally {
    Set-Location $OldLocation
    try { Remove-Item -Path $StagingDir -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

$DeployedRhp = Join-Path $env:APPDATA "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudio.Rhino.rhp"
if (-not (Test-Path $DeployedRhp)) {
    throw "Deployment failed: Deployed rhp not found at '$DeployedRhp'."
}

# 7. Cryptographic SHA-256 verification
$sourceRhp = Join-Path $SourceBin "RadianceStudio.Rhino.rhp"
$sourceHash = (Get-FileHash -Path $sourceRhp -Algorithm SHA256).Hash
$deployedHash = (Get-FileHash -Path $DeployedRhp -Algorithm SHA256).Hash

if ($sourceHash -ne $deployedHash) {
    throw "DEPLOYMENT INTEGRITY FAILURE: Source hash ($sourceHash) does not match deployed hash ($deployedHash)!"
}

Write-Host "[PASS] Cryptographic SHA-256 Match Verified: $deployedHash" -ForegroundColor Green
Write-Host "[OK] Officially installed RadianceStudio 1.0.0 to Rhino 8 Package Manager:" -ForegroundColor Green
Write-Host "     $DeployedRhp" -ForegroundColor Gray
