# Investigation & Fix Strategy Report: Non-Square Cut Double-Scaling Reconciliation

**Agent**: `explorer_m1_fix_scale_1`  
**Date**: 2026-10-17T00:05:00Z  
**Role**: Explorer, Specialist, Synthesis  
**Working Directory**: `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_scale_1`  
**Status**: Complete (Hard Handoff)  
**Clean-Room Integrity**: STRICT COMPLIANCE (0 violations, zero access to `_dirty_room/`)  

---

## 1. Observation

### 1.1 Verbatim Adversarial Test Failures
Directly observed via `& "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release`:

```
[FAIL] EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(cutType: Oval, expectedAspectRatio: 1.45)
Error Message: Double-scaling defect detected! Cut Oval canonical aspect ratio is 1.45, but transformed mesh has aspect ratio 2.1025 (deviating by 0.6525, matching 1.45^2 = 2.1025)
Stack Trace: at RadianceStudio.Core.Tests.Meshes.AdversarialStressTests.EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(GemCutType cutType, Double expectedAspectRatio) in C:\Users\omarp\matrix\RadianceStudio\tests\RadianceStudio.Core.Tests\Meshes\AdversarialStressTests.cs:line 294

[FAIL] EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(cutType: Pear, expectedAspectRatio: 1.55)
Error Message: Double-scaling defect detected! Cut Pear canonical aspect ratio is 1.55, but transformed mesh has aspect ratio 2.4025 (deviating by 0.8525, matching 1.55^2 = 2.4025)

[FAIL] EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(cutType: Emerald, expectedAspectRatio: 1.40)
Error Message: Double-scaling defect detected! Cut Emerald canonical aspect ratio is 1.40, but transformed mesh has aspect ratio 1.9600 (deviating by 0.5600, matching 1.4^2 = 1.9600)

[FAIL] EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(cutType: Marquise, expectedAspectRatio: 1.95)
Error Message: Double-scaling defect detected! Cut Marquise canonical aspect ratio is 1.95, but transformed mesh has aspect ratio 3.8025 (deviating by 1.8525, matching 1.95^2 = 3.8025)
```

Overall execution: `Failed! - Failed: 6, Passed: 154, Skipped: 0, Total: 160`. The 4 failures above represent the entire double-scaling defect class.

---

### 1.2 Codebase Locations of Double-Scaling Mechanics

1. **Mesh Generators (Baking Aspect Ratio into Canonical Vertices)**:
   - `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/OvalMeshGenerator.cs` (lines 19–20):
     ```csharp
     double rx0 = diameterMm * 0.5;
     double ry0 = rx0 * dim.AspectRatio; // ry0 = 0.5 * 1.45 = 0.725 mm -> Length = 1.45 mm
     ```
   - `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/PearMeshGenerator.cs` (lines 20–21):
     ```csharp
     double a = diameterMm * 0.5;
     double b = a * dim.AspectRatio; // b = 0.5 * 1.55 = 0.775 mm -> Length = 1.55 mm
     ```
   - `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs` (lines 20–21):
     ```csharp
     double a = diameterMm * 0.5;
     double b = a * dim.AspectRatio; // b = 0.5 * 1.40 = 0.700 mm -> Length = 1.40 mm
     ```
   - `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/MarquiseMeshGenerator.cs` (lines 19–20):
     ```csharp
     double a = diameterMm * 0.5;
     double b = a * dim.AspectRatio; // b = 0.5 * 1.95 = 0.975 mm -> Length = 1.95 mm
     ```

2. **GemEngine Placement Calculation (Scaling Tangent Axis by AspectRatio)**:
   - `RadianceStudio/src/RadianceStudio.Core/Services/GemEngine.cs` (lines 81 & 100):
     ```csharp
     81:  double aspectRatio = GemDimensions.GetDefault(cutType, gemSizeMm).AspectRatio;
     ...
     100: var matrix = Matrix4x4D.CreatePlacement(frame, pt, gemSizeMm, aspectRatio, rotationAngleDeg);
     ```

3. **Matrix4x4D Scale Composition**:
   - `RadianceStudio/src/RadianceStudio.Core/Geometry/Matrix4x4D.cs` (lines 106–108):
     ```csharp
     double sx = gemSizeMm;
     double sy = gemSizeMm * aspectRatio;
     double sz = gemSizeMm;
     ```

4. **Adversarial Stress Harness Oracle**:
   - `RadianceStudio/tests/RadianceStudio.Core.Tests/Meshes/AdversarialStressTests.cs` (lines 281 & 284):
     ```csharp
     double ar = GemDimensions.GetDefault(cutType).AspectRatio;
     var matrix = Matrix4x4D.CreatePlacement(frame, Point3D.Origin, gemSizeMm, ar);
     var transformedMesh = mesh.Transform(matrix);
     ```

5. **Existing Placement Suite Assertion on Oval Determinant**:
   - `RadianceStudio/tests/RadianceStudio.Core.Tests/Placement/GemEnginePlacementTests.cs` (lines 164–165):
     ```csharp
     double detStd = mStd.Determinant3x3() / (2.0 * 2.0 * 1.45 * 2.0);
     double detFlip = mFlip.Determinant3x3() / (2.0 * 2.0 * 1.45 * 2.0);
     ```

---

### 1.3 Specification and Contract Requirements

1. **`ORIGINAL_REQUEST.md` (R1 & Acceptance Criteria)**:
   - Line 12: *"Generate normalized canonical 1.0 mm gem meshes centered at the origin $(0,0,0)$ with table pointing towards $+Z$ and culet at $-Z$ for: Round Brilliant... Princess... Oval (aspect ratio ~1.45)... Pear / Teardrop (aspect ratio ~1.55)... Emerald (step cut, aspect ratio ~1.40)... Marquise (aspect ratio ~1.95)."*
   - Line 42: *"Every canonical gem cut model matches the exact ratios in `specs/gem_proportions.json`."*
   - Line 43: *"Total depth, crown height, and pavilion depth scale strictly linearly with diameter."*
   - Line 44: *"Non-square cuts scale major and minor axes according to their canonical aspect ratio."*

2. **`specs/gem_proportions.json`**:
   - Line 4: `"Canonical gem cut dimensional ratios normalized to 1.0 mm girdle diameter/width. Used for procedural canonical mesh generation and matrix scaling."`
   - Aspect ratios specified: `RoundBrilliant`: 1.0, `Princess`: 1.0, `Oval`: 1.45, `Pear`: 1.55, `Emerald`: 1.40, `Marquise`: 1.95.

3. **`specs/gem_engine.spec.md`**:
   - §1 (Canonical Gem Space): *"Escala normalizada: El diámetro exterior del filetín (o ancho en gemas rectangulares) es exactamente $d_0 = 1.0\text{ mm}$."*
   - §3.3 (Affine 4x4 Matrix Composition):
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
   - §4 (Preview Pipeline): *"DisplayConduit.DrawMesh o DisplayConduit.DrawShadedPolyline renderiza la malla canónica multiplicada por cada matriz $M_i$."*

4. **`CanonicalGemMeshTests.cs` (lines 30–57 & 66–85)**:
   - Asserts canonical meshes have exact bounds: Oval length = 1.45, Pear length = 1.55, Emerald length = 1.40, Marquise length = 1.95.
   - Asserts `GemMeshFactory.Create(cutType, diameterMm)` scales canonical bounds linearly by `diameterMm`.

---

## 2. Logic Chain

### 2.1 Mathematical Decomposition of the Contradiction
Let:
- $W_0$ and $L_0$ be the width and length of the canonical mesh $M_{\text{canonical}}$ generated by `GemMeshFactory.CreateCanonical(cutType)`.
- $S_x$ and $S_y$ be the scaling factors along the transverse binormal axis $\vec{B}$ and the curve tangent axis $\vec{T}^*$ in the placement matrix $M_i$.
- $W'$ and $L'$ be the final width and length of the instance transformed by $M_i$: $W' = W_0 \cdot S_x$ and $L' = L_0 \cdot S_y$.

From Observation 1.2:
- The mesh generators produce: $W_0 = 1.0\text{ mm}$ and $L_0 = 1.0 \cdot \text{AR}$ (Observation 1.2.1).
- `CreatePlacement` applies: $S_x = d$ and $S_y = d \cdot \text{AR}$ (Observation 1.2.3).
- The transformed gem dimensions are:
  $$W' = 1.0 \cdot d = d$$
  $$L' = (1.0 \cdot \text{AR}) \cdot (d \cdot \text{AR}) = d \cdot \text{AR}^2$$
  $$\text{Effective Aspect Ratio} = \frac{L'}{W'} = \frac{d \cdot \text{AR}^2}{d} = \text{AR}^2$$
For Oval ($1.45^2 = 2.1025$), Pear ($1.55^2 = 2.4025$), Emerald ($1.40^2 = 1.9600$), Marquise ($1.95^2 = 3.8025$). This causes $+45\%$ to $+95\%$ excess elongation along the curve, severely distorting the stones and causing massive physical collisions between adjacent gems.

---

### 2.2 Proof: Why Normalizing Canonical Meshes to Unit Aspect Ratio ($L_0 = 1.0$) is Invalid

If we were to normalize canonical meshes to unit aspect ratio ($W_0 = 1.0, L_0 = 1.0$) and rely on $M_i$ to stretch them by $S_y = d \cdot \text{AR}$:

1. **Destruction of Marquise Navette Cusps**:
   - In `MarquiseMeshGenerator.cs`, the navette shape is constructed from two intersecting circular arcs of radius $R_{\text{arc}} = \frac{a^2 + b^2}{2a}$ meeting at sharp pointed cusps at $y = \pm b$.
   - The interior cusp angle is:
     $$\theta_{\text{cusp}} = 2 \arctan\left(\frac{b^2 - a^2}{2ab}\right) = 2 \arctan\left(\frac{\text{AR}^2 - 1}{2\text{AR}}\right)$$
     For Marquise ($\text{AR} = 1.95$), $\theta_{\text{cusp}} \approx 71.4^\circ \ne 0$.
   - If normalized to unit aspect ratio ($a = b = 0.5$): $R_{\text{arc}} = 0.5$, $x_{\text{center}} = 0$, $\theta_{\text{cusp}} = 0^\circ$ (a smooth circle).
   - An affine non-uniform scaling $S(1, \text{AR}, 1)$ maps a circle to an ellipse. An ellipse is $C^1$ smooth everywhere with zero cusp angle.
   - **Result**: Affine scaling cannot produce the sharp, pointed French tips of a Marquise diamond. It produces an oval ellipse with navette facets stretched over it, violating gemological geometry.

2. **Distortion of Emerald Step Cut 45° Chamfers**:
   - In `EmeraldMeshGenerator.cs`, the 4 corner chamfers are cut at an authentic $45^\circ$ bevel: $\Delta x = \Delta y = \text{chamfer}$.
   - If generated on a square template ($a = b$) and stretched by $\text{AR} = 1.40$ along Y, the corner bevel vector $(\Delta x, \Delta y)$ transforms to $(\Delta x, 1.40 \Delta x)$, changing the corner bevel angle from $45^\circ$ to $\arctan(1.40) \approx 54.46^\circ$.
   - Step cut facets lose their parallel edges and correct dihedral slopes.

3. **Breakage of the Standalone `IGemMeshGenerator` Contract**:
   - `IGemMeshGenerator.GenerateMesh(cutType, diameterMm)` is a primary public API.
   - Calling `GemMeshFactory.Create(GemCutType.Marquise, 4.0)` must return a Marquise mesh of width $4.0\text{ mm}$ and length $7.8\text{ mm}$ ($4.0 \times 1.95$), as validated by `CanonicalGemMeshTests.ScaledMesh_ScalesLinearlyWithDiameter` (Observation 1.3.4).
   - If canonical meshes were unit-square/circle, `GemMeshFactory.Create` would return circular gems of length $4.0\text{ mm}$ and width $4.0\text{ mm}$, breaking standalone mesh generation.

4. **Breakage of Existing Test Suites**:
   - `CanonicalGemMeshTests.CanonicalMesh_MatchesProportionsAndBounds` (Observation 1.3.4) asserts exact bounds: Oval length = 1.45, Pear length = 1.55, Emerald length = 1.40, Marquise length = 1.95.

---

### 2.3 Proof: Why Uniform Scaling in `GemEngine.CalculatePlacements` is the Unique Correct Solution

1. **Geometric Integrity**:
   - Canonical meshes generated by `GemMeshFactory.CreateCanonical(cutType)` are 1.0 mm in girdle width ($W_0 = 1.0$), with length $L_0 = 1.0 \cdot \text{AR}_{\text{canonical}}$ and depth $D_0 = 1.0 \cdot (\text{TotalDepthPercent} / 100)$, matching `specs/gem_proportions.json` and `ORIGINAL_REQUEST.md` R1.
   - In `GemEngine.CalculatePlacements`, setting the instance scaling in $M_i$ to uniform scale by `gemSizeMm` ($S_x = d, S_y = d, S_z = d$, i.e. `aspectRatio: 1.0` in `Matrix4x4D.CreatePlacement`):
     $$W' = 1.0 \cdot d = d$$
     $$L' = (1.0 \cdot \text{AR}) \cdot d = d \cdot \text{AR}$$
     $$D' = (1.0 \cdot D) \cdot d = d \cdot D$$
     $$\text{Effective Aspect Ratio} = \frac{L'}{W'} = \frac{d \cdot \text{AR}}{d} = \text{AR}$$
   - Both major and minor axes scale strictly linearly with diameter. The transformed instance aspect ratio exactly equals the canonical aspect ratio $\text{AR}$ without double-scaling.
   - Facet angles, 45° emerald chamfers, and pointed Marquise tips are preserved without anisotropic distortion.

2. **Reconciliation with `specs/gem_engine.spec.md` §3.3**:
   - In §3.3, the scaling matrix is written as $S(d, d \cdot \text{AspectRatio}, d)$.
   - In architectural terms, the parameter `aspectRatio` in $M_i$ represents the *relative aspect ratio factor* ($\text{AR}_{\text{rel}} = \frac{\text{AR}_{\text{target}}}{\text{AR}_{\text{canonical}}}$) applied to the canonical mesh.
   - For standard placement of canonical gem cuts, $\text{AR}_{\text{target}} = \text{AR}_{\text{canonical}}$, so $\text{AR}_{\text{rel}} \equiv 1.0$.
   - If a caller desires a parametric variation (e.g. stretching an Oval from 1.45 to 1.60), the matrix scales the tangent axis by $\text{AR}_{\text{rel}} = 1.60 / 1.45$.
   - This reconciles §3.3, §1, and §4 into a unified, mathematically consistent framework.

3. **Inter-Gem Spacing & Collision Resolution**:
   - Arc length pitch is $S_{\text{step}} = \text{GemSize} + \text{Spacing}$.
   - Eliminating the double-scaling prevents the catastrophic $4.2\text{ mm}$ (Oval) and $7.6\text{ mm}$ (Marquise) stone blowouts, restoring geometric clearance along the curve rail.

---

## 3. Caveats

1. **Curve Arc-Length Pitch with Elongated Cuts**:
   - `specs/gem_engine.spec.md` §3.1 defines $S_{\text{step}} = \text{GemSize} + \text{Spacing}$ based on `GemSize` (diameter/width $d$).
   - Because canonical non-square gems have length $L = d \cdot \text{AR} > d$ along the curve tangent $\vec{T}^*$, gems placed head-to-tail will touch when $\text{Spacing} = d(\text{AR} - 1)$.
   - While eliminating the double-scaling defect reduces the gem length from $d \cdot \text{AR}^2$ back to its proper $d \cdot \text{AR}$, users laying out elongated gems along curves should be aware that positive clearance between tips along the rail requires $\text{Spacing} > d(\text{AR} - 1)$, or stones rotated by $90^\circ$ around the normal ($\theta = 90^\circ$, so width $d$ aligns along tangent).
   - This is purely an end-user parametric consideration; the mathematical placement engine strictly adheres to §3.1.
2. **Matrix4x4D.CreatePlacement API Compatibility**:
   - `Matrix4x4D.CreatePlacement(frame, pos, gemSizeMm, aspectRatio = 1.0, rotationAngleDeg = 0.0)` does NOT need to change its signature or implementation. Its fourth argument already defaults to `1.0`. Standalone math tests calling `CreatePlacement` directly with explicit aspect ratios (e.g. in `Matrix4x4DTests.cs` and `AdversarialPlacementTests.cs`) remain 100% valid.

---

## 4. Conclusion & Actionable Fix Strategy

### 4.1 Summary of Changes
1. **`RadianceStudio/src/RadianceStudio.Core/Services/GemEngine.cs`**:
   - In `CalculatePlacements`, invoke `Matrix4x4D.CreatePlacement` with `aspectRatio: 1.0` (or rely on the default argument), ensuring the placement matrix performs uniform scaling $S(d, d, d)$ on canonical meshes.
2. **`RadianceStudio/tests/RadianceStudio.Core.Tests/Meshes/AdversarialStressTests.cs`**:
   - In `EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio`, evaluate the matrix via the end-to-end `GemEngine.CalculatePlacements` pipeline (or with `aspectRatio: 1.0`), verifying that canonical meshes transformed by placement matrices preserve their canonical aspect ratio.
3. **`RadianceStudio/tests/RadianceStudio.Core.Tests/Placement/GemEnginePlacementTests.cs`**:
   - In `CalculatePlacements_WithFlipNormal_InvertsNormalAndPreservesDet`, update the 3x3 determinant divisor from `(2.0 * 2.0 * 1.45 * 2.0)` to `(2.0 * 2.0 * 2.0) = 8.0`, reflecting uniform scaling of the placement frame.

---

### 4.2 Concrete Code Snippets & Diff Proposals

#### Change 1: `RadianceStudio/src/RadianceStudio.Core/Services/GemEngine.cs`
Target file: `C:\Users\omarp\matrix\RadianceStudio\src\RadianceStudio.Core\Services\GemEngine.cs`  
Lines: 79–102  

```diff
<<<<
         double step = gemSizeMm + spacingMm;
         double firstCenter = startOffsetMm + (gemSizeMm * 0.5);
-        double aspectRatio = GemDimensions.GetDefault(cutType, gemSizeMm).AspectRatio;

         // Pre-allocate exact list capacity to prevent array resizes
         var placements = new List<GemPlacement>(count);

         for (int i = 0; i < count; i++)
         {
             double s = firstCenter + (i * step);
             Point3D pt = curve.PointAtArcLength(s);
             double paramT = curve.ParameterAtArcLength(s);
             Vector3D tangent = curve.TangentAtArcLength(s);

             // Evaluate normal from surface or Frenet curve frame
             Vector3D normalCand = surface != null ? surface.NormalAtPoint(pt) : curve.NormalAtArcLength(s);

             // Construct strictly orthonormal right-handed frame (handles flipNormal & degeneracies)
             var frame = OrthonormalFrame.Create(tangent, normalCand, flipNormal);

             // Direct closed-form affine 4x4 matrix composition (Spec §3.3)
-            var matrix = Matrix4x4D.CreatePlacement(frame, pt, gemSizeMm, aspectRatio, rotationAngleDeg);
+            // Canonical gem meshes already embody authentic cut aspect ratios (width = 1.0, length = AspectRatio).
+            // Placement transforms scale uniformly by gemSizeMm (aspectRatio multiplier = 1.0).
+            var matrix = Matrix4x4D.CreatePlacement(frame, pt, gemSizeMm, aspectRatio: 1.0, rotationAngleDeg);
====
```

---

#### Change 2: `RadianceStudio/tests/RadianceStudio.Core.Tests/Meshes/AdversarialStressTests.cs`
Target file: `C:\Users\omarp\matrix\RadianceStudio\tests\RadianceStudio.Core.Tests\Meshes\AdversarialStressTests.cs`  
Lines: 275–296  

```diff
<<<<
     [Theory]
     [InlineData(GemCutType.Oval, 1.45)]
     [InlineData(GemCutType.Pear, 1.55)]
     [InlineData(GemCutType.Emerald, 1.40)]
     [InlineData(GemCutType.Marquise, 1.95)]
     public void EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(
         GemCutType cutType, double expectedAspectRatio)
     {
         var mesh = GemMeshFactory.CreateCanonical(cutType);
-        var frame = OrthonormalFrame.Create(Vector3D.UnitY, Vector3D.UnitZ);
+        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(10, 0, 0));
+        var engine = new GemEngine();
         double gemSizeMm = 1.0;
-        double ar = GemDimensions.GetDefault(cutType).AspectRatio;
 
-        // Matrix composed per GemEngine placement logic
-        var matrix = Matrix4x4D.CreatePlacement(frame, Point3D.Origin, gemSizeMm, ar);
+        // Matrix obtained directly from GemEngine end-to-end placement calculation
+        var placement = engine.CalculatePlacements(line, null, cutType, gemSizeMm, spacingMm: 0.5)[0];
+        var matrix = new Matrix4x4D(placement.Matrix4x4);
         var transformedMesh = mesh.Transform(matrix);
 
         var (min, max) = transformedMesh.GetBounds();
         double width = max.X - min.X;
         double length = max.Y - min.Y;
         double actualAspectRatio = length / width;
 
         // The transformed 1.0mm gem must have length/width ratio equal to expectedAspectRatio (e.g. 1.45).
         // If double-scaling occurs, actualAspectRatio will be expectedAspectRatio^2 (e.g. 2.1025 for Oval, 3.8025 for Marquise).
         Assert.True(Math.Abs(actualAspectRatio - expectedAspectRatio) < 0.05,
             $"Double-scaling defect detected! Cut {cutType} canonical aspect ratio is {expectedAspectRatio:F2}, but transformed mesh has aspect ratio {actualAspectRatio:F4} (deviating by {Math.Abs(actualAspectRatio - expectedAspectRatio):F4}, matching {expectedAspectRatio}^2 = {expectedAspectRatio * expectedAspectRatio:F4})");
     }
====
```

---

#### Change 3: `RadianceStudio/tests/RadianceStudio.Core.Tests/Placement/GemEnginePlacementTests.cs`
Target file: `C:\Users\omarp\matrix\RadianceStudio\tests\RadianceStudio.Core.Tests\Placement\GemEnginePlacementTests.cs`  
Lines: 163–169  

```diff
<<<<
-            // Both must be right-handed
-            double detStd = mStd.Determinant3x3() / (2.0 * 2.0 * 1.45 * 2.0);
-            double detFlip = mFlip.Determinant3x3() / (2.0 * 2.0 * 1.45 * 2.0);
+            // Both must be right-handed with uniform scaling (gemSize = 2.0 on all 3 axes)
+            double detStd = mStd.Determinant3x3() / (2.0 * 2.0 * 2.0);
+            double detFlip = mFlip.Determinant3x3() / (2.0 * 2.0 * 2.0);
 
             Assert.Equal(1.0, detStd, 5);
             Assert.Equal(1.0, detFlip, 5);
====
```

---

## 5. Verification Method

To independently verify this strategy after application:

1. **Clean-Room Anti-Contamination Verification**:
   ```powershell
   powershell -File tools/check_clean_room.ps1
   ```
   *Expected Result*: Exit Code 0, `[PASS] Clean-Room Integrity Verified. Zero proprietary contaminants found.`

2. **Targeted Pipeline Aspect Ratio Verification**:
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~EndToEndPipeline" --verbosity normal
   ```
   *Expected Result*: All 4 test cases (`Oval`, `Pear`, `Emerald`, `Marquise`) pass with 0 failures:
   - `Oval`: Aspect ratio = 1.45 (diff = 0.0000)
   - `Pear`: Aspect ratio = 1.55 (diff = 0.0000)
   - `Emerald`: Aspect ratio = 1.40 (diff = 0.0000)
   - `Marquise`: Aspect ratio = 1.95 (diff = 0.0000)

3. **Targeted Placement Matrix Invariants Verification**:
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~CalculatePlacements_WithFlipNormal" --verbosity normal
   ```
   *Expected Result*: Passes with normalized determinant ratio = 1.00000.

4. **Mesh Factory Regression Check**:
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~CanonicalGemMeshTests" --verbosity normal
   ```
   *Expected Result*: All tests pass (verifying standalone meshes continue to maintain canonical proportions and linear scaling).

5. **Placement Benchmark Check**:
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~Benchmark" --verbosity normal
   ```
   *Expected Result*: 500-stone placement calculation executes in $< 1.0\text{ ms}$ (budget $< 10\text{ ms}$).

6. **Invalidation Conditions**:
   - Any test failure in `CanonicalGemMeshTests` (would indicate canonical mesh corruption).
   - Any failure in `EndToEndPipeline` (would indicate persistent double-scaling).
   - Non-zero violations in `tools/check_clean_room.ps1`.
