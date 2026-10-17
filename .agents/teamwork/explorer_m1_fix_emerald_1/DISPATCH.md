# Dispatch Directive: M1 Fix Explorer 1 (Emerald Cut Pavilion Winding)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1`

## Mission
Analyze and specify the exact fix for the inverted pavilion winding order and non-manifold edge orientation in `EmeraldMeshGenerator.cs`.

## Failure Evidence from Reviewer 1 & Challenger 1
- `EmeraldMeshGenerator.cs` lines 122-140:
  - Step 1: `faces.Add(new[] { 24 + j, 24 + next, 32 + next, 32 + j });`
  - Step 2: `faces.Add(new[] { 32 + j, 32 + next, 40 + next, 40 + j });`
  - Step 3: `faces.Add(new[] { 40 + j, 40 + next, 48 + next, 48 + j });`
- Problem: Edge (24+j, 24+next) is traversed in the same direction by Crown Step 3 and Pavilion Step 1, breaking 2-manifold orientation. Newell's method computes inward normals ($N_z > 0$).
- Target: All 24 pavilion faces must be CCW viewed from exterior, with outward normals ($N_z < 0$), sharing edges with opposite directed orientations.

## Output
Write fix proposal to `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1\handoff.md`.
NEVER access `_dirty_room/`.


## 2026-10-16T23:57:02Z
Read C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1. Formulate fix strategy for Emerald pavilion winding. NEVER access _dirty_room/. Write handoff.md and send completion message.
