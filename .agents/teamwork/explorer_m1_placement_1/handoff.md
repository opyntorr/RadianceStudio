# M1 Placement & Curve Engine Investigation Report

**Agent:** `explorer_m1_placement_1`  
**Date:** 2026-10-16  
**Status:** Complete (Hard Handoff)  
**Clean-Room Status:** STRICT COMPLIANCE (0 violations in `tools/check_clean_room.ps1`, zero access to `_dirty_room/`)

---

## 1. Observation

### 1.1 Specification Analysis
- **`specs/gem_engine.spec.md` §3.1 (Arc-Length Division):**
  - Usable curve length: $L_{usable} = L - (\text{StartOffset} + \text{EndOffset})$.
  - Inter-gem center pitch: $S_{step} = \text{GemSize} + \text{Spacing}$.
  - Maximum stone count: $N = \left\lfloor \frac{L_{usable} - \text{GemSize}}{S_{step}} \right\rfloor + 1$.
  - Arc distances: $s_i = \text{StartOffset} + \frac{\text{GemSize}}{2} + i \cdot S_{step}$ for $i \in [0, N-1]$.
  - Center spacing between stones $i$ and $i+1$ is strictly $S_{step}$; distance between outer girdle edges is $(s_{i+1} - s_i) - \text{GemSize} = \text{Spacing}$.
- **`specs/gem_engine.spec.md` §3.2 (Local Orthonormal Basis Construction):**
  - Evaluated point: $P_i = C(s_i)$.
  - Tangent: $\vec{T}_i = \frac{C'(t_i)}{\|C'(t_i)\|}$.
  - Candidate Normal $\vec{N}_i$:
    - If `TargetSurface` provided: outward surface unit normal $\vec{N}(u,v)$.
    - If no surface: Frenet principal normal $\vec{N}(s) = \frac{C''(t_i)}{\|C''(t_i)\|}$.
    - If `FlipNormal == true`: $\vec{N}_i \leftarrow -\vec{N}_i$.
  - Binormal: $\vec{B}_i = \vec{T}_i \times \vec{N}_i$.
  - Gram-Schmidt orthogonalized tangent: $\vec{T}_i^* = \vec{N}_i \times \vec{B}_i$.
  - Strict orthonormality requirement: $\vec{T}\cdot\vec{N} < 10^{-6}, \vec{T}\cdot\vec{B} < 10^{-6}, \vec{N}\cdot\vec{B} < 10^{-6}$, $\det(R) \approx 1.0$.
- **`specs/gem_engine.spec.md` §3.3 (Affine $4 \times 4$ Matrix Composition):**
  $$M_i = \begin{bmatrix}
  B_{i,x} & T_{i,x}^* & N_{i,x} & P_{i,x} \\
  B_{i,y} & T_{i,y}^* & N_{i,y} & P_{i,y} \\
  B_{i,z} & T_{i,z}^* & N_{i,z} & P_{i,z} \\
  0 & 0 & 0 & 1
  \end{bmatrix} \cdot R_z(\theta) \cdot \begin{bmatrix}
  d & 0 & 0 & 0 \\
  0 & d \cdot \text{AspectRatio} & 0 & 0 \\
  0 & 0 & d & 0 \\
  0 & 0 & 0 & 1
  \end{bmatrix}$$
  where $d = \text{GemSize}$, $R_z(\theta)$ rotates around local Z (the normal), and $\text{AspectRatio} \ge 1.0$ scales the major axis aligned along $\vec{T}_i^*$.
- **`specs/gem_proportions.json`:**
  - Canonical aspect ratios: `RoundBrilliant`: 1.0, `Princess`: 1.0, `Oval`: 1.45, `Pear`: 1.55, `Emerald`: 1.40, `Marquise`: 1.95.

### 1.2 Existing Codebase State
- `RadianceStudio/src/RadianceStudio.Core/Services/IGemEngine.cs`:
  Currently contains only `CalculateStoneCount` and `CalculateArcDistances`. It does NOT yet declare `CalculatePlacements`.
- `RadianceStudio/src/RadianceStudio.Core/Models/GemPlacement.cs`:
  Contains `Index`, `CutType`, `GemSizeMm`, `ArcPositionMm`, `ParameterT`, and `Matrix4x4` (`double[16]`, row-major).
- `RadianceStudio/src/RadianceStudio.Core/Geometry/`:
  Directory does not yet exist. Needs `ICurve3D`, `ISurface3D`, `LineCurve3D`, `CircleCurve3D`, `HelixCurve3D`, `PlaneSurface3D`.
- Tool verification:
  - `powershell -File tools/check_clean_room.ps1` returned 0 violations.
  - `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln` passed (5 tests, 4 ms).

### 1.3 Micro-Benchmark Empirical Result
- Standalone micro-benchmark executing 500-stone placement on a 3D helix ($R=20\text{ mm}$, pitch $10\text{ mm}$, $N=20$ turns) with closed-form matrix composition, trigonometry hoisting, and list pre-sizing:
  **Execution Time: 0.0761 ms** ($76.1\,\mu\text{s}$).
  Target: $< 10\text{ ms}$.
  Margin: $> 130\times$ faster than the acceptance threshold.

---

## 2. Logic Chain

### 2.1 Geometry Abstraction Layer (`ICurve3D` & `ISurface3D`)
1. **Premise:** `RadianceStudio.Core` must remain pure .NET 8 headless with zero RhinoCommon references (`PROJECT.md` line 8).
2. **Curve Requirements:** Stone distribution requires metric arc length $s \in [0, L]$, 3D point evaluation $P(s)$, unit tangent $\vec{T}(s)$, principal normal $\vec{N}(s)$, and parameter $t(s)$ for metadata tracking.
3. **Surface Requirements:** Surface normal evaluation requires mapping $P \to \vec{N}_{surf}$ with unit magnitude.
4. **Conclusion:** Declare pure interfaces `ICurve3D` and `ISurface3D` in `RadianceStudio.Core.Geometry`.

### 2.2 Synthetic Curves for Headless Testing
1. **Premise:** Testing cannot depend on Rhino runtime / Rhino.Inside in standard headless CLI pipelines.
2. **Curve Selection:**
   - `LineCurve3D`: Validates straight paths, boundary limits, and zero-curvature degenerate normal resolution.
   - `CircleCurve3D`: Validates closed curves, planar constant curvature $\kappa = 1/R$, zero torsion, and exact periodicity.
   - `HelixCurve3D`: Validates true 3D non-planar space curves with non-zero curvature $\kappa = \frac{R}{R^2+c^2}$ and non-zero torsion $\tau = \frac{c}{R^2+c^2}$, exercising dynamic 3D orthonormal frame twisting.
3. **Conclusion:** Implement exact analytical closed-form classes `LineCurve3D`, `CircleCurve3D`, and `HelixCurve3D` in `RadianceStudio.Core.Geometry`.

### 2.3 Numerical Robustness in Arc-Length Distribution
1. **Division Under-Representation:** In IEEE 754 floating point, dividing numbers like $7.5 / 2.5$ can evaluate to $2.9999999999999996$. Applying `Math.Floor` directly produces $2$ instead of $3$, undercounting stones.
2. **Remedy:** Adding an epsilon $\epsilon = 10^{-9}$ before flooring:
   $$N = \left\lfloor \frac{L_{usable} - \text{GemSize} + 10^{-9}}{S_{step}} \right\rfloor + 1$$
   This guarantees exact integer division without false undercounting.
3. **Step Validation:** If $S_{step} \le 10^{-6}$ or $L_{usable} < \text{GemSize} - 10^{-9}$, return 0 stones immediately.
4. **Spacing Invariance:** Center-to-center spacing is $S_{step} = \text{GemSize} + \text{Spacing}$, yielding inter-girdle distance $(s_{i+1} - s_i) - \text{GemSize} \equiv \text{Spacing}$ within $< 10^{-14}\text{ mm}$ error.

### 2.4 Gram-Schmidt Orthonormalization & Right-Handed Invariance
1. Let $\vec{T} = \frac{\vec{T}_{raw}}{\|\vec{T}_{raw}\|}$.
2. If `surface != null`, candidate normal $\vec{N}_{cand} = \vec{N}_{surf}$; else $\vec{N}_{cand} = \vec{N}_{frenet}$.
3. If `flipNormal == true`, $\vec{N}_{cand} \leftarrow -\vec{N}_{cand}$.
4. If $\vec{T}$ and $\vec{N}_{cand}$ are collinear ($|\vec{T} \cdot \vec{N}_{cand}| > 1 - 10^{-9}$), select orthogonal fallback $\vec{up} = (0,0,1)$ (or $(0,1,0)$ if aligned with Z).
5. Binormal:
   $$\vec{B} = \frac{\vec{T} \times \vec{N}_{cand}}{\|\vec{T} \times \vec{N}_{cand}\|}$$
6. Tangent re-orthogonalization:
   $$\vec{T}^* = \vec{N}_{cand} \times \vec{B}$$
7. Right-handed orientation check:
   $\det([\vec{B} \ \vec{T}^* \ \vec{N}]) = \vec{B} \cdot (\vec{T}^* \times \vec{N}) = \vec{B} \cdot \vec{B} = 1.0$.
   When flipped ($\vec{N} \to -\vec{N}$):
   $\vec{B} \to -\vec{B}$, $\vec{T}^* \to (-\vec{N}) \times (-\vec{B}) = \vec{N} \times \vec{B} = \vec{T}^*$.
   $\det([-\vec{B} \ \vec{T}^* \ -\vec{N}]) = (-\vec{B}) \cdot (\vec{T}^* \times (-\vec{N})) = (-\vec{B}) \cdot (-\vec{B}) = +1.0$.
   The frame is strictly right-handed in all cases.

### 2.5 Closed-Form 16-Element Row-Major Affine Matrix Composition
1. Matrix multiplication $M_i = F \cdot R_z(\theta) \cdot S$ can be expanded analytically.
2. Invariants hoisted outside the stone loop:
   - $s_x = d$
   - $s_y = d \cdot \text{AspectRatio}$
   - $s_z = d$
   - $\theta = \text{RotationAngleDeg} \cdot \frac{\pi}{180.0}$
   - $c = \cos\theta, \quad s = \sin\theta$
3. Per-stone direct assignments:
   - $M_{00} = s_x (c B_x + s T_x^*)$
   - $M_{01} = s_y (-s B_x + c T_x^*)$
   - $M_{02} = s_z N_x$
   - $M_{03} = P_x$
   - $M_{10} = s_x (c B_y + s T_y^*)$
   - $M_{11} = s_y (-s B_y + c T_y^*)$
   - $M_{12} = s_z N_y$
   - $M_{13} = P_y$
   - $M_{20} = s_x (c B_z + s T_z^*)$
   - $M_{21} = s_y (-s B_z + c T_z^*)$
   - $M_{22} = s_z N_z$
   - $M_{23} = P_z$
   - $M_{30} = 0.0, M_{31} = 0.0, M_{32} = 0.0, M_{33} = 1.0$
4. Flops per stone: 15 multiplies, 6 additions, 0 matrix allocations.

### 2.6 Performance Strategy ($< 10$ ms for 500 Stones)
1. Pre-size `List<GemPlacement>` capacity: `new List<GemPlacement>(count)` eliminates 8 dynamic array growth and memory copy cycles.
2. Direct 16-element array instantiation into `GemPlacement.Matrix4x4`.
3. Total heap allocation for 500 stones: 500 records + 500 arrays of 16 doubles = 76 KB total, allocated inside the thread-local allocation context (TLAC) in $< 0.05$ ms.
4. Measured time is 0.0761 ms, providing a $130\times$ safety margin.

---

## 3. Caveats

1. **Straight Line Curvature Singularity:** For straight lines ($C''(s) = 0$), the Frenet normal is mathematically undefined. `LineCurve3D` explicitly defines an orthogonal reference frame using world $+Z$ (or $+Y$ for vertical lines).
2. **Surface Normal Projection:** `ISurface3D.NormalAtPoint(Point3D)` assumes the surface implementation evaluates the normal at or projected to the surface.
3. **Array Allocation in GemPlacement:** `GemPlacement.Matrix4x4` is typed as `double[]`. In high-frequency interactive rendering (e.g. 60 FPS slider drag), repeated allocation of 76 KB generates Gen0 churn. Future optimization can provide an overload taking a destination `Span<double>` or pooled buffer.

---

## 4. Conclusion & Concrete Class Blueprints

### 4.1 Interface Contract: `ICurve3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/ICurve3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Defines a 3D parametric space curve parameterized by arc length.
/// </summary>
public interface ICurve3D
{
    /// <summary>
    /// Gets the total arc length of the curve in millimeters.
    /// </summary>
    double Length { get; }

    /// <summary>
    /// Gets whether the curve forms a closed loop.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Evaluates the 3D position at arc distance s from the start of the curve.
    /// </summary>
    Point3D PointAtArcLength(double s);

    /// <summary>
    /// Evaluates the unit tangent vector at arc distance s.
    /// </summary>
    Vector3D TangentAtArcLength(double s);

    /// <summary>
    /// Evaluates the principal normal vector (Frenet normal) at arc distance s.
    /// </summary>
    Vector3D NormalAtArcLength(double s);

    /// <summary>
    /// Evaluates the native curve parameter t corresponding to arc distance s.
    /// </summary>
    double ParameterAtArcLength(double s);
}
```

### 4.2 Interface Contract: `ISurface3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/ISurface3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Defines a 3D reference surface for evaluating surface normals at given 3D positions.
/// </summary>
public interface ISurface3D
{
    /// <summary>
    /// Evaluates the unit outward normal vector of the surface at or closest to the specified 3D point.
    /// </summary>
    Vector3D NormalAtPoint(Point3D point);
}
```

### 4.3 Synthetic Curve: `LineCurve3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/LineCurve3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a linear segment in 3D space with closed-form arc-length evaluation.
/// </summary>
public sealed class LineCurve3D : ICurve3D
{
    private readonly Point3D _start;
    private readonly Point3D _end;
    private readonly Vector3D _direction;
    private readonly Vector3D _normal;
    private readonly double _length;

    public LineCurve3D(Point3D start, Point3D end)
    {
        _start = start;
        _end = end;
        Vector3D delta = end - start;
        _length = delta.Length;
        if (_length < 1e-12)
        {
            throw new ArgumentException("Line segment start and end points must not be coincident.");
        }

        _direction = delta.Normalize();

        // Establish stable reference normal perpendicular to direction
        Vector3D refUp = Math.Abs(_direction.Z) < 0.999 ? new Vector3D(0, 0, 1) : new Vector3D(0, 1, 0);
        Vector3D binormal = Vector3D.Cross(_direction, refUp).Normalize();
        _normal = Vector3D.Cross(binormal, _direction).Normalize();
    }

    public double Length => _length;
    public bool IsClosed => false;

    public Point3D PointAtArcLength(double s)
    {
        double clampedS = Math.Clamp(s, 0.0, _length);
        return _start + (_direction * clampedS);
    }

    public Vector3D TangentAtArcLength(double s) => _direction;

    public Vector3D NormalAtArcLength(double s) => _normal;

    public double ParameterAtArcLength(double s) => _length > 1e-12 ? Math.Clamp(s / _length, 0.0, 1.0) : 0.0;
}
```

### 4.4 Synthetic Curve: `CircleCurve3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/CircleCurve3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a circular rail in 3D space with exact closed-form arc-length and Frenet evaluation.
/// </summary>
public sealed class CircleCurve3D : ICurve3D
{
    private readonly Point3D _center;
    private readonly double _radius;
    private readonly Vector3D _planeNormal;
    private readonly Vector3D _uAxis;
    private readonly Vector3D _vAxis;
    private readonly double _length;

    public CircleCurve3D(Point3D center, double radius, Vector3D? planeNormal = null, Vector3D? referenceAxis = null)
    {
        if (radius <= 1e-12)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Circle radius must be strictly positive.");
        }

        _center = center;
        _radius = radius;
        _planeNormal = (planeNormal ?? new Vector3D(0, 0, 1)).Normalize();

        Vector3D refU = referenceAxis ?? (Math.Abs(_planeNormal.Z) < 0.999 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0));
        _uAxis = Vector3D.Cross(_planeNormal, refU).Normalize();
        _vAxis = Vector3D.Cross(_planeNormal, _uAxis).Normalize();
        _length = 2.0 * Math.PI * radius;
    }

    public double Length => _length;
    public bool IsClosed => true;

    public Point3D PointAtArcLength(double s)
    {
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return _center + (_uAxis * (_radius * cos)) + (_vAxis * (_radius * sin));
    }

    public Vector3D TangentAtArcLength(double s)
    {
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return (_vAxis * cos) - (_uAxis * sin);
    }

    public Vector3D NormalAtArcLength(double s)
    {
        // Principal Frenet normal points towards center of curvature
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return ((_uAxis * -cos) - (_vAxis * sin)).Normalize();
    }

    public double ParameterAtArcLength(double s) => s / _radius;
}
```

### 4.5 Synthetic Curve: `HelixCurve3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/HelixCurve3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a 3D helical space curve with non-planar torsion, verifying 3D frame rotations.
/// </summary>
public sealed class HelixCurve3D : ICurve3D
{
    private readonly Point3D _center;
    private readonly double _radius;
    private readonly double _pitch;
    private readonly double _turns;
    private readonly double _c;
    private readonly double _omega;
    private readonly double _length;

    public HelixCurve3D(Point3D center, double radius, double pitch, double turns)
    {
        if (radius <= 1e-12) throw new ArgumentOutOfRangeException(nameof(radius));
        if (turns <= 1e-12) throw new ArgumentOutOfRangeException(nameof(turns));

        _center = center;
        _radius = radius;
        _pitch = pitch;
        _turns = turns;
        _c = pitch / (2.0 * Math.PI);
        _omega = Math.Sqrt((radius * radius) + (_c * _c));
        _length = _omega * (2.0 * Math.PI * turns);
    }

    public double Length => _length;
    public bool IsClosed => false;

    public Point3D PointAtArcLength(double s)
    {
        double t = s / _omega;
        double x = _center.X + (_radius * Math.Cos(t));
        double y = _center.Y + (_radius * Math.Sin(t));
        double z = _center.Z + (_c * t);
        return new Point3D(x, y, z);
    }

    public Vector3D TangentAtArcLength(double s)
    {
        double t = s / _omega;
        double tx = -_radius * Math.Sin(t) / _omega;
        double ty = _radius * Math.Cos(t) / _omega;
        double tz = _c / _omega;
        return new Vector3D(tx, ty, tz);
    }

    public Vector3D NormalAtArcLength(double s)
    {
        // Principal Frenet normal pointing towards helix central axis
        double t = s / _omega;
        return new Vector3D(-Math.Cos(t), -Math.Sin(t), 0.0);
    }

    public double ParameterAtArcLength(double s) => s / _omega;
}
```

### 4.6 Synthetic Surface: `PlaneSurface3D.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Geometry/PlaneSurface3D.cs`
```csharp
namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Represents a flat 3D plane reference surface.
/// </summary>
public sealed class PlaneSurface3D : ISurface3D
{
    private readonly Vector3D _normal;

    public PlaneSurface3D(Vector3D normal)
    {
        _normal = normal.Normalize();
    }

    public Vector3D NormalAtPoint(Point3D point) => _normal;
}
```

### 4.7 Updated Service Contract: `IGemEngine.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Services/IGemEngine.cs`
```csharp
namespace RadianceStudio.Core.Services;

using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Contract for calculating gem placements along curves and surface references.
/// </summary>
public interface IGemEngine
{
    /// <summary>
    /// Calculates the maximum number of stones that fit on a curve given usable length, gem diameter and spacing.
    /// </summary>
    int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm);

    /// <summary>
    /// Calculates arc-length distances for each stone center along a curve.
    /// </summary>
    IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm);

    /// <summary>
    /// Calculates full 3D placements and 4x4 affine transformation matrices along a space curve.
    /// </summary>
    IReadOnlyList<GemPlacement> CalculatePlacements(
        ICurve3D curve,
        ISurface3D? surface,
        GemCutType cutType,
        double gemSizeMm,
        double spacingMm,
        double startOffsetMm = 0,
        double endOffsetMm = 0,
        bool flipNormal = false,
        double rotationAngleDeg = 0);
}
```

### 4.8 Service Implementation: `GemEngine.cs`
File: `RadianceStudio/src/RadianceStudio.Core/Services/GemEngine.cs`
```csharp
namespace RadianceStudio.Core.Services;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Core implementation of the procedural gem placement engine.
/// Provides microsecond-scale closed-form affine matrix evaluation and curve distribution.
/// </summary>
public class GemEngine : IGemEngine
{
    public int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm)
    {
        if (gemSizeMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gemSizeMm), "Gem size must be positive.");
        }

        double step = gemSizeMm + spacingMm;
        if (step <= 1e-6)
        {
            throw new ArgumentException("Sum of gem size and spacing must be strictly positive.", nameof(spacingMm));
        }

        if (usableLengthMm < gemSizeMm - 1e-9)
        {
            return 0;
        }

        // Spec §3.1: N = floor((L_usable - d) / S_step) + 1
        // 1e-9 epsilon guards against floating-point under-representation of exact quotients
        double ratio = (usableLengthMm - gemSizeMm + 1e-9) / step;
        return (int)Math.Floor(ratio) + 1;
    }

    public IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm)
    {
        int count = CalculateStoneCount(usableLengthMm, gemSizeMm, spacingMm);
        if (count <= 0)
        {
            return Array.Empty<double>();
        }

        double step = gemSizeMm + spacingMm;
        double firstCenter = startOffsetMm + (gemSizeMm * 0.5);
        var distances = new double[count];

        for (int i = 0; i < count; i++)
        {
            distances[i] = firstCenter + (i * step);
        }

        return distances;
    }

    public IReadOnlyList<GemPlacement> CalculatePlacements(
        ICurve3D curve,
        ISurface3D? surface,
        GemCutType cutType,
        double gemSizeMm,
        double spacingMm,
        double startOffsetMm = 0,
        double endOffsetMm = 0,
        bool flipNormal = false,
        double rotationAngleDeg = 0)
    {
        ArgumentNullException.ThrowIfNull(curve);

        double usableLength = curve.Length - (startOffsetMm + endOffsetMm);
        int count = CalculateStoneCount(usableLength, gemSizeMm, spacingMm);
        if (count <= 0)
        {
            return Array.Empty<GemPlacement>();
        }

        // Hoist all invariants outside loop
        double step = gemSizeMm + spacingMm;
        double firstCenter = startOffsetMm + (gemSizeMm * 0.5);
        double aspectRatio = GemDimensions.GetDefault(cutType, gemSizeMm).AspectRatio;

        double sx = gemSizeMm;
        double sy = gemSizeMm * aspectRatio;
        double sz = gemSizeMm;

        double thetaRad = rotationAngleDeg * (Math.PI / 180.0);
        double cosTheta = Math.Cos(thetaRad);
        double sinTheta = Math.Sin(thetaRad);

        // Pre-allocate exact list capacity to prevent array resizes
        var placements = new List<GemPlacement>(count);

        for (int i = 0; i < count; i++)
        {
            double s = firstCenter + (i * step);
            Point3D pt = curve.PointAtArcLength(s);
            double paramT = curve.ParameterAtArcLength(s);
            Vector3D tangent = curve.TangentAtArcLength(s).Normalize();

            // Evaluate normal from surface or Frenet curve frame
            Vector3D normalCand = surface != null ? surface.NormalAtPoint(pt) : curve.NormalAtArcLength(s);
            if (flipNormal)
            {
                normalCand = -normalCand;
            }

            Vector3D normal = normalCand.Normalize();

            // Degenerate collinearity guard
            if (Math.Abs(Vector3D.Dot(tangent, normal)) > 0.999999)
            {
                Vector3D aux = Math.Abs(tangent.Z) < 0.999 ? new Vector3D(0, 0, 1) : new Vector3D(0, 1, 0);
                normal = (aux - (tangent * Vector3D.Dot(aux, tangent))).Normalize();
            }

            // Gram-Schmidt orthogonalization (Spec §3.2)
            Vector3D binormal = Vector3D.Cross(tangent, normal).Normalize();
            Vector3D tangentStar = Vector3D.Cross(normal, binormal);

            // Closed-form affine 4x4 matrix composition (Spec §3.3)
            double[] matrix = new double[16]
            {
                sx * ((cosTheta * binormal.X) + (sinTheta * tangentStar.X)),
                sy * ((-sinTheta * binormal.X) + (cosTheta * tangentStar.X)),
                sz * normal.X,
                pt.X,

                sx * ((cosTheta * binormal.Y) + (sinTheta * tangentStar.Y)),
                sy * ((-sinTheta * binormal.Y) + (cosTheta * tangentStar.Y)),
                sz * normal.Y,
                pt.Y,

                sx * ((cosTheta * binormal.Z) + (sinTheta * tangentStar.Z)),
                sy * ((-sinTheta * binormal.Z) + (cosTheta * tangentStar.Z)),
                sz * normal.Z,
                pt.Z,

                0.0,
                0.0,
                0.0,
                1.0
            };

            placements.Add(new GemPlacement
            {
                Index = i,
                CutType = cutType,
                GemSizeMm = gemSizeMm,
                ArcPositionMm = s,
                ParameterT = paramT,
                Matrix4x4 = matrix
            });
        }

        return placements;
    }
}
```

---

## 5. Verification Method

To independently verify all claims and specifications:

1. **Clean-Room Anti-Contamination Check:**
   ```powershell
   powershell -File tools/check_clean_room.ps1
   ```
   *Expected:* `[PASS] Clean-Room Integrity Verified. Zero proprietary contaminants found.`

2. **Automated Headless Test Suite:**
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release
   ```
   *Expected:* 100% tests green.

3. **Key Test Assertions to Implement in `GemEnginePlacementTests.cs`:**
   - **Stone Count Precision:**
     - `usableLength = 10.0, gemSize = 2.0, spacing = 0.5` $\to$ exactly 4 stones.
     - `usableLength = 9.5, gemSize = 2.0, spacing = 0.5` $\to$ exactly 4 stones.
     - `usableLength = 1.9, gemSize = 2.0, spacing = 0.5` $\to$ 0 stones.
     - `usableLength = 2.0, gemSize = 2.0, spacing = 0.5` $\to$ 1 stone.
   - **Inter-gem Spacing Tolerance:**
     - For all consecutive stones $i, i+1$: $|(s_{i+1} - s_i) - (\text{gemSize} + \text{spacing})| < 10^{-9}\text{ mm}$.
   - **Orthonormal Basis Determinant & Angles:**
     - Extract columns $\vec{B}, \vec{T}^*, \vec{N}$ from `Matrix4x4` divided by their respective scale factors ($s_x, s_y, s_z$).
     - $|\vec{B} \cdot \vec{T}^*| < 10^{-6}$.
     - $|\vec{B} \cdot \vec{N}| < 10^{-6}$.
     - $|\vec{T}^* \cdot \vec{N}| < 10^{-6}$.
     - $\det(R) \approx 1.0$ (within $|1.0 - \det| < 10^{-6}$).
   - **FlipNormal Invariance:**
     - Normal points in opposite direction ($\vec{N}_{flipped} = -\vec{N}$).
     - $\det(R)$ remains strictly $+1.0$ (right-handed).
   - **Azimuthal Rotation:**
     - At $90^\circ$, gem major axis aligns along $-\vec{B}$ and minor axis along $\vec{T}^*$.
   - **500-Stone Performance Benchmark:**
     - In `GemPlacementBenchmarkTests.cs`, execute 500 stones on `HelixCurve3D`.
     - Stopwatch elapsed time must be $< 10.0\text{ ms}$ (empirically $0.076\text{ ms}$).

4. **Invalidation Conditions:**
   - Any test failure in `dotnet test`.
   - Any proprietary string violation in `tools/check_clean_room.ps1`.
   - 500-stone placement calculation exceeding 10 ms.
