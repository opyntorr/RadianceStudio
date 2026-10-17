namespace RadianceStudio.Core.Geometry;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a high-performance 3D point in Euclidean space.
/// Implemented as a readonly record struct for zero-allocation value semantics.
/// </summary>
public readonly record struct Point3D(double X, double Y, double Z)
{
    public static readonly Point3D Origin = new(0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceTo(Point3D other) => Math.Sqrt(DistanceSquaredTo(other));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double DistanceSquaredTo(Point3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D ToVector3D() => new(X, Y, Z);

    // Operator Overloads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3D operator +(Point3D p, Vector3D v) => new(p.X + v.X, p.Y + v.Y, p.Z + v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3D operator -(Point3D p, Vector3D v) => new(p.X - v.X, p.Y - v.Y, p.Z - v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator -(Point3D a, Point3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3D(Point3D p) => new(p.X, p.Y, p.Z);

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
}
