# Dispatch Directive: M1 Math & Frame Explorer

## Working Directory
`C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_math_1`

## Mission
Analyze and specify exact implementation designs for pure .NET 8 3D vector, point, affine matrix, and orthonormal frame mathematics in `RadianceStudio.Core.Geometry`.

## Inputs to Read
- `C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- `C:\Users\omarp\matrix\PROJECT.md`
- `C:\Users\omarp\matrix\specs\gem_engine.spec.md`

## Scope & Analysis Tasks
1. Design `Vector3D` (struct with Dot, Cross, Length, Normalize, operators) and `Point3D`.
2. Design `Matrix4x4D` (row-major 16-element array, Transform point/vector, 3x3 rotational determinant, translation, scaling, multiply).
3. Design `OrthonormalFrame` with Gram-Schmidt orthogonalization:
   - Tangent $\vec{T}$, candidate normal $\vec{N}$ (or Frenet), $\vec{B} = \vec{T} \times \vec{N}$, $\vec{T}^* = \vec{N} \times \vec{B}$.
   - Ensure dot products $< 10^{-6}$ (achieving $< 10^{-12}$) and $\det(R) \approx 1.0$.
   - Handle `FlipNormal` ($\vec{N} \to -\vec{N}$, preserving right-handed frame).
   - Degenerate fallback (straight line $C'' = 0$ or parallel tangent/normal).
4. Provide concrete C# code architecture proposals and test case designs for `RadianceStudio.Core.Tests`.

## Strict Clean-Room Constraint
NEVER access, grep, search, or mention `_dirty_room/` or any decompiled proprietary code. STRICT CHINESE WALL.

## Output
Write report to `C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_math_1\handoff.md` and send message to caller.


## 2026-10-16T23:29:18Z
From: 4d69e4bc-084b-47a0-ad74-5668ab8750e2
Priority: HIGH
Content: Read C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_math_1\DISPATCH.md and C:\Users\omarp\matrix\.agents\teamwork\ORIGINAL_REQUEST.md. Your working directory is C:\Users\omarp\matrix\.agents\teamwork\explorer_m1_math_1. Design Vector3D, Point3D, Matrix4x4D, and OrthonormalFrame (Gram-Schmidt, det=1.0). NEVER access _dirty_room/. Write your findings to handoff.md and send message when done.
