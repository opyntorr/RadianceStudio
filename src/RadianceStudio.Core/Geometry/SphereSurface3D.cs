namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a spherical reference surface with non-zero Gaussian curvature K = 1/R^2.
/// Metric parameterization around equator arc length (u) and meridian arc length (v).
/// </summary>
public sealed class SphereSurface3D : ISurface3D
{
    private readonly Point3D _center;
    private readonly double _radius;

    public Point3D Center => _center;
    public double Radius => _radius;

    public SphereSurface3D(double radius, Point3D center = default)
    {
        if (radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Sphere radius must be strictly positive.");
        }

        _radius = radius;
        _center = center;
    }

    public Vector3D NormalAtPoint(Point3D point)
    {
        var v = point - _center;
        return v.IsZero() ? Vector3D.UnitZ : v.Normalize();
    }

    public Point3D PointAt(double u, double v)
    {
        double uRad = u / _radius;
        double vRad = v / _radius;

        double cosV = Math.Cos(vRad);
        double sinV = Math.Sin(vRad);
        double cosU = Math.Cos(uRad);
        double sinU = Math.Sin(uRad);

        return new Point3D(
            _center.X + (_radius * cosV * cosU),
            _center.Y + (_radius * cosV * sinU),
            _center.Z + (_radius * sinV));
    }

    public Vector3D NormalAt(double u, double v)
    {
        double uRad = u / _radius;
        double vRad = v / _radius;

        double cosV = Math.Cos(vRad);
        double sinV = Math.Sin(vRad);
        double cosU = Math.Cos(uRad);
        double sinU = Math.Sin(uRad);

        return new Vector3D(cosV * cosU, cosV * sinU, sinV).Normalize();
    }

    public Vector3D TangentAt(double u, double v)
    {
        double uRad = u / _radius;
        double cosU = Math.Cos(uRad);
        double sinU = Math.Sin(uRad);

        return new Vector3D(-sinU, cosU, 0.0).Normalize();
    }
}
