# Plan: GemEngine Implementation & Clean-Room Validation

## Objective
Implement complete `GemEngine` for RadianceStudio in C# .NET 8 / Rhino 8 strictly on branch `feature/gem-engine` with zero clean-room infractions, 100% test pass rate, and rigorous mathematical and geometric accuracy.

## Phase 0: Survey & Spec Mining
- Dispatch 3 Explorers / Spec Miners:
  1. Spec Miner: Extract detailed geometric, mathematical, and algorithmic specifications from `specs/gem_engine.spec.md` and `specs/gem_proportions.json`.
  2. Codebase Explorer: Survey repository structure, existing solution/projects in `RadianceStudio/`, git status, build tools, and scripts.
  3. Testing & Verification Explorer: Survey test project structure, test runners, benchmark requirements, and clean-room check script `tools/check_clean_room.ps1`.
- Verify Clean-Room boundary: Subagents must NOT touch `_dirty_room/`.

## Phase 1: Architecture & Milestone Definition (PROJECT.md)
- Synthesize survey findings.
- Formulate `PROJECT.md` with:
  - Architecture and component boundaries.
  - Feature Inventory (all requirements mapped).
  - Milestones with dependencies.
  - Interface contracts (types, signatures, error handling).
  - Code layout convention.

## Phase 2: Iterative Execution & Testing
- Iterate through milestones via Explorer → Worker → Reviewers → Challengers → Auditor cycle:
  - Milestone 1: Math & Geometry Foundations (Affine 4x4, Orthonormal frames, Gram-Schmidt, transformation matrix composition).
  - Milestone 2: Canonical Gem Cut Mesh Generators (Round Brilliant, Princess, Oval, Pear, Emerald, Marquise per proportions).
  - Milestone 3: Curve Distribution Engine (Arc-length calculation, spacing, offsets, frame orientation, benchmark <10ms for 500 stones).
  - Milestone 4: Integration & Full Test Suite (100% test coverage, xUnit tests).

## Phase 3: Final Verification & Delivery
- Run `tools/check_clean_room.ps1` with 0 infractions.
- Run full test suite (`dotnet test` / `tools/run_tests.ps1`).
- Commit all changes to `feature/gem-engine`.
- Forensic Audit verification.
- Send completion handoff report and victory claim to Sentinel.
