namespace RadianceStudio.Core.Geometry;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a 4x4 double-precision affine transformation matrix in row-major layout.
/// Optimized for stack allocation and zero-heap instantiation.
/// </summary>
public readonly struct Matrix4x4D : IEquatable<Matrix4x4D>
{
    // Row 0
    public readonly double M00, M01, M02, M03;
    // Row 1
    public readonly double M10, M11, M12, M13;
    // Row 2
    public readonly double M20, M21, M22, M23;
    // Row 3
    public readonly double M30, M31, M32, M33;

    public static readonly Matrix4x4D Identity = new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    );

    public static readonly Matrix4x4D Zero = new(
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0
    );

    public Matrix4x4D(
        double m00, double m01, double m02, double m03,
        double m10, double m11, double m12, double m13,
        double m20, double m21, double m22, double m23,
        double m30, double m31, double m32, double m33)
    {
        M00 = m00; M01 = m01; M02 = m02; M03 = m03;
        M10 = m10; M11 = m11; M12 = m12; M13 = m13;
        M20 = m20; M21 = m21; M22 = m22; M23 = m23;
        M30 = m30; M31 = m31; M32 = m32; M33 = m33;
    }

    public Matrix4x4D(ReadOnlySpan<double> values)
    {
        if (values.Length < 16)
        {
            throw new ArgumentException("Array must contain at least 16 elements.", nameof(values));
        }
        M00 = values[0];  M01 = values[1];  M02 = values[2];  M03 = values[3];
        M10 = values[4];  M11 = values[5];  M12 = values[6];  M13 = values[7];
        M20 = values[8];  M21 = values[9];  M22 = values[10]; M23 = values[11];
        M30 = values[12]; M31 = values[13]; M32 = values[14]; M33 = values[15];
    }

    public double this[int row, int col]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (row, col) switch
        {
            (0, 0) => M00, (0, 1) => M01, (0, 2) => M02, (0, 3) => M03,
            (1, 0) => M10, (1, 1) => M11, (1, 2) => M12, (1, 3) => M13,
            (2, 0) => M20, (2, 1) => M21, (2, 2) => M22, (2, 3) => M23,
            (3, 0) => M30, (3, 1) => M31, (3, 2) => M32, (3, 3) => M33,
            _ => throw new IndexOutOfRangeException($"Index ({row}, {col}) is out of bounds for Matrix4x4D.")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double[] ToRowMajorArray()
    {
        return new double[16]
        {
            M00, M01, M02, M03,
            M10, M11, M12, M13,
            M20, M21, M22, M23,
            M30, M31, M32, M33
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(double[] destination, int startIndex = 0)
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (destination.Length - startIndex < 16) throw new ArgumentException("Destination array too small.");

        destination[startIndex + 0] = M00;  destination[startIndex + 1] = M01;
        destination[startIndex + 2] = M02;  destination[startIndex + 3] = M03;
        destination[startIndex + 4] = M10;  destination[startIndex + 5] = M11;
        destination[startIndex + 6] = M12;  destination[startIndex + 7] = M13;
        destination[startIndex + 8] = M20;  destination[startIndex + 9] = M21;
        destination[startIndex + 10] = M22; destination[startIndex + 11] = M23;
        destination[startIndex + 12] = M30; destination[startIndex + 13] = M31;
        destination[startIndex + 14] = M32; destination[startIndex + 15] = M33;
    }

    /// <summary>
    /// Calculates the determinant of the upper-left 3x3 rotation/scale submatrix.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Determinant3x3()
    {
        return M00 * (M11 * M22 - M12 * M21)
             - M01 * (M10 * M22 - M12 * M20)
             + M02 * (M10 * M21 - M11 * M20);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3D Transform(Point3D point)
    {
        double x = M00 * point.X + M01 * point.Y + M02 * point.Z + M03;
        double y = M10 * point.X + M11 * point.Y + M12 * point.Z + M13;
        double z = M20 * point.X + M21 * point.Y + M22 * point.Z + M23;
        double w = M30 * point.X + M31 * point.Y + M32 * point.Z + M33;

        if (Math.Abs(w - 1.0) > 1e-12 && Math.Abs(w) > 1e-12)
        {
            double invW = 1.0 / w;
            return new Point3D(x * invW, y * invW, z * invW);
        }

        return new Point3D(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D Transform(Vector3D vector)
    {
        double x = M00 * vector.X + M01 * vector.Y + M02 * vector.Z;
        double y = M10 * vector.X + M11 * vector.Y + M12 * vector.Z;
        double z = M20 * vector.X + M21 * vector.Y + M22 * vector.Z;
        return new Vector3D(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4D Multiply(Matrix4x4D other)
    {
        return new Matrix4x4D(
            // Row 0
            M00 * other.M00 + M01 * other.M10 + M02 * other.M20 + M03 * other.M30,
            M00 * other.M01 + M01 * other.M11 + M02 * other.M21 + M03 * other.M31,
            M00 * other.M02 + M01 * other.M12 + M02 * other.M22 + M03 * other.M32,
            M00 * other.M03 + M01 * other.M13 + M02 * other.M23 + M03 * other.M33,

            // Row 1
            M10 * other.M00 + M11 * other.M10 + M12 * other.M20 + M13 * other.M30,
            M10 * other.M01 + M11 * other.M11 + M12 * other.M21 + M13 * other.M31,
            M10 * other.M02 + M11 * other.M12 + M12 * other.M22 + M13 * other.M32,
            M10 * other.M03 + M11 * other.M13 + M12 * other.M23 + M13 * other.M33,

            // Row 2
            M20 * other.M00 + M21 * other.M10 + M22 * other.M20 + M23 * other.M30,
            M20 * other.M01 + M21 * other.M11 + M22 * other.M21 + M23 * other.M31,
            M20 * other.M02 + M21 * other.M12 + M22 * other.M22 + M23 * other.M32,
            M20 * other.M03 + M21 * other.M13 + M22 * other.M23 + M23 * other.M33,

            // Row 3
            M30 * other.M00 + M31 * other.M10 + M32 * other.M20 + M33 * other.M30,
            M30 * other.M01 + M31 * other.M11 + M32 * other.M21 + M33 * other.M31,
            M30 * other.M02 + M31 * other.M12 + M32 * other.M22 + M33 * other.M32,
            M30 * other.M03 + M31 * other.M13 + M32 * other.M23 + M33 * other.M33
        );
    }

    // Factory Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateTranslation(double x, double y, double z) => new(
        1, 0, 0, x,
        0, 1, 0, y,
        0, 0, 1, z,
        0, 0, 0, 1
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateTranslation(Vector3D v) => CreateTranslation(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateTranslation(Point3D p) => CreateTranslation(p.X, p.Y, p.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateScale(double s) => CreateScale(s, s, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateScale(double sx, double sy, double sz) => new(
        sx, 0,  0,  0,
        0,  sy, 0,  0,
        0,  0,  sz, 0,
        0,  0,  0,  1
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateRotationZ(double angleRadians)
    {
        double cos = Math.Cos(angleRadians);
        double sin = Math.Sin(angleRadians);
        return new Matrix4x4D(
            cos, -sin, 0, 0,
            sin,  cos, 0, 0,
            0,    0,   1, 0,
            0,    0,   0, 1
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateRotationZDegrees(double angleDegrees) =>
        CreateRotationZ(angleDegrees * (Math.PI / 180.0));

    /// <summary>
    /// Creates a transformation matrix mapping canonical gem basis to the given orthonormal frame and position.
    /// Canonical mapping: X -> Binormal, Y -> Tangent, Z -> Normal, Origin -> Position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreateFromFrame(OrthonormalFrame frame, Point3D position) => new(
        frame.Binormal.X, frame.Tangent.X, frame.Normal.X, position.X,
        frame.Binormal.Y, frame.Tangent.Y, frame.Normal.Y, position.Y,
        frame.Binormal.Z, frame.Tangent.Z, frame.Normal.Z, position.Z,
        0,                0,               0,              1
    );

    /// <summary>
    /// Direct closed-form composition for gem instance placement:
    /// M = [B  T*  N  P] * Rz(theta) * S(gemSize, gemSize * aspectRatio, gemSize)
    /// Executes in single-digit nanoseconds with zero heap allocations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D CreatePlacement(
        OrthonormalFrame frame,
        Point3D position,
        double gemSizeMm,
        double aspectRatio = 1.0,
        double rotationAngleDeg = 0.0)
    {
        double rad = rotationAngleDeg * (Math.PI / 180.0);
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);

        double sx = gemSizeMm;
        double sy = gemSizeMm * aspectRatio;
        double sz = gemSizeMm;

        // Column 0 = B * (sx * cos) + T* * (sx * sin)
        double c00 = frame.Binormal.X * (sx * cos) + frame.Tangent.X * (sx * sin);
        double c10 = frame.Binormal.Y * (sx * cos) + frame.Tangent.Y * (sx * sin);
        double c20 = frame.Binormal.Z * (sx * cos) + frame.Tangent.Z * (sx * sin);

        // Column 1 = B * (-sy * sin) + T* * (sy * cos)
        double c01 = frame.Binormal.X * (-sy * sin) + frame.Tangent.X * (sy * cos);
        double c11 = frame.Binormal.Y * (-sy * sin) + frame.Tangent.Y * (sy * cos);
        double c21 = frame.Binormal.Z * (-sy * sin) + frame.Tangent.Z * (sy * cos);

        // Column 2 = N * sz
        double c02 = frame.Normal.X * sz;
        double c12 = frame.Normal.Y * sz;
        double c22 = frame.Normal.Z * sz;

        return new Matrix4x4D(
            c00, c01, c02, position.X,
            c10, c11, c12, position.Y,
            c20, c21, c22, position.Z,
            0,   0,   0,   1
        );
    }

    // Operator Overloads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4D operator *(Matrix4x4D a, Matrix4x4D b) => a.Multiply(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point3D operator *(Matrix4x4D m, Point3D p) => m.Transform(p);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D operator *(Matrix4x4D m, Vector3D v) => m.Transform(v);

    public bool Equals(Matrix4x4D other)
    {
        return M00.Equals(other.M00) && M01.Equals(other.M01) && M02.Equals(other.M02) && M03.Equals(other.M03) &&
               M10.Equals(other.M10) && M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) &&
               M20.Equals(other.M20) && M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) &&
               M30.Equals(other.M30) && M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33);
    }

    public override bool Equals(object? obj) => obj is Matrix4x4D other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(M00); hash.Add(M01); hash.Add(M02); hash.Add(M03);
        hash.Add(M10); hash.Add(M11); hash.Add(M12); hash.Add(M13);
        hash.Add(M20); hash.Add(M21); hash.Add(M22); hash.Add(M23);
        hash.Add(M30); hash.Add(M31); hash.Add(M32); hash.Add(M33);
        return hash.ToHashCode();
    }

    public static bool operator ==(Matrix4x4D left, Matrix4x4D right) => left.Equals(right);
    public static bool operator !=(Matrix4x4D left, Matrix4x4D right) => !left.Equals(right);
}
