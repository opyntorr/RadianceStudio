# Original User Request

## 2026-10-16T23:19:31Z

Implement the complete Gem Engine (`GemEngine`) for **RadianceStudio** in C# .NET 8 / Rhino 8, including canonical 1.0mm gem mesh generators, arc-length curve distribution, orthonormal frame evaluation, and affine 4x4 transformation matrices, validated with rigorous automated xUnit tests and Clean-Room Chinese Wall compliance.

Working directory: C:\Users\omarp\matrix
Integrity mode: benchmark

## Requirements

### R1. Canonical Gem Cut Models & Procedural Meshes
Generate normalized canonical 1.0 mm gem meshes centered at the origin $(0,0,0)$ with table pointing towards $+Z$ and culet at $-Z$ for:
- Round Brilliant (57 facets, table 56%, crown 34.5°, pavilion 40.75°)
- Princess (square modified brilliant)
- Oval (aspect ratio ~1.45)
- Pear / Teardrop (aspect ratio ~1.55)
- Emerald (step cut, aspect ratio ~1.40)
- Marquise (aspect ratio ~1.95)

Follow specifications defined in `specs/gem_engine.spec.md` and `specs/gem_proportions.json`.

### R2. Arc-Length Curve Distribution & Orthonormal Frame Evaluation
Implement the placement calculation of $N$ stones along 3D space curves:
- Usable curve length calculation subtracting `StartOffset` and `EndOffset`.
- Arc-length division matching target stone diameter and inter-gem spacing ($S_{step} = \text{GemSize} + \text{Spacing}$).
- Computation of tangent $\vec{T}$, surface normal $\vec{N}$ (or Frenet principal normal if surface not supplied), and binormal $\vec{B} = \vec{T} \times \vec{N}$ with Gram-Schmidt orthogonalization.
- Composition of $4 \times 4$ affine transformation matrices ($M_i$) scaling and orienting each canonical gem instance in microsecond-scale performance.

### R3. Clean-Room Compliance & Git Isolation
- Strictly adhere to the Chinese Wall protocol: ZERO access to `_dirty_room/` or decompiled proprietary code.
- Pass `tools/check_clean_room.ps1` with zero infractions.
- Work strictly inside git branch `feature/gem-engine`.

### R4. Automated Testing & Verification
- Implement unit tests covering every gem cut, linear scaling by diameter, stone count calculation, arc distance evaluation, and matrix orthonormal determinant checks ($\det(R) \approx 1.0$).
- All tests must pass cleanly via `tools/run_tests.ps1` and `dotnet test`.

## Acceptance Criteria

### Geometric Precision & Proportions
- [ ] Every canonical gem cut model matches the exact ratios in `specs/gem_proportions.json`.
- [ ] Total depth, crown height, and pavilion depth scale strictly linearly with diameter.
- [ ] Non-square cuts scale major and minor axes according to their canonical aspect ratio.

### Mathematical Orthonormality & Placement
- [ ] Calculated transformation matrices have mutually orthogonal basis vectors ($\vec{T} \cdot \vec{N} < 10^{-6}$, $\vec{T} \cdot \vec{B} < 10^{-6}$, $\vec{N} \cdot \vec{B} < 10^{-6}$).
- [ ] Inter-gem distance along the curve strictly respects `Spacing` tolerance ($< 0.001\text{ mm}$).
- [ ] Performance benchmark: Placement calculation of 500 stones executes in $< 10\text{ ms}$.

### Test Execution & Clean-Room Verification
- [ ] `powershell -File tools/check_clean_room.ps1` reports 0 violations.
- [ ] `dotnet test RadianceStudio/RadianceStudio.sln` passes with 100% success rate (all tests green, 0 failures).
- [ ] All changes committed to branch `feature/gem-engine`.
