# BRIEFING — 2026-10-16T23:36:00Z

## Mission
Design GemMesh and procedural mesh generator specifications for all 6 canonical cuts (RoundBrilliant, Princess, Oval, Pear, Emerald, Marquise) in RadianceStudio.Core.

## 🔒 My Identity
- Archetype: Specification Miner
- Roles: Teamwork specialist, Specification Miner
- Working directory: C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1
- Original parent: 4d69e4bc-084b-47a0-ad74-5668ab8750e2
- Milestone: M1 - Procedural Mesh Generators & GemMesh

## 🔒 Key Constraints
- NEVER access, grep, search, or mention _dirty_room/ or any decompiled proprietary code. STRICT CHINESE WALL.
- Do NOT implement anything — read-only and specification only.
- Document exact procedural mesh generation blueprints for all 6 canonical cut models.
- Formulate vertex construction formulas for 1.0mm normalized scale ($d_0 = 1.0$ mm), centered at origin at girdle mid-plane ($Z = 0$), table pointing towards $+Z$, culet/keel towards $-Z$.
- Exact facet topologies and facet counts per cut.
- Provide concrete C# class blueprints and test recommendations for CanonicalGemMeshTests.

## Current Parent
- Conversation ID: 4d69e4bc-084b-47a0-ad74-5668ab8750e2
- Updated: 2026-10-16T23:36:00Z

## Task Summary
- **What to build**: Specification and architectural blueprints for `GemMesh` data model and procedural generators for RoundBrilliant, Princess, Oval, Pear, Emerald, and Marquise.
- **Success criteria**: Complete vertex math, facet index connectivity, normal calculation, facet counts matching gemological standards, error handling, edge cases, and unit test specifications.
- **Interface contracts**: C:\Users\omarp\matrix\specs\gem_engine.spec.md, C:\Users\omarp\matrix\specs\gem_proportions.json
- **Code layout**: C:\Users\omarp\matrix\PROJECT.md

## Key Decisions Made
- Canonical coordinate frame centered at (0,0,0) at girdle midplane; table normal along +Z, culet along -Z.
- Girdle band faces omitted to maintain exact gemological facet counts in `GemMesh.Faces` (RB: 57, Princess: 49, Oval: 57, Pear: 58, Emerald: 50, Marquise: 57) while maintaining closed 2-manifold watertight mesh.
- Non-square cuts scale along Y by AspectRatio (Oval 1.45, Pear 1.55, Emerald 1.40, Marquise 1.95).
- Designed `GemMesh`, `Face3D`, `IGemMeshGenerator`, `BaseGemMeshGenerator`, `GemMeshFactory`, and 6 concrete generator blueprints.
- Created `CanonicalGemMeshTests` test specification covering facet counts, bounding box dimensions, linear depth scaling, and outward normal directions.

## Artifact Index
- C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1\handoff.md — Final specification & handoff report
- C:\Users\omarp\matrix\.agents\teamwork\spec_miner_m1_mesh_1\progress.md — Liveness heartbeat
