# Dispatch Directive: Challenger 2 (Adversarial Curve & Placement Tester)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_2`

## Mission
Adversarially challenge and stress-test the curve distribution engine, orthonormal basis evaluation, and transformation matrices in `RadianceStudio.Core`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`

## Stress-Testing Tasks
1. Empirically probe placement edge cases:
   - Zero and negative usable length ($L_{usable} \le 0$).
   - Negative spacing (pavé overlap setting).
   - Collinear tangent and normal vectors (degenerate cross product).
   - High curvature and twisting 3D space curves.
   - Orthonormality verification across thousands of arbitrary points ($\vec{T} \cdot \vec{N} < 10^{-6}$, $\det(R) \approx 1.0$).
   - Performance stress: verify 500-stone placement runs in $< 10$ ms.
2. Run test execution:
   - `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`
   - `powershell -File tools/check_clean_room.ps1`
3. Provide verdict: `APPROVE` or `REQUEST_CHANGES`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your challenge report to `C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_2\handoff.md` and send message with verdict.


## 2026-10-16T23:47:34Z
Read C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_2\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_2. Adversarially challenge curve placement, orthonormal frames, and 500-stone performance. Run tests and clean-room check. NEVER access _dirty_room/. Write handoff.md and send your APPROVE/REQUEST_CHANGES verdict.
