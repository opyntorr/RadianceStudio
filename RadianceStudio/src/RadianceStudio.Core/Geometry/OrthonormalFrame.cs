namespace RadianceStudio.Core.Geometry;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a right-handed orthonormal coordinate frame evaluated along a 3D rail curve.
/// Canonical mapping:
///   Binormal (X-axis): Transverse width across rail curve.
///   Tangent  (Y-axis): Direction of curve propagation (major gem axis).
///   Normal   (Z-axis): Surface normal / gem table direction (+Z).
/// Strictly satisfies det(R) = +1.0 and mutual dot products &lt; 10^-9.
/// </summary>
public readonly struct OrthonormalFrame : IEquatable<OrthonormalFrame>
{
    public Vector3D Tangent { get; }
    public Vector3D Normal { get; }
    public Vector3D Binormal { get; }

    public OrthonormalFrame(Vector3D tangent, Vector3D normal, Vector3D binormal)
    {
        Tangent = tangent;
        Normal = normal;
        Binormal = binormal;
    }

    /// <summary>
    /// Constructs a strictly orthonormal right-handed frame using Gram-Schmidt orthogonalization.
    /// Preserves candidateNormal direction while orthogonalizing tangent to surface tangent plane.
    /// Handles degeneracies (collinear vectors, straight rail C'' = 0) with deterministic fallbacks.
    /// </summary>
    public static OrthonormalFrame Create(Vector3D tangent, Vector3D candidateNormal, bool flipNormal = false)
    {
        // 1. Tangent validation
        double tLenSq = tangent.LengthSquared;
        if (tLenSq < 1e-24)
        {
            throw new ArgumentException("Tangent vector cannot be zero or degenerate.", nameof(tangent));
        }
        Vector3D T = tangent.Normalize();

        // 2. Candidate normal handling
        Vector3D N = candidateNormal;
        if (flipNormal)
        {
            N = -N;
        }

        // 3. Degeneracy check: zero-length or collinear with T
        double nLenSq = N.LengthSquared;
        bool isDegenerate = nLenSq < 1e-12;
        if (!isDegenerate)
        {
            N = N.Normalize();
            double dotTN = Math.Abs(T.Dot(N));
            if (dotTN > 1.0 - 1e-6) // Collinear (parallel or anti-parallel)
            {
                isDegenerate = true;
            }
        }

        if (isDegenerate)
        {
            // Deterministic orthogonal fallback: pick world Z, or world Y if tangent is vertical
            Vector3D aux = Math.Abs(T.Z) < 0.9 ? Vector3D.UnitZ : Vector3D.UnitY;
            Vector3D bFallback = T.Cross(aux).Normalize();
            N = bFallback.Cross(T).Normalize();

            if (flipNormal && candidateNormal.LengthSquared >= 1e-12)
            {
                N = -N;
            }
        }

        // 4. Gram-Schmidt Orthogonalization (spec §3.2)
        // Binormal: B = T x N
        Vector3D B = T.Cross(N).Normalize();

        // Recomputed orthogonal Tangent: T* = N x B
        Vector3D T_star = N.Cross(B).Normalize();

        // Final unit normalization to eliminate IEEE-754 numerical drift
        N = N.Normalize();

        return new OrthonormalFrame(T_star, N, B);
    }

    /// <summary>
    /// Computes the 3x3 determinant of the frame [B  T*  N].
    /// In an orthonormal right-handed basis, this is strictly +1.0.
    /// </summary>
    public double Determinant => Matrix4x4D.CreateFromFrame(this, Point3D.Origin).Determinant3x3();

    /// <summary>
    /// Validates whether the frame meets tolerance criteria.
    /// </summary>
    public bool IsOrthonormal(double tolerance = 1e-6)
    {
        return Math.Abs(Tangent.LengthSquared - 1.0) <= tolerance &&
               Math.Abs(Normal.LengthSquared - 1.0) <= tolerance &&
               Math.Abs(Binormal.LengthSquared - 1.0) <= tolerance &&
               Math.Abs(Tangent.Dot(Normal)) <= tolerance &&
               Math.Abs(Tangent.Dot(Binormal)) <= tolerance &&
               Math.Abs(Normal.Dot(Binormal)) <= tolerance &&
               Math.Abs(Determinant - 1.0) <= tolerance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4D ToTransformationMatrix(
        Point3D position,
        double gemSizeMm = 1.0,
        double aspectRatio = 1.0,
        double rotationAngleDeg = 0.0)
    {
        return Matrix4x4D.CreatePlacement(this, position, gemSizeMm, aspectRatio, rotationAngleDeg);
    }

    public bool Equals(OrthonormalFrame other) =>
        Tangent.Equals(other.Tangent) && Normal.Equals(other.Normal) && Binormal.Equals(other.Binormal);

    public override bool Equals(object? obj) => obj is OrthonormalFrame other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Tangent, Normal, Binormal);

    public static bool operator ==(OrthonormalFrame left, OrthonormalFrame right) => left.Equals(right);
    public static bool operator !=(OrthonormalFrame left, OrthonormalFrame right) => !left.Equals(right);
}
