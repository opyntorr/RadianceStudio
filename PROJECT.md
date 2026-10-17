# Project: GemEngine for RadianceStudio

## Architecture
RadianceStudio is a C# .NET 8 jewelry CAD system running within Rhino 8.
The `GemEngine` provides mathematical foundations for procedural gem geometry, curve distribution, and affine matrix placement.

### Module Boundaries
- `RadianceStudio.Core`: Pure headless .NET 8 library with zero unmanaged or RhinoCommon dependencies.
  - `RadianceStudio.Core.Enums`: Cut types (`GemCutType`).
  - `RadianceStudio.Core.Models`: Canonical gem dimensions (`GemDimensions`), placement results (`GemPlacement`), and procedural mesh definitions (`GemMesh`, `Face3D`).
  - `RadianceStudio.Core.Geometry`: Vector and matrix mathematics (`Vector3D`, `Point3D`, `Matrix4x4D`, `OrthonormalFrame`, `ICurve3D`, `ISurface3D`, `SyntheticCurve3D`).
  - `RadianceStudio.Core.Services`: Canonical mesh generators (`IGemMeshGenerator`, `RoundBrilliantMeshGenerator`, `PrincessMeshGenerator`, `OvalMeshGenerator`, `PearMeshGenerator`, `EmeraldMeshGenerator`, `MarquiseMeshGenerator`) and placement engine (`IGemEngine`, `GemEngine`).
- `RadianceStudio.Core.Tests`: Automated xUnit 2.5.3 test suite providing 100% headless verification of all geometry, math, proportions, meshes, curve placements, and performance benchmarks.

### Data Flow
1. **User / Tool Input**: Curve rail, optional target surface, gem cut, gem size, spacing, offsets, rotation, normal flip.
2. **Arc-Length Calculation**: Usable length $L_{usable} = L - (\text{StartOffset} + \text{EndOffset})$; Stone count $N = \lfloor (L_{usable} - d) / S_{step} \rfloor + 1$; Arc positions $s_i = \text{StartOffset} + \frac{d}{2} + i \cdot S_{step}$.
3. **Orthonormal Frame Evaluation**: Tangent $\vec{T}_i$, Normal $\vec{N}_i$ (surface normal or Frenet normal, inverted if `FlipNormal`), Binormal $\vec{B}_i = \vec{T}_i \times \vec{N}_i$, Gram-Schmidt $\vec{T}_i^* = \vec{N}_i \times \vec{B}_i$.
4. **Affine Matrix Composition**: Closed-form row-major $4 \times 4$ transformation matrix $M_i = [\vec{B}_i \ \vec{T}_i^* \ \vec{N}_i \ \vec{P}_i] \cdot R_z(\theta) \cdot S(d, \text{AR})$.
5. **Procedural Canonical Meshes**: Normalized 1.0mm mesh models centered at origin $(0,0,0)$ with table at $+Z$, culet at $-Z$, transformed by $M_i$.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Gram-Schmidt Frame Orthogonalization | Enforces strict orthonormality ($\vec{T}\cdot\vec{N} < 10^{-6}, \det(R) \approx 1.0$) | M1 | `specs/gem_engine.spec.md` §3.2 |
| 2 | Affine 4x4 Matrix Composition | Closed-form row-major 16-element matrix $M_i$ with translation, rotation, scale | M1 | `specs/gem_engine.spec.md` §3.3 |
| 3 | Normal Inversion (`FlipNormal`) | Flips normal $\vec{N} \to -\vec{N}$, preserving right-handed frame | M1 | `specs/gem_engine.spec.md` §3.2 |
| 4 | Azimuthal Rotation Around Normal | Rotates gem around normal vector by $\theta \in [0, 360^\circ]$ | M1 | `specs/gem_engine.spec.md` §3.3 |
| 5 | Canonical Gem Proportions Catalog | Exact ratios for 6 cuts in `specs/gem_proportions.json` | M1 | `specs/gem_proportions.json` |
| 6 | Linear Depth & Height Scaling | Total depth, crown height, pavilion depth scale linearly with diameter | M1 | `specs/gem_proportions.json` |
| 7 | Aspect Ratio Scaling | Major axis scaled by AspectRatio for Oval, Pear, Emerald, Marquise | M1 | `specs/gem_proportions.json` |
| 8 | Procedural Canonical Gem Meshes | 1.0mm normalized meshes with exact facet counts (RB: 57, Princess: 49, Oval: 57, Pear: 58, Emerald: 50, Marquise: 57) | M1 | `ORIGINAL_REQUEST.md` R1 |
| 9 | Usable Length Calculation | $L_{usable} = L - (\text{StartOffset} + \text{EndOffset})$ | M1 | `specs/gem_engine.spec.md` §3.1 |
| 10 | Stone Count Evaluation | $N = \lfloor (L_{usable} - d) / S_{step} \rfloor + 1$ | M1 | `specs/gem_engine.spec.md` §3.1 |
| 11 | Arc Distance Evaluation | Array of exact center positions $s_i$; spacing tolerance $< 0.001$ mm | M1 | `specs/gem_engine.spec.md` §3.1 |
| 12 | Curve Space Abstraction & Tangents | Parametric evaluation of 3D curve point and tangent vector | M1 | `specs/gem_engine.spec.md` §3.2 |
| 13 | Surface Normal Evaluation & Frenet Fallback | Surface normal projection or Frenet curve normal evaluation | M1 | `specs/gem_engine.spec.md` §3.2 |
| 14 | Gem Engine Placement Calculation | End-to-end placement calculation returning list of `GemPlacement` | M1 | `specs/gem_engine.spec.md` §3.3 |
| 15 | Performance Benchmark & Hardening | Placement of 500 stones executes in $< 10$ ms; corner case validation | M2 | `ORIGINAL_REQUEST.md` R4 |
| 16 | Clean-Room Audit & Git Commit | Chinese Wall validation (0 infractions), 100% tests pass, commit to `feature/gem-engine` | M2 | `ORIGINAL_REQUEST.md` R3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | GemEngine Core Implementation | Math, canonical meshes (6 cuts), curve distribution, affine 4x4 matrix placement in RadianceStudio.Core | none | IN_PROGRESS |
| M2 | Test Suite, Benchmark & Final Verification | Full xUnit test suite (Tiers 1-4 + Tier 5 hardening), 500-stone benchmark (<10ms), clean-room audit, git commit to feature/gem-engine | M1 | PLANNED |

## Interface Contracts
### Geometry & Math
- `public readonly record struct Vector3D(double X, double Y, double Z)`:
  - Vector arithmetic (`+`, `-`, `*`), `Length`, `LengthSquared`, `Normalize()`, `Dot(Vector3D)`, `Cross(Vector3D)`.
- `public readonly record struct Point3D(double X, double Y, double Z)`:
  - Point displacement (`Point3D + Vector3D`), vector between points (`Point3D - Point3D`).
- `public readonly struct Matrix4x4D`:
  - 16 double values in row-major order: `M00` through `M33`, array accessor `this[int row, int col]`, `ToRowMajorArray()`.
  - `Determinant3x3()`, `Transform(Point3D)`, `Transform(Vector3D)`, `Multiply(Matrix4x4D)`.
- `public readonly struct OrthonormalFrame`:
  - `Vector3D Tangent`, `Vector3D Normal`, `Vector3D Binormal`.
  - Factory: `OrthonormalFrame.Create(Vector3D tangent, Vector3D candidateNormal, bool flipNormal = false)`.
  - Guaranteed: `Dot(T, N) < 1e-9`, `Dot(T, B) < 1e-9`, `Dot(N, B) < 1e-9`, `Det == 1.0`.

### Gem Meshes & Proportions
- `public record GemMesh`:
  - `IReadOnlyList<Point3D> Vertices`, `IReadOnlyList<int[]> Faces`, `int FacetCount`, `GemCutType CutType`, `double DiameterMm`.
- `public interface IGemMeshGenerator`:
  - `GemMesh GenerateCanonicalMesh(GemCutType cutType);`
  - `GemMesh GenerateMesh(GemCutType cutType, double diameterMm);`

### Curve & Placement Engine
- `public interface ICurve3D`:
  - `double Length { get; }`
  - `Point3D PointAtArcLength(double s);`
  - `Vector3D TangentAtArcLength(double s);`
  - `Vector3D NormalAtArcLength(double s);` (Frenet normal)
- `public interface ISurface3D`:
  - `Vector3D NormalAtPoint(Point3D point);`
- `public interface IGemEngine`:
  - `int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm);`
  - `IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm);`
  - `IReadOnlyList<GemPlacement> CalculatePlacements(ICurve3D curve, ISurface3D? surface, GemCutType cutType, double gemSizeMm, double spacingMm, double startOffsetMm = 0, double endOffsetMm = 0, bool flipNormal = false, double rotationAngleDeg = 0);`

## Code Layout
- `RadianceStudio/src/RadianceStudio.Core/`:
  - `Geometry/`: `Vector3D.cs`, `Point3D.cs`, `Matrix4x4D.cs`, `OrthonormalFrame.cs`, `ICurve3D.cs`, `ISurface3D.cs`, `LineCurve3D.cs`, `HelixCurve3D.cs`.
  - `Models/`: `GemDimensions.cs`, `GemPlacement.cs`, `GemMesh.cs`.
  - `Services/`:
    - `IGemEngine.cs`, `GemEngine.cs`.
    - `MeshGenerators/`: `IGemMeshGenerator.cs`, `GemMeshFactory.cs`, `RoundBrilliantMeshGenerator.cs`, `PrincessMeshGenerator.cs`, `OvalMeshGenerator.cs`, `PearMeshGenerator.cs`, `EmeraldMeshGenerator.cs`, `MarquiseMeshGenerator.cs`.
- `RadianceStudio/tests/RadianceStudio.Core.Tests/`:
  - `Geometry/`: `OrthonormalFrameTests.cs`, `Matrix4x4DTests.cs`.
  - `Models/`: `GemDimensionsTests.cs`.
  - `Meshes/`: `CanonicalGemMeshTests.cs`.
  - `Placement/`: `GemEnginePlacementTests.cs`.
  - `Benchmarks/`: `GemPlacementBenchmarkTests.cs`.
