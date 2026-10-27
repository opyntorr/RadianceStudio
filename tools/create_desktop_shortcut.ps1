# ==============================================================================
# RadianceStudio Desktop Shortcut Creator
# ==============================================================================
# Creates a Windows Desktop shortcut pointing to the standalone RadianceStudio
# launcher executable.
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent
$TargetExe = Join-Path $RootDir "src\RadianceStudio.Launcher\bin\$Configuration\net8.0\RadianceStudio.exe"

if (-not (Test-Path $TargetExe)) {
    # If Release build does not exist, check Debug build as fallback
    $FallbackExe = Join-Path $RootDir "src\RadianceStudio.Launcher\bin\Debug\net8.0\RadianceStudio.exe"
    if (Test-Path $FallbackExe) {
        $TargetExe = $FallbackExe
    } else {
        Write-Warning "RadianceStudio.exe not found at: $TargetExe"
        Write-Host "Building RadianceStudio.Launcher in $Configuration configuration..." -ForegroundColor Cyan
        $DotnetCmd = if (Test-Path "C:\Users\omarp\.dotnet\dotnet.exe") { "C:\Users\omarp\.dotnet\dotnet.exe" } else { "dotnet" }
        & $DotnetCmd build (Join-Path $RootDir "src\RadianceStudio.Launcher\RadianceStudio.Launcher.csproj") --configuration $Configuration
    }
}

$ResolvedTarget = (Resolve-Path $TargetExe).Path
$DesktopPath = [Environment]::GetFolderPath([Environment+SpecialFolder]::Desktop)
if (-not (Test-Path $DesktopPath)) {
    $DesktopPath = Join-Path $env:USERPROFILE "Desktop"
}

$ShortcutPath = Join-Path $DesktopPath "RadianceStudio.lnk"

# Register RadianceStudio.Rhino.rhp into Rhino 8 registry
$RegisterScript = Join-Path $ScriptDir "register_plugin.ps1"
if (Test-Path $RegisterScript) {
    & powershell -File $RegisterScript -Configuration $Configuration
}

$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $ResolvedTarget
$Shortcut.WorkingDirectory = Split-Path $ResolvedTarget -Parent
$Shortcut.Description = "RadianceStudio - Professional Standalone Jewelry CAD Suite"
$Shortcut.IconLocation = "$ResolvedTarget,0"
$Shortcut.Save()

Write-Host "Created desktop shortcut: $ShortcutPath" -ForegroundColor Green
Write-Host "  Target: $ResolvedTarget" -ForegroundColor Gray
