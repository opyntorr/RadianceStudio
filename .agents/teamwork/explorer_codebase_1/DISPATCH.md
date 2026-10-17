# Dispatch Directive: Codebase & Architecture Explorer

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_codebase_1`

## Mission
Survey the existing `RadianceStudio` repository structure, project configurations, dependencies, git branch status, and tool scripts.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Solution and project files in `C:\Users\omarp\matrix\RadianceStudio\`
- `C:\Users\omarp\matrix\tools\` (especially `check_clean_room.ps1` and `run_tests.ps1`)

## Scope & Investigation Tasks
1. Check git branch and status (confirm branch is `feature/gem-engine` or what git state is).
2. Examine `RadianceStudio/RadianceStudio.sln` and all `.csproj` files:
   - Target framework (.NET 8.0, C# version)
   - Packages/references (e.g. RhinoCommon, System.Numerics, etc.)
   - Existing project structure (namespaces, folders, build output)
3. Check where GemEngine classes/interfaces should live within `RadianceStudio`.
4. Inspect `tools/check_clean_room.ps1` to see exactly what checks and rules it runs.
5. Inspect build and clean-room requirements.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your findings and handoff report to:
`C:\Users\omarp\matrix\.agents\teamwork\explorer_codebase_1\handoff.md`
Report back via `send_message` when complete.


## 2026-10-16T23:22:23Z
Read C:\Users\omarp\matrix\.agents\teamwork\explorer_codebase_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_codebase_1. Survey repository structure, solution, git branch, and clean-room script. NEVER touch _dirty_room/. Write findings to handoff.md in your working directory and send a completion message to your caller.
