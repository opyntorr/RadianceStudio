namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a circular rail in 3D space with exact closed-form arc-length and Frenet evaluation.
/// </summary>
public sealed class CircleCurve3D : ICurve3D
{
    private readonly Point3D _center;
    private readonly double _radius;
    private readonly Vector3D _planeNormal;
    private readonly Vector3D _uAxis;
    private readonly Vector3D _vAxis;
    private readonly double _length;

    public Point3D Center => _center;
    public double Radius => _radius;
    public Vector3D PlaneNormal => _planeNormal;
    public double Length => _length;
    public bool IsClosed => true;

    public CircleCurve3D(Point3D center, double radius, Vector3D? planeNormal = null, Vector3D? referenceAxis = null)
    {
        if (radius <= 1e-12)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Circle radius must be strictly positive.");
        }

        _center = center;
        _radius = radius;
        _planeNormal = (planeNormal ?? new Vector3D(0, 0, 1)).Normalize();

        Vector3D refU = referenceAxis ?? (Math.Abs(_planeNormal.Z) < 0.999 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0));
        _uAxis = Vector3D.Cross(_planeNormal, refU).Normalize();
        _vAxis = Vector3D.Cross(_planeNormal, _uAxis).Normalize();
        _length = 2.0 * Math.PI * radius;
    }

    public Point3D PointAtArcLength(double s)
    {
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return _center + (_uAxis * (_radius * cos)) + (_vAxis * (_radius * sin));
    }

    public Vector3D TangentAtArcLength(double s)
    {
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return (_vAxis * cos) - (_uAxis * sin);
    }

    public Vector3D NormalAtArcLength(double s)
    {
        // Principal Frenet normal points towards center of curvature
        double theta = s / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return ((_uAxis * -cos) - (_vAxis * sin)).Normalize();
    }

    public double ParameterAtArcLength(double s) => s / _radius;
}
