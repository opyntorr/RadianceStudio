# Dispatch Directive: Challenger 1 (Adversarial Geometry & Stress Tester)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_1`

## Mission
Adversarially challenge and stress-test the geometry, procedural mesh generators, and matrix mathematics in `RadianceStudio.Core`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`
- `C:\Users\omarp\matrix\specs\gem_proportions.json`

## Stress-Testing Tasks
1. Empirically probe edge cases:
   - Extreme diameters ($0.001$ mm, $1000$ mm, negative, 0, NaN, infinity).
   - Mesh watertightness: verify 2-manifold Euler characteristic $\chi = V - E + F = 2$.
   - Normal consistency: verify all normals point strictly outwards.
   - Matrix invertibility and condition numbers for extreme aspect ratios.
2. Run test execution:
   - `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`
   - `powershell -File tools/check_clean_room.ps1`
3. Provide verdict: `APPROVE` (correct and robust) or `REQUEST_CHANGES` (demonstrated defect).

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your challenge report to `C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_1\handoff.md` and send message with verdict.


## 2026-10-16T23:47:33Z
Read C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_1. Adversarially challenge geometry and mesh topology across extreme inputs. Run tests and clean-room check. NEVER access _dirty_room/. Write handoff.md and send your APPROVE/REQUEST_CHANGES verdict.
