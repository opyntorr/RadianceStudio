# RadianceStudio Dual-Track E2E Test Infrastructure Specification (`TEST_INFRA.md`)

## 1. Overview & Architecture
RadianceStudio employs an automated, headless, requirement-driven opaque-box testing infrastructure validating all 21 architectural features defined in `PROJECT.md § Feature Inventory`.

The test suite runs under .NET 8 using xUnit 2.5.3 and strictly satisfies:
- **Zero compiler warnings** under `/warnaserror` across Debug and Release configurations.
- **Clean-Room Chinese Wall Compliance**: Zero proprietary vendor strings and zero prohibited legacy UI frameworks (`System.Windows.Forms`, `WPF`, `Microsoft.Win32`, `DllImport`).
- **Headless CI Execution Architecture**: All tests execute cleanly in continuous integration without requiring physical display hardware, GPU monitors, or unmanaged native Rhino runtime libraries (`rhcommon_c.dll`), safely guarded via `RailEngine.IsRhinoNativeAvailable`.

---

## 2. Four-Tier Testing Methodology

```
┌────────────────────────────────────────────────────────────────────────┐
│                   Tier 4: Real-World Jewelry Workflows                 │
│   (Engagement Ring Workflow, Halo Cluster, Pavé Eternity Band)         │
├────────────────────────────────────────────────────────────────────────┤
│                Tier 3: Cross-Feature Combinations & State              │
│   (Rail + Shank, Shank + Gem Row, Gem Row + Cutters, Rollback, BOM)    │
├────────────────────────────────────────────────────────────────────────┤
│                Tier 2: Boundary & Corner Case Stress Tests             │
│   (Extreme Ring Sizes, Micro-gems, Degenerate Curves, Malformed JSON)   │
├────────────────────────────────────────────────────────────────────────┤
│                    Tier 1: Feature Coverage (Happy Path)               │
│   (Launcher CLI, Dark Theme, Ribbon, Inspector, MCP 16 Tools, Lifecycle│
└────────────────────────────────────────────────────────────────────────┘
```

### Tier 1: Feature Coverage (Happy-Path Unit Tests)
Validates individual components, specifications, data bindings, and schemas:
- **Launcher**: CLI switch formulation (`/scheme="RadianceStudio"`, `/nosplash`, `/runscript="_-RadInitializeStudio"`), cross-platform discovery (`RHINO8_PATH`, standard Windows `ProgramFiles`, macOS app bundles), borderless splash dimensions (540x320 px) and palette, desktop shortcut generator script.
- **Theme**: Charcoal (`#1E1E1E`), Gold (`#D4AF37`, `#FFD700`), Cyan (`#00E5FF`), Text (`#F1F1F1`), Action (`#2EA44F`, `#E53935`) color palette definitions and WCAG AAA/AA contrast compliance.
- **Ribbon**: 6 workflow tabs (Rails & Shanks, Gems, Settings, Cutters, Transform, Manufacturing), fixed height (105 px), all 13 canonical gem cuts, alloy definitions, procedural vector icon registry.
- **Inspector**: `IInspectorViewModel` contract, millimetric coupled slider/stepper synchronization (0.01mm resolution), Action Bar (Apply baking with `ParametricMetadata` vs Cancel discarding preview), 1-click dynamic edit selection parameter reloading.
- **Parametric History**: Dual-mode switching (`StudioClassic` vs `FeatureTree`), chronological `OrderIndex` sorting, draggable rollback bar (`RollbackIndex`), right-click suppress/resume (`IsSuppressed`) with recursive child cascading.
- **Rhino Plugin**: `/scheme="RadianceStudio"` startup, window title `"RadianceStudio - Jewelry CAD Suite"`, millimetric template (0.001mm tolerance, 0.1mm grid/snap, studio ground plane), 12 interactive `Rad*` commands.
- **MCP Bridge**: Thread-safe `RhinoUiDispatcher` with headless CI fallback, all 16 MCP tool schemas (type `object`, required parameters, descriptions), structured JSON response envelopes (`success`/`error`).

### Tier 2: Boundary & Corner Cases
Stresses components against domain extremes and invalid inputs:
- Non-existent/invalid `RHINO8_PATH` environments, malformed readiness tokens, extreme process IDs.
- Extreme ring sizes (US 0, -5, 50), zero-length or sub-millimeter curves, kissing stones (0.0mm spacing), micro-gems (0.2mm) vs oversized center stones (35mm).
- Extreme shank dimensions (0.2mm ultra-thin vs 25mm chunky signet), out-of-range rollback indices, corrupt metadata dictionaries.
- Malformed MCP JSON payloads, missing required parameters, unknown tool names, out-of-bounds parameters.

### Tier 3: Cross-Feature Combinations
Tests multi-component interoperation and state persistence:
- **Rail + Shank Integration**: Sweeping a comfort-fit shank profile onto a ring rail, verifying inner diameter clearance and outer profile dimensions.
- **Swept Shank + Gem Row Placement**: Distributing accent gems along a shank shoulder curve with orthonormal frames ($|\vec{T}|=1, |\vec{N}|=1, |\vec{B}|=1, \vec{T}\cdot\vec{N} < 10^{-6}$).
- **Gem Row + Cutters Subtraction**: Generating matching 70° bearing seat cutters with 10% girdle clearance and light holes.
- **Parametric History + Timeline Rollback**: Registering 5 sequential dependent features (Rail -> Shank -> Center Gem -> Prong Head -> Cutters), freezing downstream nodes at rollback index 2, dynamically modifying shank width, and unrolling timeline.
- **Feature Suppression + Live BOM Recalculation**: Suppressing halo melee gems from a ring assembly and verifying the live BOM immediately omits halo stones while preserving the center diamond and shank metal.

### Tier 4: Real-World Jewelry Workflows
End-to-end simulation of production jewelry manufacturing:
- **Workflow 1: Solitaire Cathedral Engagement Ring**:
  US Size 6.5 rail -> Swept comfort-fit cathedral shank (with under-gallery azurador) -> 1.0ct Round Brilliant center stone -> 4-claw prong head with gallery and base wires -> 12 shoulder accent gems -> 70° seat cutters -> 14K White Gold BOM weight & cost calculation -> 0.80mm wall thickness casting inspection.
- **Workflow 2: Luxury Halo Cluster Cocktail Ring**:
  US Size 7.0 cushion squircle rail -> Euro-shank -> 8.0x6.0mm Oval Brilliant center stone -> Offset halo curve (+1.4mm guard) -> 24 melee stones (1.2mm, 0.12mm spacing) -> Recessed platform tray with micro-prongs -> Platinum 950 casting weight evaluation.
- **Workflow 3: Multi-Row Pavé Eternity Band**:
  US Size 8.0 eternity rail -> 5.5mm wide comfort-fit band -> 3-row hexagonal close-packed pavé (135 x 1.4mm stones) -> Interstitial micro-bead retention grains -> Metal under-bead thickness inspection -> Binary STL byte stream validation for casting.

---

## 3. Feature Inventory Coverage Matrix (All 21 Features)

| Feature # | Feature Description | Milestone | Primary Test Suite | Coverage Tier |
|---|---|---|---|---|
| **1** | Standalone Launcher Executable | M1 | `Launcher/LauncherArchitectureTests.cs` | Tier 1, Tier 2 |
| **2** | Cross-Platform Rhino Discovery | M1 | `Launcher/LauncherArchitectureTests.cs`, `LauncherBoundaryAndStressTests.cs` | Tier 1, Tier 2 |
| **3** | Borderless Eto Splash Screen | M1 | `Launcher/LauncherArchitectureTests.cs` | Tier 1 |
| **4** | Desktop Shortcut Generator | M1 | `Launcher/LauncherArchitectureTests.cs` | Tier 1 |
| **5** | Jewelry Dark Theme | M2 | `UI/JewelryDarkThemeTests.cs` | Tier 1, Tier 2 |
| **6** | Top Horizontal Ribbon Bar | M2 | `UI/JewelryRibbonWorkflowTests.cs` | Tier 1 |
| **7** | Dynamic Inspector Panel | M2 | `UI/DynamicInspectorPanelTests.cs` | Tier 1, Tier 2 |
| **8** | 60 FPS Viewport Preview Conduit | M2 | `UI/DynamicInspectorPanelTests.cs` | Tier 1 |
| **9** | Action Bar (Apply / Cancel) | M2 | `UI/DynamicInspectorPanelTests.cs` | Tier 1, Tier 2 |
| **10** | 1-Click Dynamic Edit Selection | M2 | `UI/DynamicInspectorPanelTests.cs` | Tier 1, Tier 3 |
| **11** | Dual-Mode Parametric History | M2 | `UI/ParametricHistoryAndRollbackTests.cs` | Tier 1, Tier 3 |
| **12** | Draggable Rollback Bar | M2 | `UI/ParametricHistoryAndRollbackTests.cs` | Tier 1, Tier 2, Tier 3 |
| **13** | Right-Click Suppress / Resume | M2 | `UI/ParametricHistoryAndRollbackTests.cs` | Tier 1, Tier 3 |
| **14** | Plugin Scheme Lifecycle & Title | M3 | `UI/RhinoPluginLifecycleAndCommandsTests.cs` | Tier 1 |
| **15** | Millimetric Jewelry Template | M3 | `UI/RhinoPluginLifecycleAndCommandsTests.cs` | Tier 1 |
| **16** | Interactive Commands (12 Rad*) | M3 | `UI/RhinoPluginLifecycleAndCommandsTests.cs` | Tier 1, Tier 2 |
| **17** | Thread-Safe Rhino UI Dispatcher | M4 | `MCP/RhinoUiDispatcherTests.cs` | Tier 1, Tier 2 |
| **18** | Expanded 16 MCP Tools | M4 | `MCP/Mcp16ToolsSchemaTests.cs`, `MCP/McpInvocationAndBoundaryTests.cs` | Tier 1, Tier 2 |
| **19** | MCP JSON Schema & Registry | M4 | `MCP/Mcp16ToolsSchemaTests.cs`, `MCP/McpInvocationAndBoundaryTests.cs` | Tier 1, Tier 2 |
| **20** | Clean-Room Chinese Wall Compliance | M5 | `UI/CleanRoomComplianceTests.cs` | Tier 1 |
| **21** | Automated Test Suite Verification | M5 | `UI/CleanRoomComplianceTests.cs`, `run_tests.ps1` | Tier 1, Tier 2, Tier 3, Tier 4 |

---

## 4. Test Suite Execution & Verification

### Running Complete Local Test Suite
```powershell
powershell -File tools/run_tests.ps1 -Configuration Release
```
This executes:
1. `tools/check_clean_room.ps1` anti-contamination scanner.
2. `dotnet test RadianceStudio.sln --configuration Release /warnaserror`.

### Running Clean-Room Scanner Independently
```powershell
powershell -File tools/check_clean_room.ps1
```

### Running xUnit Tests via .NET SDK Directly
```powershell
C:\Users\omarp\.dotnet\dotnet.exe test tests/RadianceStudio.Core.Tests/RadianceStudio.Core.Tests.csproj --configuration Release /warnaserror
```
