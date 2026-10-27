# ==============================================================================
# Deploy RadianceStudio to Rhino 8 via Yak Package Manager
# ==============================================================================
# Packages RadianceStudio using Rhino's official Yak package manager CLI and
# installs it legitimately so Rhino 8's PackageManager never removes it.
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$SourceBin = Join-Path $RootDir "src\RadianceStudio.Rhino\bin\$Configuration\net8.0"

if (-not (Test-Path $SourceBin)) {
    Write-Error "Source directory not found: $SourceBin. Please compile in $Configuration mode first."
}

# Locate Yak.exe
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

# 1. Write official manifest.yml
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

# 2. Copy compiled assemblies and rhp into net8.0 staging
$FilesToCopy = Get-ChildItem -Path "$SourceBin\*" -Include *.rhp, *.dll, *.pdb, *.deps.json
foreach ($file in $FilesToCopy) {
    Copy-Item -Path $file.FullName -Destination $NetDir -Force
}

# 3. Build .yak package
$OldLocation = Get-Location
try {
    Set-Location $StagingDir
    & $YakExe build
    $YakPackage = Get-ChildItem -Path $StagingDir -Filter "*.yak" | Select-Object -First 1
    if (-not $YakPackage) {
        throw "Yak build failed to produce a .yak package file."
    }

    # 4. Install .yak package officially into Rhino 8
    Write-Host "Installing $($YakPackage.Name) into Rhino 8..." -ForegroundColor Cyan
    & $YakExe install $YakPackage.FullName
}
finally {
    Set-Location $OldLocation
    try { Remove-Item -Path $StagingDir -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

$DeployedRhp = Join-Path $env:APPDATA "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudio.Rhino.rhp"
if (Test-Path $DeployedRhp) {
    Write-Host "[OK] Officially installed RadianceStudio 1.0.0 to Rhino 8 Package Manager:" -ForegroundColor Green
    Write-Host "     $DeployedRhp" -ForegroundColor Gray
} else {
    Write-Warning "Package installed, but rhp path '$DeployedRhp' not yet detected."
}
