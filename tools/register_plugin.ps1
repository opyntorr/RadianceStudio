# ==============================================================================
# RadianceStudio Plugin Registry & Package Setup for Rhino 8
# ==============================================================================
# Registers RadianceStudio in Rhino 8's package directory and Windows Registry
# under both the default hive and the isolated RadianceStudio scheme hive.
# Populates CommandList so Rhino indexes all 12 jewelry commands immediately.
# ==============================================================================

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$RootDir = Split-Path $ScriptDir -Parent

# 1. Deploy via official Yak Package Manager
$DeployScript = Join-Path $ScriptDir "deploy_to_rhino_packages.ps1"
if (Test-Path $DeployScript) {
    & powershell -File $DeployScript -Configuration $Configuration
}

$DeployedRhp = Join-Path $env:APPDATA "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudio.Rhino.rhp"
if (-not (Test-Path $DeployedRhp)) {
    $DeployedRhp = Join-Path $RootDir "src\RadianceStudio.Rhino\bin\$Configuration\net8.0\RadianceStudio.Rhino.rhp"
}

$PluginGuid = "1d4fb0a4-325a-4b6a-b6b1-d1817eb7ff4b"

$Commands = @(
    "RadStudio",
    "RadInitializeStudio",
    "RadHarnessAudit",
    "RadRingRail",
    "RadEuroRail",
    "RadCushionRail",
    "RadBypassRail",
    "RadShank",
    "RadFlatShank",
    "RadDShapeShank",
    "RadKnifeEdgeShank",
    "RadCathedralShank",
    "RadSplitShank",
    "RadEternityBand",
    "RadProfilePlacer",
    "RadSweep1Rail",
    "RadSweep2Rail",
    "RadGemOnCurve",
    "RadGemBetweenCurves",
    "RadDiamondPlacer",
    "RadBaguettePlacer",
    "RadProngHead",
    "RadBezel",
    "RadHalo",
    "RadChannelSetting",
    "RadClusterSetting",
    "RadSharedProng",
    "RadCutter",
    "RadChannelCutter",
    "RadAzureCutter",
    "RadScoopCutter",
    "RadPave",
    "RadDynamicBend",
    "RadSurfaceMorph",
    "Rad3DRelief",
    "RadTextureOnSurface",
    "RadMillgrain",
    "RadRopeWire",
    "RadCelticKnot",
    "RadQuickRender",
    "RadMetalMaterials",
    "RadGemMaterials",
    "RadStudioEnvironment",
    "RadTurntable",
    "RadSpruePlacer",
    "RadCastingTree",
    "RadMeshDoctor",
    "RadWallThickness",
    "RadAutoDimension",
    "RadTechDrawing",
    "RadGemMapSheet",
    "RadAppraisalSheet",
    "RadBOM",
    "RadExportSTL",
    "RadToggleTreeMode",
    "RadPlaceholder"
)

$Hives = @(
    "HKCU:\Software\McNeel\Rhinoceros\8.0\Plug-Ins\$PluginGuid",
    "HKCU:\Software\McNeel\Rhinoceros\8.0\Scheme: RadianceStudio\Plug-Ins\$PluginGuid"
)

foreach ($hive in $Hives) {
    if (-not (Test-Path $hive)) {
        New-Item -Path $hive -Force | Out-Null
    }

    Set-ItemProperty -Path $hive -Name "Name" -Value "RadianceStudio" -Type String -Force
    Set-ItemProperty -Path $hive -Name "EnglishName" -Value "RadianceStudio" -Type String -Force
    Set-ItemProperty -Path $hive -Name "LoadMode" -Value 1 -Type DWord -Force # 1 = Load at startup
    Set-ItemProperty -Path $hive -Name "Type" -Value 16 -Type DWord -Force     # 16 = Utility Plug-in
    Set-ItemProperty -Path $hive -Name "IsDotNETPlugIn" -Value 1 -Type DWord -Force
    Set-ItemProperty -Path $hive -Name "Organization" -Value "RadianceStudio CAD" -Type String -Force
    Set-ItemProperty -Path $hive -Name "Description" -Value "RadianceStudio Jewelry CAD Plugin" -Type String -Force

    $pluginSubKey = Join-Path $hive "PlugIn"
    if (-not (Test-Path $pluginSubKey)) {
        New-Item -Path $pluginSubKey -Force | Out-Null
    }
    Set-ItemProperty -Path $pluginSubKey -Name "FileName" -Value $DeployedRhp -Type String -Force

    # Populate CommandList so Rhino indexes commands without needing prior manual invocation
    $cmdListKey = Join-Path $hive "CommandList"
    if (-not (Test-Path $cmdListKey)) {
        New-Item -Path $cmdListKey -Force | Out-Null
    }
    foreach ($cmd in $Commands) {
        Set-ItemProperty -Path $cmdListKey -Name $cmd -Value "2;$cmd" -Type String -Force
    }

    Write-Host "[OK] Registered in $hive with $(@($Commands).Count) commands" -ForegroundColor Green
    Write-Host "     FileName -> $DeployedRhp" -ForegroundColor Gray
}

Write-Host "RadianceStudio successfully registered and indexed for Rhino 8!" -ForegroundColor Cyan
