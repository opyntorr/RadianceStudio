# Handoff Report: Emerald Cut Pavilion Winding Fix Strategy

**Agent:** `explorer_m1_fix_emerald_1`  
**Roles:** Explorer (Investigation & Synthesis)  
**Working Directory:** `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_fix_emerald_1`  
**Target File:** `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs`  
**Date:** 2026-10-16T23:58:30Z  
**Status:** Hard Handoff (Investigation & Fix Strategy Complete)

---

## 1. Observation

### 1.1 Verbatim Adversarial Test Failures
Running the test suite via:
`& "C:\Users\omarp\.dotnet\dotnet.exe" test C:\Users\omarp\matrix\RadianceStudio\RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~AdversarialStressTests"`

produced the following two empirical test failures on `GemCutType.Emerald`:

1. **Non-Manifold Edge Orientation**:
```
[FAIL] RadianceStudio.Core.Tests.Meshes.AdversarialStressTests.Mesh_IsStrictly2Manifold_AndSatisfiesEulerCharacteristic(cutType: Emerald)
Error Message:
 Cut Emerald: Edge (24, 25) has inconsistent orientation between face 17 (24->25) and face 25 (24->25)
Stack Trace:
   at RadianceStudio.Core.Tests.Meshes.AdversarialStressTests.Mesh_IsStrictly2Manifold_AndSatisfiesEulerCharacteristic(GemCutType cutType) in C:\Users\omarp\matrix\RadianceStudio\tests\RadianceStudio.Core.Tests\Meshes\AdversarialStressTests.cs:line 159
```

2. **Inverted Pavilion Normals**:
```
[FAIL] RadianceStudio.Core.Tests.Meshes.AdversarialStressTests.Mesh_NormalsPointStrictlyOutwardFromInterior(cutType: Emerald)
Error Message:
 Cut Emerald, Face 25: normal (-0.448, -0.448, 0.774) has non-positive outward dot -0.5051 with ray from interior center.
Stack Trace:
   at RadianceStudio.Core.Tests.Meshes.AdversarialStressTests.Mesh_NormalsPointStrictlyOutwardFromInterior(GemCutType cutType) in C:\Users\omarp\matrix\RadianceStudio\tests\RadianceStudio.Core.Tests\Meshes\AdversarialStressTests.cs:line 226
```

### 1.2 Source Code Inspection: `EmeraldMeshGenerator.cs` Lines 111–142
```csharp
111:         // 4. Crown Step 3 (8 trapezoids bordering girdle)
112:         for (int j = 0; j < 8; j++)
113:         {
114:             int next = (j + 1) % 8;
115:             faces.Add(new[] { 16 + j, 24 + j, 24 + next, 16 + next });
116:         }
117: 
118:         // 5. Pavilion Step 1 (8 trapezoids)
119:         for (int j = 0; j < 8; j++)
120:         {
121:             int next = (j + 1) % 8;
122:             faces.Add(new[] { 24 + j, 24 + next, 32 + next, 32 + j });
123:         }
124: 
125:         // 6. Pavilion Step 2 (8 trapezoids)
126:         for (int j = 0; j < 8; j++)
127:         {
128:             int next = (j + 1) % 8;
129:             faces.Add(new[] { 32 + j, 32 + next, 40 + next, 40 + j });
130:         }
131: 
132:         // 7. Pavilion Step 3 (8 trapezoids converging to culet)
133:         for (int j = 0; j < 8; j++)
134:         {
135:             int next = (j + 1) % 8;
136:             faces.Add(new[] { 40 + j, 40 + next, 48 + next, 48 + j });
137:         }
138: 
139:         // 8. Culet Face (1 face at zCulet, CCW viewed from exterior)
140:         faces.Add(new[] { 55, 54, 53, 52, 51, 50, 49, 48 });
```

---

## 2. Logic Chain

### 2.1 Edge Orientation Analysis Across Shared Boundaries
In an orientable closed 2-manifold surface, every undirected edge $(u, v)$ must be shared by exactly two faces, and must be traversed in **opposite directed orientations**: $(u \to v)$ by one face and $(v \to u)$ by the other.

1. **Girdle Boundary (Crown Step 3 vs Pavilion Step 1):**
   - Face 17 (Crown Step 3, $j=0$): defined as `new[] { 16, 24, 25, 17 }`.
     The edge along the girdle ring ($24..31$) is traversed from vertex $24$ to vertex $25$: $(24 \to 25)$.
   - Face 25 (Pavilion Step 1, $j=0$): currently defined as `new[] { 24, 25, 33, 32 }`.
     The edge along the girdle ring ($24..31$) is traversed from vertex $24$ to vertex $25$: $(24 \to 25)$.
   - **Conflict**: Both Face 17 and Face 25 traverse $(24 \to 25)$ in the **same direction**. This violates the 2-manifold orientation theorem.

2. **Culet Boundary (Pavilion Step 3 vs Culet Face):**
   - Face 49 (Culet Face): defined as `new[] { 55, 54, 53, 52, 51, 50, 49, 48 }`.
     The edge between vertices 48 and 49 is traversed from vertex $49$ to vertex $48$: $(49 \to 48)$.
   - Face 41 (Pavilion Step 3, $j=0$): currently defined as `new[] { 40, 41, 49, 48 }`.
     The edge between vertices 48 and 49 is traversed from vertex $49$ to vertex $48$: $(49 \to 48)$.
   - **Conflict**: Both Face 41 and Culet Face traverse $(49 \to 48)$ in the **same direction**.

3. **Facet Normal Inversion (Newell's Method):**
   - `GemMesh.ComputeFaceNormal` uses Newell's method, which assumes counter-clockwise (CCW) vertex ordering viewed from the exterior to generate outward unit normals.
   - For Face 25 currently defined as `{ 24, 25, 33, 32 }`, viewed from outside the gem, the vertex cycle is clockwise:
     $(24, \text{top-left}) \to (25, \text{top-right}) \to (33, \text{bottom-right}) \to (32, \text{bottom-left})$.
   - Newell's method computes an **inward normal**: $(-0.448, -0.448, +0.774)$.
     Because $N_z = +0.774 > 0$, the normal points upwards and into the gem interior instead of downward ($N_z < 0$) and outward.
   - The dot product with the ray from the interior centroid is $-0.5051 < 0$, causing backface culling and visual inversion in Rhino viewport rendering.

### 2.2 Proof of Corrected Pavilion Topology
Reversing the winding order of the trapezoids so they follow the same cyclic ordering pattern as Crown Steps:
`{ base + j, base + 8 + j, base + 8 + next, base + next }`

- **Pavilion Step 1**: `{ 24 + j, 32 + j, 32 + next, 24 + next }` (or cyclically `{ 24 + next, 24 + j, 32 + j, 32 + next }`)
  - Girdle ring edge: $(24 + next) \to (24 + j)$.
    Opposite to Crown Step 3's $(24 + j) \to (24 + next)$. **CONSISTENT**.
  - P1 ring edge: $(32 + j) \to (32 + next)$.

- **Pavilion Step 2**: `{ 32 + j, 40 + j, 40 + next, 32 + next }` (or cyclically `{ 32 + next, 32 + j, 40 + j, 40 + next }`)
  - P1 ring edge: $(32 + next) \to (32 + j)$.
    Opposite to Pavilion Step 1's $(32 + j) \to (32 + next)$. **CONSISTENT**.
  - P2 ring edge: $(40 + j) \to (40 + next)$.

- **Pavilion Step 3**: `{ 40 + j, 48 + j, 48 + next, 40 + next }` (or cyclically `{ 40 + next, 40 + j, 48 + j, 48 + next }`)
  - P2 ring edge: $(40 + next) \to (40 + j)$.
    Opposite to Pavilion Step 2's $(40 + j) \to (40 + next)$. **CONSISTENT**.
  - Culet ring edge: $(48 + j) \to (48 + next)$.
    Opposite to Culet Face's $(48 + next) \to (48 + j)$. **CONSISTENT**.

- **Internal Slant Edges (Between adjacent sectors $j$ and $j-1$):**
  - Sector $j$ slant edge: $(base + j) \to (base + 8 + j)$.
  - Sector $j-1$ (where $next = j$) slant edge: $(base + 8 + j) \to (base + j)$.
  - Opposite directed across all sectors. **CONSISTENT**.

- **Outward Normal Proof**:
  - Vertex ordering: $(top\text{-}left) \to (bottom\text{-}left) \to (bottom\text{-}right) \to (top\text{-}right)$.
  - Viewed from exterior: counter-clockwise.
  - Newell's normal calculation yields:
    $N_x > 0$ for positive $X$ sectors, $N_y > 0$ for positive $Y$ sectors, and $N_z < 0$ (pointing downward along $-Z$).
  - Outward ray dot product $\vec{N} \cdot \vec{R}_{outward} > 0.05$.

---

## 3. Caveats

- **No Caveats.**
  - The analysis is exact and mathematically complete.
  - No changes are needed to the vertex coordinate generation or culet ring indexing.
  - This fix exclusively touches the 3 pavilion loops in `EmeraldMeshGenerator.cs` (lines 122, 129, 136).
  - Note: Other known defects (e.g., pipeline double-scaling across elongated cuts) are addressed by separate work packages and do not interfere with this topology fix.

---

## 4. Conclusion

### 4.1 Recommended Fix Specification
In `RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs`:

Replace lines 118–138:

#### Before:
```csharp
        // 5. Pavilion Step 1 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 24 + j, 24 + next, 32 + next, 32 + j });
        }

        // 6. Pavilion Step 2 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 32 + j, 32 + next, 40 + next, 40 + j });
        }

        // 7. Pavilion Step 3 (8 trapezoids converging to culet)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 40 + j, 40 + next, 48 + next, 48 + j });
        }
```

#### After:
```csharp
        // 5. Pavilion Step 1 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 24 + j, 32 + j, 32 + next, 24 + next });
        }

        // 6. Pavilion Step 2 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 32 + j, 40 + j, 40 + next, 32 + next });
        }

        // 7. Pavilion Step 3 (8 trapezoids converging to culet)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 40 + j, 48 + j, 48 + next, 40 + next });
        }
```

*(Note: Cyclically equivalent variant `new[] { 24 + next, 24 + j, 32 + j, 32 + next }` produces the exact same face; the proposed variant is preferred because it matches the `r1 + j, r2 + j, r2 + next, r1 + next` pattern used in Crown Steps 1–3).*

### 4.2 Unified Unified Diff Patch
```patch
--- a/RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs
+++ b/RadianceStudio/src/RadianceStudio.Core/Services/MeshGenerators/EmeraldMeshGenerator.cs
@@ -122,19 +122,19 @@ protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces)
         for (int j = 0; j < 8; j++)
         {
             int next = (j + 1) % 8;
-            faces.Add(new[] { 24 + j, 24 + next, 32 + next, 32 + j });
+            faces.Add(new[] { 24 + j, 32 + j, 32 + next, 24 + next });
         }
 
         // 6. Pavilion Step 2 (8 trapezoids)
         for (int j = 0; j < 8; j++)
         {
             int next = (j + 1) % 8;
-            faces.Add(new[] { 32 + j, 32 + next, 40 + next, 40 + j });
+            faces.Add(new[] { 32 + j, 40 + j, 40 + next, 32 + next });
         }
 
         // 7. Pavilion Step 3 (8 trapezoids converging to culet)
         for (int j = 0; j < 8; j++)
         {
             int next = (j + 1) % 8;
-            faces.Add(new[] { 40 + j, 40 + next, 48 + next, 48 + j });
+            faces.Add(new[] { 40 + j, 48 + j, 48 + next, 40 + next });
         }
```

---

## 5. Verification Method

To verify this fix once applied by an implementing agent:

1. **Verify Emerald 2-Manifold Edge Orientation:**
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~Mesh_IsStrictly2Manifold_AndSatisfiesEulerCharacteristic"
   ```
   *Expected Result:* All 6 gem cuts PASS, including Emerald with 0 orientation inconsistencies.

2. **Verify Emerald Outward Normals:**
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~Mesh_NormalsPointStrictlyOutwardFromInterior"
   ```
   *Expected Result:* All 6 gem cuts PASS, with all 24 pavilion face normals having $N_z < 0$ and dot product $> 0.05$.

3. **Verify Facet Planarity and Bounds:**
   ```powershell
   & "C:\Users\omarp\.dotnet\dotnet.exe" test RadianceStudio/RadianceStudio.sln --configuration Release --filter "FullyQualifiedName~CanonicalGemMeshTests"
   ```
   *Expected Result:* 100% PASS across all 50 facets.

4. **Verify Clean-Room Gatekeeper:**
   ```powershell
   powershell -File tools/check_clean_room.ps1
   ```
   *Expected Result:* `[PASS] Clean-Room Integrity Verified. Zero proprietary contaminants found.`
