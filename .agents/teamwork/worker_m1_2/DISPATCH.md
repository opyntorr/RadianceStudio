# Dispatch Directive: M1 Fix Implementation Worker

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\worker_m1_2`

## Mission
Apply the verified fixes from the 3 Fix Explorers to resolve all test failures, eliminate double-scaling, fix Emerald pavilion winding, achieve 100% facet planarity, and ensure 100% test pass rate across the whole solution with 0 clean-room violations.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1\handoff.md`
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1\handoff.md`
- `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_quads_1\handoff.md`

## Required Code Edits
1. **`EmeraldMeshGenerator.cs`**:
   - Reverse the vertex winding order in Pavilion Steps 1, 2, and 3:
     - Step 1: `faces.Add(new[] { 24 + j, 32 + j, 32 + next, 24 + next });`
     - Step 2: `faces.Add(new[] { 32 + j, 40 + j, 40 + next, 32 + next });`
     - Step 3: `faces.Add(new[] { 40 + j, 48 + j, 48 + next, 40 + next });`
2. **`GemEngine.cs`**:
   - In `CalculatePlacements`: invoke `Matrix4x4D.CreatePlacement` with uniform scaling (`aspectRatio: 1.0` or default parameter) so canonical meshes (which already have length $L = d \cdot \text{AspectRatio}$) are not double-scaled.
3. **`PrincessMeshGenerator.cs` & `PearMeshGenerator.cs`**:
   - Apply the closed-form analytical formulas from `explorer_m1_fix_quads_1\handoff.md` to guarantee quad facet planarity without altering exact facet counts (Princess: 49, Pear: 58).
4. **Test Adjustments**:
   - In `AdversarialStressTests.cs`: Ensure `EndToEndPipeline` verifies that transformed canonical meshes match their expected canonical aspect ratio without double-scaling; resolve duplicate `[InlineData]` warning.
   - In `GemEnginePlacementTests.cs`: Update determinant divisor in `CalculatePlacements_WithFlipNormal_InvertsNormalAndPreservesDet` to `(2.0 * 2.0 * 2.0)` matching uniform scaling.
5. **Verification**:
   - Run `powershell -File tools/check_clean_room.ps1` (must output 0 violations).
   - Run `powershell -File tools/run_tests.ps1` (all tests must pass, 0 failures, 0 errors).
   - Verify `git status` on `feature/gem-engine`.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your handoff report to `C:\Users\omarp\matrix\.agents\teamwork\worker_m1_2\handoff.md` and send message when complete.


## 2026-10-17T00:07:30Z
Read C:\Users\omarp\matrix\.agents\teamwork\worker_m1_2\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\worker_m1_2. Apply the verified fixes for Emerald pavilion winding, double-scaling on non-square cuts, facet planarity in Princess and Pear, and test assertions. Run check_clean_room.ps1 and run_tests.ps1 with dotnet.exe. DO NOT CHEAT. All implementations must be genuine. NEVER access _dirty_room/. Write handoff.md and send completion message.


## 2026-10-17T00:20:16Z
**Context**: M1 Fix Implementation
**Content**: What is your current status applying the fixes to EmeraldMeshGenerator.cs, GemEngine.cs, PrincessMeshGenerator.cs, PearMeshGenerator.cs, and running the test suite?
**Action**: Please report progress and test run results.
