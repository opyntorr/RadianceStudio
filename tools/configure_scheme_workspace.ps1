# ==============================================================================
# Configure RadianceStudio Scheme Workspace (containers.xml)
# ==============================================================================
# Customizes Rhino 8 Scheme__RadianceStudio:
# 1. Hides/replaces standard Rhino toolbars with native Radiance Studio tabs:
#    (Projects, Rails, Shanks, Gems, Settings, Cutters, Transform, Art & Textures, Render, Manufacturing, Drafting)
# 2. Hides all left sidebar dockbars (Rhinoceros Sidebar, OSnap, Main)
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

$ruiFileGuid = "c4a754b2-2987-43a1-9b90-1e5f8a09b431"
$pluginGuid = "1d4fb0a4-325a-4b6a-b6b1-d1817eb7ff4b"
$ruiFilePath = Join-Path $AppData "McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0\RadianceStudioToolbar.rui"

# 1. Register RadianceStudioToolbar.rui in <files>
$filesNode = $doc.SelectSingleNode("//files")
if ($filesNode) {
    $existingFile = $filesNode.SelectSingleNode("file_name[@guid='$ruiFileGuid']")
    if (-not $existingFile) {
        $fileElem = $doc.CreateElement("file_name")
        $fileElem.SetAttribute("guid", $ruiFileGuid)
        $fileElem.SetAttribute("plug_in_guid", $pluginGuid)
        $fileElem.SetAttribute("source", "PlugInFolder")
        $fileElem.InnerText = $ruiFilePath
        $filesNode.AppendChild($fileElem) | Out-Null
        Write-Host "[OK] Registered RadianceStudioToolbar.rui in containers.xml." -ForegroundColor Green
    } else {
        $existingFile.InnerText = $ruiFilePath
    }
}

# 2. Configure Top Dock Bar (guid: 171011a9-a956-41ee-853e-3ccc0c0db1d8) to display Radiance Studio tabs
$topDockBar = $doc.SelectSingleNode("//dock_bar[@guid='171011a9-a956-41ee-853e-3ccc0c0db1d8']")
if ($topDockBar) {
    $topDockBar.SetAttribute("source_group_file", $ruiFileGuid)
    $topDockBar.SetAttribute("source_group", "e8b52194-4360-4963-8a02-984cd4d47150")

    if ($topDockBar.placement) {
        $topDockBar.placement.SetAttribute("visible", "True")
        $topDockBar.placement.SetAttribute("dock_location", "Top")
    }

    $tabsNode = $topDockBar.SelectSingleNode("tabs")
    if ($tabsNode) {
        $tabsNode.SetAttribute("name", "Radiance Studio")
        $tabsNode.SetAttribute("selected_item", "f1011111-1111-1111-1111-111111111102") # Default to Rails
        $tabsNode.SetAttribute("display_style", "Text")

        # Replace all standard tool_bar nodes with 11 Radiance Studio tabs
        $tabsNode.RemoveAll()

        $nameElem = $doc.CreateElement("name")
        $locElem = $doc.CreateElement("locale_1033")
        $locElem.InnerText = "Radiance Studio"
        $nameElem.AppendChild($locElem) | Out-Null
        $tabsNode.AppendChild($nameElem) | Out-Null

        $toolbars = @(
            "f1011111-1111-1111-1111-111111111101", # Projects
            "f1011111-1111-1111-1111-111111111102", # Rails
            "f1011111-1111-1111-1111-111111111103", # Shanks
            "f1011111-1111-1111-1111-111111111104", # Gems
            "f1011111-1111-1111-1111-111111111105", # Settings
            "f1011111-1111-1111-1111-111111111106", # Cutters
            "f1011111-1111-1111-1111-111111111107", # Transform
            "f1011111-1111-1111-1111-111111111108", # Art & Textures
            "f1011111-1111-1111-1111-111111111109", # Render
            "f1011111-1111-1111-1111-111111111110", # Manufacturing
            "f1011111-1111-1111-1111-111111111111"  # Drafting
        )

        foreach ($tbGuid in $toolbars) {
            $tbElem = $doc.CreateElement("tool_bar")
            $tbElem.SetAttribute("guid", $tbGuid)
            $tbElem.SetAttribute("file", $ruiFileGuid)
            $tabsNode.AppendChild($tbElem) | Out-Null
        }

        Write-Host "[OK] Top Dock Bar configured with 11 Radiance Studio tabs." -ForegroundColor Green
    }
}

# 3. Hide Left Sidebar Dock Bars (Rhinoceros Sidebar, OSnap, Main)
$leftSidebars = @(
    "7491ffac-ebf2-4214-bf42-d3d1e4e0f0e1", # Rhinoceros Sidebar
    "b1c225e5-68ba-44a9-8402-f70a6eb49937", # OSnap
    "d1225ee0-cf16-4f05-b053-60aa46183ac0"  # Main
)

foreach ($lsGuid in $leftSidebars) {
    $lsBar = $doc.SelectSingleNode("//dock_bar[@guid='$lsGuid']")
    if ($lsBar) {
        if ($lsBar.placement) {
            $lsBar.placement.SetAttribute("visible", "False")
        }
    }
    # Also remove from visible dock site bands
    $siteItems = $doc.SelectNodes("//dock_site//dock_bar[@guid='$lsGuid']")
    foreach ($si in $siteItems) {
        $si.ParentNode.RemoveChild($si) | Out-Null
    }
}
Write-Host "[OK] Left sidebar dockbars hidden and detached." -ForegroundColor Green

# 4. Remove any floating or leftover ribbon dock_bars
$floatingRibbons = $doc.SelectNodes("//dock_bar[.//panel[@guid='7721867f-0ac8-45f4-a0c3-bc5d500ba899']]")
foreach ($fr in $floatingRibbons) {
    $fr.ParentNode.RemoveChild($fr) | Out-Null
    Write-Host "[OK] Removed floating ribbon dock_bar." -ForegroundColor Green
}

$floatingOldRibbons = $doc.SelectNodes("//dock_bar[@guid='7721867f-0ac8-45f4-a0c3-bc5d500ba899']")
foreach ($fr in $floatingOldRibbons) {
    $fr.ParentNode.RemoveChild($fr) | Out-Null
}

# 5. Dock Jewelry Inspector and Parametric History in Right Container
$rightTabs = $doc.SelectSingleNode("//tabs[@name='Right Container']")
if ($rightTabs) {
    $inspectorGuid = "552c1656-787d-4fce-a263-ac0174092e47"
    $historyGuid = "8965008e-eea4-4a86-bfa3-a2d696417531"

    # Remove old incorrect guids if present
    $oldGuids = @("2e27cd9a-3bf2-4bf3-9f9b-b85ff6e9d5aa", "9b82963b-b0b5-4cfa-bc4b-11da04aa3d66")
    foreach ($og in $oldGuids) {
        $oldPanel = $rightTabs.SelectSingleNode("panel[@guid='$og']")
        if ($oldPanel) {
            $rightTabs.RemoveChild($oldPanel) | Out-Null
        }
    }

    $hasInspector = $rightTabs.SelectSingleNode("panel[@guid='$inspectorGuid']")
    if (-not $hasInspector) {
        $inspPanel = $doc.CreateElement("panel")
        $inspPanel.SetAttribute("guid", $inspectorGuid)
        $inspPanel.SetAttribute("plug_in", $pluginGuid)
        $inspPanel.SetAttribute("name", "Jewelry Inspector")
        $rightTabs.AppendChild($inspPanel) | Out-Null
    }

    $hasHistory = $rightTabs.SelectSingleNode("panel[@guid='$historyGuid']")
    if (-not $hasHistory) {
        $histPanel = $doc.CreateElement("panel")
        $histPanel.SetAttribute("guid", $historyGuid)
        $histPanel.SetAttribute("plug_in", $pluginGuid)
        $histPanel.SetAttribute("name", "Parametric History")
        $rightTabs.AppendChild($histPanel) | Out-Null
    }

    # Set default tab focus to Jewelry Inspector
    $rightTabs.SetAttribute("selected_item", $inspectorGuid)
    Write-Host "[OK] Right Container configured with Jewelry Inspector and Parametric History." -ForegroundColor Green
}

$doc.Save($ContainersXmlPath)
Write-Host "[SUCCESS] containers.xml configured with 11 Radiance Studio tabs and clean workspace." -ForegroundColor Cyan
