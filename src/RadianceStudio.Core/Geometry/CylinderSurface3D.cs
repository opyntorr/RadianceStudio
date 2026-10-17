namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a cylindrical reference surface with metric parameterization along the circumference and axis.
/// </summary>
public sealed class CylinderSurface3D : ISurface3D
{
    private readonly Point3D _origin;
    private readonly double _radius;
    private readonly Vector3D _axis;
    private readonly Vector3D _uRef;
    private readonly Vector3D _vRef;

    public double Radius => _radius;
    public Point3D Origin => _origin;
    public Vector3D Axis => _axis;

    public CylinderSurface3D(double radius, Point3D origin = default, Vector3D axis = default)
    {
        if (radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Cylinder radius must be strictly positive.");
        }

        _radius = radius;
        _origin = origin;
        _axis = axis.IsZero() ? Vector3D.UnitZ : axis.Normalize();

        if (Math.Abs(_axis.Z) < 0.999)
        {
            _uRef = new Vector3D(0, 0, 1).Cross(_axis).Normalize();
        }
        else
        {
            _uRef = Vector3D.UnitX;
        }
        _vRef = _axis.Cross(_uRef).Normalize();
    }

    public Vector3D NormalAtPoint(Point3D point)
    {
        var v = point - _origin;
        double proj = v.Dot(_axis);
        var radial = v - (_axis * proj);
        return radial.IsZero() ? _uRef : radial.Normalize();
    }

    public Point3D PointAt(double u, double v)
    {
        double theta = u / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);

        var radial = (_uRef * cos) + (_vRef * sin);
        return _origin + (radial * _radius) + (_axis * v);
    }

    public Vector3D NormalAt(double u, double v)
    {
        double theta = u / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return ((_uRef * cos) + (_vRef * sin)).Normalize();
    }

    public Vector3D TangentAt(double u, double v)
    {
        double theta = u / _radius;
        double cos = Math.Cos(theta);
        double sin = Math.Sin(theta);
        return ((_vRef * cos) - (_uRef * sin)).Normalize();
    }
}
