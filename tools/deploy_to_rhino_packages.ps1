# ==============================================================================
# Deploy RadianceStudio to Rhino 8 Packages Directory
# ==============================================================================
# Copies RadianceStudio plugin and dependencies into Rhino 8's package manager hive
# (%APPDATA%\McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0).
# This ensures automatic discovery and command indexing by Rhino 8 across all schemes.
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

$PackageBase = Join-Path $env:APPDATA "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0"
$PackageNet8 = Join-Path $PackageBase "net8.0"

if (-not (Test-Path $PackageNet8)) {
    New-Item -ItemType Directory -Path $PackageNet8 -Force | Out-Null
}

# 1. Write manifest.yml
$ManifestContent = @"
---
name: RadianceStudio
version: 1.0.0
authors:
- RadianceStudio Team
description: Commercial-Grade Parametric Jewelry CAD Suite for Rhino 8
url: https://github.com/opyntorr/RadianceStudio
keywords:
- jewelry
- cad
- parametric
- gems
- pave
- cutters
- matrix
"@
Set-Content -Path (Join-Path $PackageBase "manifest.yml") -Value $ManifestContent -Encoding UTF8

# 2. Copy compiled assemblies and rhp
$FilesToCopy = Get-ChildItem -Path $SourceBin -Include *.rhp, *.dll, *.pdb -Recurse
foreach ($file in $FilesToCopy) {
    Copy-Item -Path $file.FullName -Destination $PackageNet8 -Force
}

Write-Host "[OK] Deployed RadianceStudio 1.0.0 to Rhino packages directory:" -ForegroundColor Green
Write-Host "     $PackageNet8" -ForegroundColor Gray
