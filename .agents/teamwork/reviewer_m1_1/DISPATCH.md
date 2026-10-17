# Dispatch Directive: Reviewer 1 (Geometry & Mesh Topology)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_1`

## Mission
Independently review and verify the implementation of canonical gem cut models and procedural mesh generators in `RadianceStudio.Core`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`
- `C:\Users\omarp\matrix\specs\gem_proportions.json`
- `C:\Users\omarp\matrix\.agents\teamwork\worker_m1_1\handoff.md`

## Review Tasks
1. Verify all 6 canonical cuts:
   - Round Brilliant (57 facets)
   - Princess (49 facets)
   - Oval (57 facets, AR 1.45)
   - Pear (58 facets, AR 1.55)
   - Emerald (50 facets, AR 1.40)
   - Marquise (57 facets, AR 1.95)
2. Verify coordinate normalization: origin at girdle center $(0,0,0)$, table at $+Z$, culet at $-Z$, girdle at $Z=0$.
3. Verify linear depth scaling with diameter and non-square aspect ratio scaling.
4. Execute tests and verify clean-room compliance:
   - `powershell -File tools/check_clean_room.ps1`
   - `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`
5. Provide verdict: `APPROVE` or `REQUEST_CHANGES`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your review report to `C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_1\handoff.md` and send message with verdict.

## 2026-10-16T23:47:33Z
[Message] timestamp=2026-10-16T23:47:33Z sender=4d69e4bc-084b-47a0-ad74-5668ab8750e2 priority=MESSAGE_PRIORITY_HIGH content=Read C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\reviewer_m1_1. Review geometry, procedural meshes, facet counts, and bounds. Run tests and clean-room check with dotnet.exe. NEVER access _dirty_room/. Write handoff.md and send your APPROVE/REQUEST_CHANGES verdict.
