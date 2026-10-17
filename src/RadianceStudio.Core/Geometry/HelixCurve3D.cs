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

    public Point3D Center => _center;
    public double Radius => _radius;
    public double Pitch => _pitch;
    public double Turns => _turns;
    public double Length => _length;
    public bool IsClosed => false;

    public HelixCurve3D(Point3D center, double radius, double pitch, double turns)
    {
        if (radius <= 1e-12) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be positive.");
        if (turns <= 1e-12) throw new ArgumentOutOfRangeException(nameof(turns), "Turns must be positive.");

        _center = center;
        _radius = radius;
        _pitch = pitch;
        _turns = turns;
        _c = pitch / (2.0 * Math.PI);
        _omega = Math.Sqrt((radius * radius) + (_c * _c));
        _length = _omega * (2.0 * Math.PI * turns);
    }

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
