namespace RadianceStudio.Core.Geometry;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a high-performance 3D vector with double-precision floating-point components.
/// Implemented as a readonly record struct for zero-allocation value semantics.
/// </summary>
public readonly record struct Vector3D(double X, double Y, double Z)
{
    public static readonly Vector3D Zero = new(0, 0, 0);
    public static readonly Vector3D UnitX = new(1, 0, 0);
    public static readonly Vector3D UnitY = new(0, 1, 0);
    public static readonly Vector3D UnitZ = new(0, 0, 1);

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared => X * X + Y * Y + Z * Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Vector3D a, Vector3D b) => a.Dot(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D Cross(Vector3D other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D Cross(Vector3D a, Vector3D b) => a.Cross(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D Normalize()
    {
        double len = Length;
        if (len < 1e-15)
        {
            return Zero;
        }
        double inv = 1.0 / len;
        return new Vector3D(X * inv, Y * inv, Z * inv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNormalized(double tolerance = 1e-6) => Math.Abs(LengthSquared - 1.0) <= tolerance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsZero(double tolerance = 1e-12) => LengthSquared <= tolerance * tolerance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3D ToPoint3D() => new(X, Y, Z);

    // Operator Overloads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator -(Vector3D a) => new(-a.X, -a.Y, -a.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator *(Vector3D v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator *(double scalar, Vector3D v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator /(Vector3D v, double scalar)
    {
        double inv = 1.0 / scalar;
        return new Vector3D(v.X * inv, v.Y * inv, v.Z * inv);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Point3D(Vector3D v) => new(v.X, v.Y, v.Z);

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
}
