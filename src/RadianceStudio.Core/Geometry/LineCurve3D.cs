namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a linear segment in 3D space with closed-form arc-length evaluation.
/// </summary>
public sealed class LineCurve3D : ICurve3D
{
    private readonly Point3D _start;
    private readonly Point3D _end;
    private readonly Vector3D _direction;
    private readonly Vector3D _normal;
    private readonly double _length;

    public Point3D Start => _start;
    public Point3D End => _end;
    public double Length => _length;
    public bool IsClosed => false;

    public LineCurve3D(Point3D start, Point3D end)
    {
        _start = start;
        _end = end;
        Vector3D delta = end - start;
        _length = delta.Length;
        if (_length < 1e-12)
        {
            throw new ArgumentException("Line segment start and end points must not be coincident.", nameof(end));
        }

        _direction = delta.Normalize();

        // Establish stable reference normal perpendicular to direction
        Vector3D refUp = Math.Abs(_direction.Z) < 0.999 ? new Vector3D(0, 0, 1) : new Vector3D(0, 1, 0);
        Vector3D binormal = Vector3D.Cross(_direction, refUp).Normalize();
        _normal = Vector3D.Cross(binormal, _direction).Normalize();
    }

    public Point3D PointAtArcLength(double s)
    {
        double clampedS = Math.Clamp(s, 0.0, _length);
        return _start + (_direction * clampedS);
    }

    public Vector3D TangentAtArcLength(double s) => _direction;

    public Vector3D NormalAtArcLength(double s) => _normal;

    public double ParameterAtArcLength(double s) => _length > 1e-12 ? Math.Clamp(s / _length, 0.0, 1.0) : 0.0;
}
