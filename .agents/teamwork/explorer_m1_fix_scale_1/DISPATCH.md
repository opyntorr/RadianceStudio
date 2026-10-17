# Dispatch Directive: M1 Fix Explorer 2 (Aspect Ratio Double-Scaling Reconciliation)

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1`

## Mission
Analyze and resolve the end-to-end double-scaling defect on non-square cuts (`Oval`, `Pear`, `Emerald`, `Marquise`).

## Failure Evidence from Challenger 1
- `AdversarialStressTests.cs`:
  When canonical meshes are transformed by $M_i$ from `GemEngine.CalculatePlacements`:
  - Oval (AR=1.45) becomes $1.45^2 = 2.1025$.
  - Pear (AR=1.55) becomes $1.55^2 = 2.4025$.
  - Emerald (AR=1.40) becomes $1.40^2 = 1.9600$.
  - Marquise (AR=1.95) becomes $1.95^2 = 3.8025$.
- Cause: The mesh generators bake `AspectRatio` into canonical vertices ($ry0 = rx0 * AR$), AND `CreatePlacement` scales the Tangent axis by `sy = gemSizeMm * aspectRatio`.
- Target: Determine whether canonical meshes should be normalized to unit aspect ratio (width=1, length=1) and let $M_i$ scale by $AR$, OR canonical meshes maintain $AR$ and $M_i$ scales uniformly ($sx = d, sy = d, sz = d$), OR another standard approach matching `ORIGINAL_REQUEST.md` R1 and `specs/gem_engine.spec.md` §1 & §3.3.

## Output
Write fix proposal to `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1\handoff.md`.
NEVER access `_dirty_room/`.


## 2026-10-16T23:57:02Z
Read C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1. Formulate fix strategy for non-square cut double-scaling. NEVER access _dirty_room/. Write handoff.md and send completion message.
