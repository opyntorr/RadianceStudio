# ==============================================================================
# Clean-Room Guardrail / Anti-Contamination Scanner for JewelryStudio
# ==============================================================================
# This script scans the clean room source directories (JewelryStudio/) and
# the specification bridge (specs/) to ensure NO proprietary strings, namespaces,
# decompiled code signatures, or licensing terms have leaked across the Chinese Wall.
# ==============================================================================

param(
    [string]$RootPath = "$PSScriptRoot\.."
)

$CleanRoomDir = Join-Path $RootPath "RadianceStudio"
if (-not (Test-Path $CleanRoomDir)) {
    $CleanRoomDir = Join-Path $RootPath "JewelryStudio"
}
$SpecsDir = Join-Path $RootPath "specs"

$ForbiddenPatterns = @(
    "Gemvision",
    "MatrixGold",
    "Stuller",
    "SibInstaller",
    "Sentinel",
    "HASP",
    "SafeNet",
    "DevExpress",
    "InnoSetup"
)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Clean-Room Gatekeeper: Scanning for Contamination" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$Violations = @()

# 1. Scan JewelryStudio source files
if (Test-Path $CleanRoomDir) {
    $SourceFiles = Get-ChildItem -Path $CleanRoomDir -Recurse -Include *.cs, *.xaml, *.csproj, *.json, *.md |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' }

    foreach ($file in $SourceFiles) {
        $lines = Get-Content -Path $file.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            foreach ($pattern in $ForbiddenPatterns) {
                if ($line -match "(?i)\b$pattern\b") {
                    $Violations += [PSCustomObject]@{
                        File = $file.FullName.Replace($RootPath, ".")
                        Line = $i + 1
                        Pattern = $pattern
                        Snippet = $line.Trim()
                    }
                }
            }
        }
    }
}

# 2. Scan specs directory
if (Test-Path $SpecsDir) {
    $SpecFiles = Get-ChildItem -Path $SpecsDir -Recurse -Include *.md, *.json
    foreach ($file in $SpecFiles) {
        $lines = Get-Content -Path $file.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            foreach ($pattern in $ForbiddenPatterns) {
                if ($line -match "(?i)\b$pattern\b") {
                    $Violations += [PSCustomObject]@{
                        File = $file.FullName.Replace($RootPath, ".")
                        Line = $i + 1
                        Pattern = $pattern
                        Snippet = $line.Trim()
                    }
                }
            }
        }
    }
}

# 3. Report Results
if ($Violations.Count -gt 0) {
    Write-Host "`n[ALERT] Clean-Room Contamination Detected: $($Violations.Count) violation(s) found!" -ForegroundColor Red
    $Violations | Format-Table -AutoSize
    exit 1
} else {
    Write-Host "`n[PASS] Clean-Room Integrity Verified. Zero proprietary contaminants found." -ForegroundColor Green
    exit 0
}
