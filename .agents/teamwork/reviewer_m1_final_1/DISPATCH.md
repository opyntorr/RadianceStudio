# Dispatch Directive: Final Reviewer 1 (Geometry & Facets)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_final_1`

## Mission
Perform final independent verification of the geometry and mesh generators after the defect fixes.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3\handoff.md`

## Verification Tasks
1. Verify Emerald cut 2-manifold orientation and outward normal vectors.
2. Verify Princess (49 facets) and Pear (58 facets) quad facet planarity.
3. Run:
   - `powershell -File tools/check_clean_room.ps1`
   - `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`
4. Provide verdict: `APPROVE` or `REQUEST_CHANGES`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_final_1\handoff.md` and send message with verdict.

## 2026-10-17T00:45:27Z
Read C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_final_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_final_1. Verify geometry fixes, facet planarity, 2-manifold orientation. Run check_clean_room.ps1 and dotnet test. NEVER access _dirty_room/. Write handoff.md and send APPROVE/REQUEST_CHANGES verdict.
