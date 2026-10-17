# Dispatch Directive: M1 Placement & Curve Explorer

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_placement_1`

## Mission
Analyze and specify the curve distribution engine, space curve abstractions, placement evaluation, and performance strategy for `RadianceStudio.Core.Services.GemEngine`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`

## Scope & Analysis Tasks
1. Design `ICurve3D` and `ISurface3D` pure interfaces in `RadianceStudio.Core.Geometry`.
2. Design synthetic test curves (`LineCurve3D`, `CircleCurve3D`, `HelixCurve3D`) for headless unit testing without Rhino runtime dependencies.
3. Detail `GemEngine` service implementation:
   - `int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm)`
   - `IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm)`
   - `IReadOnlyList<GemPlacement> CalculatePlacements(...)`
   - Closed-form 16-element row-major matrix composition matching spec §3.3.
4. Performance analysis: Formulate zero-allocation or pooled data structures to guarantee 500-stone placement calculation executes in $< 10$ ms.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_placement_1\handoff.md` and send message to caller.


## 2026-10-16T23:29:18Z
Read C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_placement_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_placement_1. Design curve distribution, placement engine, and 500-stone <10ms strategy. NEVER access _dirty_room/. Write your findings to handoff.md and send message when done.
