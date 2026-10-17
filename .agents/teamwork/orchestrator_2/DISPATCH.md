# Succession Dispatch Directive: Project Orchestrator (Generation 2)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\orchestrator_2`

## Predecessor Handoff
Read `C:\Users\omarp\matrix\.agents\teamwork\orchestrator_1\handoff.md` immediately.

## Parent Information
Your parent is `423ccf4c-3392-4eec-abad-96640a0c491d` (Sentinel). Use this ID for all status reporting and victory claim.

## Current Mission & Next Action
1. Initialize your BRIEFING.md and progress.md in `C:\Users\omarp\matrix\.agents\teamwork\orchestrator_2`.
2. Start your recurring heartbeat cron (`schedule`).
3. Spawn replacement worker (`worker_m1_3`) to apply the verified patches from:
   - `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1\handoff.md` (Emerald pavilion winding)
   - `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1\handoff.md` (Aspect ratio double-scaling fix)
   - `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_quads_1\handoff.md` (Princess/Pear facet planarity)
4. Worker verifies that `powershell -File tools/check_clean_room.ps1` passes with 0 violations and `powershell -File tools/run_tests.ps1` passes with 100% green tests (~160 tests).
5. Run gate verification, commit all work to `feature/gem-engine`, and report victory claim to parent (`423ccf4c-3392-4eec-abad-96640a0c491d`).
