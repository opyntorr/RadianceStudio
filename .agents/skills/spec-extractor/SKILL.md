---
name: spec-extractor
description: Systematic mathematical and functional specification extractor from binary evidence into specs/*.spec.md.
---

# Spec Extractor Skill

Guidelines for converting reverse-engineered evidence into clean mathematical specifications:

1. **Pure Geometry & Vector Algebra:**
   - Define exact dimensions, ratios, angles, and boundary conditions.
   - Express positioning and rotations as $4 \times 4$ affine transformation matrices and Frenet-Serret orthonormal bases.

2. **Parameter Contracts:**
   - Define inputs: valid ranges, default values, units (mm, degrees).
   - Define outputs: geometry type (Brep, Mesh, Matrix list).

3. **Zero Contamination:**
   - Never copy method or variable names from proprietary binaries.
   - Use standard mathematical and CAD terminology.
