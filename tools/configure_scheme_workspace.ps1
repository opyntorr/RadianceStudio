# ==============================================================================
# Configure RadianceStudio Scheme Workspace (containers.xml)
# ==============================================================================
# Customizes Rhino 8 Scheme__RadianceStudio:
# 1. Hides standard Rhino toolbars (Standard Toolbars -> visible="False")
# 2. Docks Radiance Ribbon across the top (dock_location="Top", visible="True")
# 3. Docks Jewelry Inspector & Parametric History in the right sidebar
# ==============================================================================

$ErrorActionPreference = "Stop"

$AppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$SettingsDir = Join-Path $AppData "McNeel\Rhinoceros\8.0\settings\Scheme__RadianceStudio"
if (-not (Test-Path $SettingsDir)) {
    New-Item -ItemType Directory -Path $SettingsDir -Force | Out-Null
}

$ContainersXmlPath = Join-Path $SettingsDir "containers.xml"
if (-not (Test-Path $ContainersXmlPath)) {
    # If Scheme__RadianceStudio does not have containers.xml yet, copy from Scheme__Default
    $DefaultContainers = Join-Path $AppData "McNeel\Rhinoceros\8.0\settings\Scheme__Default\containers.xml"
    if (Test-Path $DefaultContainers) {
        Copy-Item -Path $DefaultContainers -Destination $ContainersXmlPath -Force
    }
}

if (-not (Test-Path $ContainersXmlPath)) {
    Write-Warning "containers.xml not found; will be created on first Rhino launch."
    exit 0
}

[xml]$doc = Get-Content -Path $ContainersXmlPath -Raw -Encoding UTF8

# 1. Hide Standard Toolbars (guid: 171011a9-a956-41ee-853e-3ccc0c0db1d8)
$stdDockBar = $doc.SelectSingleNode("//dock_bar[@guid='171011a9-a956-41ee-853e-3ccc0c0db1d8']")
if ($stdDockBar -and $stdDockBar.placement) {
    $stdDockBar.placement.SetAttribute("visible", "False")
    Write-Host "[OK] Hidden standard toolbars dock_bar in Scheme__RadianceStudio." -ForegroundColor Green
}

# 2. Add or update Radiance Ribbon Top DockBar (guid: 7721867f-0ac8-45f4-a0c3-bc5d500ba899)
$ribbonGuid = "7721867f-0ac8-45f4-a0c3-bc5d500ba899"
$pluginGuid = "1d4fb0a4-325a-4b6a-b6b1-d1817eb7ff4b"
$ribbonDockBar = $doc.SelectSingleNode("//dock_bar[@guid='$ribbonGuid']")
if (-not $ribbonDockBar) {
    $dockBarsNode = $doc.SelectSingleNode("//dock_bars")
    if ($dockBarsNode) {
        $ribbonElement = $doc.CreateElement("dock_bar")
        $ribbonElement.SetAttribute("guid", $ribbonGuid)

        $placement = $doc.CreateElement("placement")
        $placement.SetAttribute("dock_location", "Top")
        $placement.SetAttribute("recent_dock_location", "Top")
        $placement.SetAttribute("dock_band_size", "36,105")
        $placement.SetAttribute("dock_band_item_size", "1,-1")
        $placement.SetAttribute("docked_placement", "0,0")
        $placement.SetAttribute("float_point", "0,0")
        $placement.SetAttribute("float_size", "1200,105")
        $placement.SetAttribute("visible", "True")
        $ribbonElement.AppendChild($placement) | Out-Null

        $tabs = $doc.CreateElement("tabs")
        $tabs.SetAttribute("name", "Radiance Ribbon")
        $tabs.SetAttribute("selected_item", $ribbonGuid)
        $tabs.SetAttribute("display_style", "Text")

        $name = $doc.CreateElement("name")
        $locale = $doc.CreateElement("locale_1033")
        $locale.InnerText = "Radiance Ribbon"
        $name.AppendChild($locale) | Out-Null
        $tabs.AppendChild($name) | Out-Null

        $panel = $doc.CreateElement("panel")
        $panel.SetAttribute("guid", $ribbonGuid)
        $panel.SetAttribute("plug_in", $pluginGuid)
        $panel.SetAttribute("name", "Radiance Ribbon")
        $tabs.AppendChild($panel) | Out-Null

        $ribbonElement.AppendChild($tabs) | Out-Null
        $dockBarsNode.PrependChild($ribbonElement) | Out-Null
        Write-Host "[OK] Added Radiance Ribbon dock_bar to containers.xml." -ForegroundColor Green
    }
} else {
    if ($ribbonDockBar.placement) {
        $ribbonDockBar.placement.SetAttribute("visible", "True")
        $ribbonDockBar.placement.SetAttribute("dock_location", "Top")
    }
}

# 3. Add Jewelry Inspector & Parametric History to Right Container if not present
$rightTabs = $doc.SelectSingleNode("//tabs[@name='Right Container']")
if ($rightTabs) {
    $inspectorGuid = "2e27cd9a-3bf2-4bf3-9f9b-b85ff6e9d5aa"
    $historyGuid = "9b82963b-b0b5-4cfa-bc4b-11da04aa3d66"

    $hasInspector = $rightTabs.SelectSingleNode("panel[@guid='$inspectorGuid']")
    if (-not $hasInspector) {
        $inspPanel = $doc.CreateElement("panel")
        $inspPanel.SetAttribute("guid", $inspectorGuid)
        $inspPanel.SetAttribute("plug_in", $pluginGuid)
        $inspPanel.SetAttribute("name", "Jewelry Inspector")
        $rightTabs.AppendChild($inspPanel) | Out-Null
        Write-Host "[OK] Added Jewelry Inspector to Right Container." -ForegroundColor Green
    }

    $hasHistory = $rightTabs.SelectSingleNode("panel[@guid='$historyGuid']")
    if (-not $hasHistory) {
        $histPanel = $doc.CreateElement("panel")
        $histPanel.SetAttribute("guid", $historyGuid)
        $histPanel.SetAttribute("plug_in", $pluginGuid)
        $histPanel.SetAttribute("name", "Parametric History")
        $rightTabs.AppendChild($histPanel) | Out-Null
        Write-Host "[OK] Added Parametric History to Right Container." -ForegroundColor Green
    }
}

$doc.Save($ContainersXmlPath)
Write-Host "[SUCCESS] containers.xml successfully configured for 1:1 MatrixGold experience." -ForegroundColor Cyan
