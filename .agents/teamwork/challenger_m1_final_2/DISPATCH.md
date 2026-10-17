# Dispatch Directive: Final Challenger 2 (Curve & Benchmark Verifier)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_final_2`

## Mission
Perform final verification of the curve placement engine, spacing tolerances, and the 500-stone placement benchmark.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\.agents\teamwork\worker_m1_3\handoff.md`

## Verification Tasks
1. Execute full benchmark and placement suites:
   `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~Placement|FullyQualifiedName~Benchmark"`
2. Verify 500-stone placement runs in $< 10$ ms.
3. Verify clean-room scanner:
   `powershell -File tools/check_clean_room.ps1`
4. Provide verdict: `APPROVE` or `REQUEST_CHANGES`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_final_2\handoff.md` and send message with verdict.


## 2026-10-17T00:45:27Z
Read C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_final_2\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\challenger_m1_final_2. Verify 500-stone placement benchmark and curve placement stress tests. Run check_clean_room.ps1. NEVER access _dirty_room/. Write handoff.md and send APPROVE/REQUEST_CHANGES verdict.
