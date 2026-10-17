# Dispatch Directive: M1 Mesh Generators Explorer

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1`

## Mission
Analyze and specify exact procedural mesh generation blueprints for all 6 canonical cut models (`RoundBrilliant`, `Princess`, `Oval`, `Pear`, `Emerald`, `Marquise`) in `RadianceStudio.Core`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`
- `C:\Users\omarp\matrix\specs\gem_proportions.json`

## Scope & Analysis Tasks
1. Design `GemMesh` data model (Vertices, Normals, Faces, FacetCount, CutType, DiameterMm).
2. Formulate vertex construction formulas for 1.0mm normalized scale ($d_0 = 1.0$ mm):
   - Centered at origin $(0,0,0)$ at girdle mid-plane ($Z = 0$).
   - Table pointing towards $+Z$, culet/keel towards $-Z$.
   - Linear depth scaling and aspect ratio scaling on major/minor axes.
3. Formulate face topologies and facet counts:
   - Round Brilliant: 57 facets (33 crown: 1 table, 8 stars, 8 bezels, 16 upper girdle; 24 pavilion: 8 mains, 16 lower girdle).
   - Princess: 49 facets (25 crown: 1 table, 24 bezel/star/corner; 24 pavilion).
   - Oval: 57 facets.
   - Pear: 58 facets.
   - Emerald: 50 facets (octagonal step cut, linear keel line).
   - Marquise: 57 facets (navette outline).
4. Provide concrete C# class blueprints and test recommendations for `CanonicalGemMeshTests`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1\handoff.md` and send message to caller.


## 2026-10-16T23:29:18Z
Read C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1. Design GemMesh and procedural mesh generators for all 6 canonical cuts. NEVER access _dirty_room/. Write your findings to handoff.md and send message when done.
