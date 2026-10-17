# Dispatch Directive: M1 Fix Implementation Worker 3

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3`

## Mission
Apply the exact, verified code edits from the 3 Fix Explorers, run full test suite and clean-room linter, and report completion.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1\handoff.md` (Emerald pavilion winding)
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1\handoff.md` (Double-scaling fix)
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_quads_1\handoff.md` (Princess/Pear planarity & warning fix)

## Exact Code Modifications Required
1. **`RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs`**:
   Replace lines 122, 129, 136 in the three pavilion step loops:
   - Step 1 (line 125): `faces.Add(new[] { 24 + j, 32 + j, 32 + next, 24 + next });`
   - Step 2 (line 132): `faces.Add(new[] { 32 + j, 40 + j, 40 + next, 32 + next });`
   - Step 3 (line 139): `faces.Add(new[] { 40 + j, 48 + j, 48 + next, 40 + next });`

2. **`RadianceStudio/src/RadianceStudio.Core/Services/GemEngine.cs`**:
   - In `CalculatePlacements` (around line 100):
     Change `aspectRatio` to `1.0` when calling `CreatePlacement`:
     `var matrix = Matrix4x4D.CreatePlacement(frame, pt, gemSizeMm, aspectRatio: 1.0, rotationAngleDeg);`

3. **`RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/PrincessMeshGenerator.cs`**:
   - Apply the closed-form corner bezel formula from Section 4.2 of `explorer_m1_fix_quads_1\handoff.md` to ensure upper girdle quad planarity (max deviation $\le 0.003$ mm) while strictly preserving 49 facets.

4. **`RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/PearMeshGenerator.cs`**:
   - Apply the equator shift from Section 4.2 of `explorer_m1_fix_quads_1\handoff.md` to ensure bezel quad planarity (max deviation $\le 0.021$ mm) while strictly preserving 58 facets.

5. **`RadianceStudio/tests/RadianceStudio.Core.Tests/Meshes/AdversarialStressTests.cs`**:
   - Change `[InlineData(-0.0)]` at line 77 to `[InlineData(-0.0001)]` to eliminate the xUnit1025 duplicate warning.
   - In `EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio` (lines 280-288):
     Obtain placement matrix via `GemEngine.CalculatePlacements` or use `aspectRatio: 1.0` in `CreatePlacement`, verifying that transformed canonical meshes match their expected canonical aspect ratio without double-scaling.

6. **`RadianceStudio/tests/RadianceStudio.Core.Tests/Placement/GemEnginePlacementTests.cs`**:
   - In `CalculatePlacements_WithFlipNormal_InvertsNormalAndPreservesDet`:
     Update the 3x3 determinant divisor to `(2.0 * 2.0 * 2.0)` matching uniform scaling.

## Verification Commands
1. Run Clean-Room Linter:
   `powershell -File tools/check_clean_room.ps1`
   (Must output 0 violations).
2. Run Full Test Suite:
   `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`
   (Must pass 100% of all tests with 0 failures and 0 warnings).

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3\handoff.md` and send completion message.

## 2026-10-17T00:31:40Z
Read C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3. Apply the 5 exact code edits to EmeraldMeshGenerator.cs, GemEngine.cs, PrincessMeshGenerator.cs, PearMeshGenerator.cs, and the tests. Run check_clean_room.ps1 and dotnet test in Release mode. DO NOT CHEAT. All implementations must be genuine. NEVER access _dirty_room/. Write handoff.md and send completion message.

