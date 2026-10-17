# Dispatch Directive: Testing & Verification Explorer

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_tests_1`

## Mission
Survey the testing architecture, existing tests, xUnit conventions, test runner scripts, and acceptance criteria verification.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Test project(s) under `C:\Users\omarp\matrix\RadianceStudio\` (e.g. `RadianceStudio.Tests` or similar)
- `C:\Users\omarp\matrix\tools\run_tests.ps1`
- `C:\Users\omarp\matrix\tools\check_clean_room.ps1`

## Scope & Investigation Tasks
1. Identify test project location, project file, referenced test frameworks (xUnit, FluentAssertions, etc.).
2. Examine existing tests (if any) and test execution command (`dotnet test`, `tools/run_tests.ps1`).
3. Analyze required test coverage per R4 and Acceptance Criteria:
   - Every gem cut normalized 1.0mm dimensions and proportions.
   - Linear scaling of depth, crown, pavilion by diameter.
   - Non-square aspect ratio scaling.
   - Orthonormal basis dot products (<10^-6) and det(R) ≈ 1.0.
   - Curve usable length, stone count, spacing tolerance (<0.001mm).
   - Placement calculation benchmark: 500 stones in < 10 ms.
4. Report on how tests should be organized and verified.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your findings and handoff report to:
`C:\Users\omarp\matrix\.agents\teamwork\explorer_tests_1\handoff.md`
Report back via `send_message` when complete.


## 2026-10-16T23:22:23Z
Read C:\Users\omarp\matrix\.agents\teamwork\explorer_tests_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_tests_1. Survey test project structure, test scripts, and acceptance criteria. NEVER touch _dirty_room/. Write findings to handoff.md in your working directory and send a completion message to your caller.
