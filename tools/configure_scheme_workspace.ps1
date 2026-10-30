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

$ScriptDir = $PSScriptRoot
$PyScript = Join-Path $ScriptDir "configure_containers.py"

if (Test-Path $PyScript) {
    python $PyScript
} else {
    Write-Warning "configure_containers.py not found at $PyScript"
}
