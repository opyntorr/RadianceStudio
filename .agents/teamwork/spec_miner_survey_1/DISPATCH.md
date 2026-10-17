# Dispatch Directive: Gem Spec & Math Miner

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\spec_miner_survey_1`

## Mission
Analyze specifications and extract complete geometric, mathematical, and algorithmic requirements for GemEngine.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`
- `C:\Users\omarp\matrix\specs\gem_proportions.json`

## Scope & Analysis Tasks
1. Enumerate canonical cut proportions and geometry for all 6 cuts:
   - Round Brilliant (57 facets, table 56%, crown 34.5°, pavilion 40.75°, culet, etc.)
   - Princess (square modified brilliant)
   - Oval (aspect ratio ~1.45)
   - Pear / Teardrop (aspect ratio ~1.55)
   - Emerald (step cut, aspect ratio ~1.40)
   - Marquise (aspect ratio ~1.95)
2. Detail coordinate conventions: 1.0mm normalized scale, centered at origin (0,0,0), table at +Z, culet at -Z, scaling behavior (linear depth scaling, aspect ratio scaling for major/minor axes).
3. Detail curve distribution mathematics:
   - Usable length (subtracting StartOffset and EndOffset).
   - Step distance $S_{step} = \text{GemSize} + \text{Spacing}$.
   - Stone count calculation.
   - Orthonormal basis: Tangent $\vec{T}$, surface normal $\vec{N}$ (or Frenet principal normal if surface not supplied), binormal $\vec{B} = \vec{T} \times \vec{N}$ with Gram-Schmidt orthogonalization ($\vec{T} \cdot \vec{N} < 10^{-6}$, etc.).
   - Affine 4x4 transformation matrix composition ($M_i$).
4. Performance and tolerance requirements (<10ms for 500 stones, inter-gem distance tolerance < 0.001mm, $\det(R) \approx 1.0$).

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write your findings and handoff report to:
`C:\Users\omarp\matrix\.agents\teamwork\spec_miner_survey_1\handoff.md`
Report back via `send_message` when complete.


## 2026-10-16T23:22:23Z
Read C:\Users\omarp\matrix\.agents\teamwork\spec_miner_survey_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\spec_miner_survey_1. Analyze gem specifications, proportions, geometry, and curve mathematics. NEVER touch _dirty_room/. Write findings to handoff.md in your working directory and send a completion message to your caller.
