# Dispatch Directive: M1 Implementation Worker

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\worker_m1_1`

## Mission
Implement the complete GemEngine Core for Milestone 1 in `RadianceStudio.Core` and accompanying unit tests in `RadianceStudio.Core.Tests`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_math_1\handoff.md` (Math & Frame Blueprint)
- `C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1\handoff.md` (Mesh Generators Blueprint)
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_placement_1\handoff.md` (Placement & Curve Blueprint)

## Exclusive File Ownership
You have exclusive write access to:
1. `RadianceStudio/src/RadianceStudio.Core/Geometry/*` (`Vector3D.cs`, `Point3D.cs`, `Matrix4x4D.cs`, `OrthonormalFrame.cs`, `ICurve3D.cs`, `ISurface3D.cs`, `LineCurve3D.cs`, `CircleCurve3D.cs`, `HelixCurve3D.cs`, `PlaneSurface3D.cs`)
2. `RadianceStudio/src/RadianceStudio.Core/Models/*` (`Face3D.cs`, `GemMesh.cs`, `GemPlacement.cs`, `GemDimensions.cs`)
3. `RadianceStudio/src/RadianceStudio.Core/Services/*` (`IGemEngine.cs`, `GemEngine.cs`, `IGemMeshGenerator.cs`, `GemMeshFactory.cs`, `MeshGenerators/*`)
4. `RadianceStudio/tests/RadianceStudio.Core.Tests/Geometry/*` (`OrthonormalFrameTests.cs`, `Matrix4x4DTests.cs`)
5. `RadianceStudio/tests/RadianceStudio.Core.Tests/Meshes/*` (`CanonicalGemMeshTests.cs`)
6. `RadianceStudio/tests/RadianceStudio.Core.Tests/Placement/*` (`GemEnginePlacementTests.cs`)

## Implementation Details
1. **Math & Geometry (`RadianceStudio.Core.Geometry`)**:
   - `Vector3D`: Aggressively inlined struct with Dot, Cross, Length, Normalize, operator overloads.
   - `Point3D`: Zero-allocation struct for Euclidean positions.
   - `Matrix4x4D`: 16 double values in row-major layout (`M00` through `M33`), `ToRowMajorArray()`, `Determinant3x3()`, `Transform(Point3D/Vector3D)`, `Multiply(Matrix4x4D)`, `CreatePlacement(...)`.
   - `OrthonormalFrame`: Gram-Schmidt orthogonalization ($\vec{B} = \vec{T} \times \vec{N}$, $\vec{T}^* = \vec{N} \times \vec{B}$), `FlipNormal` support ($\vec{N} \leftarrow -\vec{N}$, preserving right-handed frame $\det = 1.0$), collinear/straight-line fallback.
   - `ICurve3D`, `ISurface3D`, `LineCurve3D`, `CircleCurve3D`, `HelixCurve3D`, `PlaneSurface3D`.
2. **Models & Procedural Meshes (`RadianceStudio.Core.Models` & `Services.MeshGenerators`)**:
   - `Face3D`, `GemMesh`.
   - `IGemMeshGenerator`, `BaseGemMeshGenerator`, `GemMeshFactory`.
   - 6 Canonical Cut Mesh Generators per specifications in `specs/gem_proportions.json`:
     - `RoundBrilliantMeshGenerator` (57 facets)
     - `PrincessMeshGenerator` (49 facets)
     - `OvalMeshGenerator` (57 facets, aspect ratio 1.45)
     - `PearMeshGenerator` (58 facets, aspect ratio 1.55)
     - `EmeraldMeshGenerator` (50 facets, aspect ratio 1.40)
     - `MarquiseMeshGenerator` (57 facets, aspect ratio 1.95)
   - Canonical 1.0mm normalized scale centered at origin $(0,0,0)$, table at $+Z$, culet at $-Z$.
   - Linear depth scaling and aspect ratio scaling on length axis $Y$.
3. **Curve Placement Engine (`RadianceStudio.Core.Services`)**:
   - `IGemEngine` and `GemEngine`.
   - `CalculateStoneCount(usableLength, gemSize, spacing)` with $10^{-9}$ epsilon guard against floating-point floor truncation.
   - `CalculateArcDistances(usableLength, gemSize, spacing, startOffset)`.
   - `CalculatePlacements(...)` with closed-form 16-element row-major matrix composition matching spec §3.3.
4. **Verification & Testing**:
   - Implement unit tests in `RadianceStudio.Core.Tests`:
     - `OrthonormalFrameTests.cs`: Gram-Schmidt dot products $< 10^{-6}$, $\det(R) \approx 1.0$, `FlipNormal`.
     - `Matrix4x4DTests.cs`: Row-major layout, determinant, transform, composition.
     - `CanonicalGemMeshTests.cs`: Exact facet counts (57, 49, 57, 58, 50, 57), dimensions, table $+Z$, culet $-Z$, linear scaling.
     - `GemEnginePlacementTests.cs`: Stone count, arc distances, spacing tolerance ($< 0.001$ mm).
   - Execute tests using `C:\Users\omarp\.dotnet\dotnet.exe test RadianceStudio/RadianceStudio.sln --configuration Release` or `powershell -File tools/run_tests.ps1`.
   - Execute clean-room check `powershell -File tools/check_clean_room.ps1`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your handoff report to:
`C:\Users\omarp\matrix\.agents\teamwork\worker_m1_1\handoff.md`
Report back via `send_message` when complete with test results and build status.


## 2026-10-16T23:36:23Z
Received invocation:
Implement the complete GemEngine Core for Milestone 1 in RadianceStudio.Core (Geometry, Models, Services, MeshGenerators) and accompanying unit tests in RadianceStudio.Core.Tests. Run tests and clean-room check with dotnet.exe. DO NOT CHEAT. All implementations must be genuine. NEVER access _dirty_room/. Document all changes and test results in handoff.md and send a completion message.
