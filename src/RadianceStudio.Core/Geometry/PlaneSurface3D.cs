namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a flat 3D plane reference surface.
/// </summary>
public sealed class PlaneSurface3D : ISurface3D
{
    private readonly Point3D _origin;
    private readonly Vector3D _normal;
    private readonly Vector3D _uDir;
    private readonly Vector3D _vDir;

    public Point3D Origin => _origin;
    public Vector3D Normal => _normal;
    public Vector3D UDir => _uDir;
    public Vector3D VDir => _vDir;

    public PlaneSurface3D(Vector3D normal)
        : this(Point3D.Origin, normal)
    {
    }

    public PlaneSurface3D(Point3D origin, Vector3D normal)
    {
        if (normal.IsZero())
        {
            throw new ArgumentException("Plane normal cannot be zero.", nameof(normal));
        }

        _origin = origin;
        _normal = normal.Normalize();

        if (Math.Abs(_normal.Z) < 0.999)
        {
            _uDir = new Vector3D(0, 0, 1).Cross(_normal).Normalize();
            _vDir = _normal.Cross(_uDir).Normalize();
        }
        else
        {
            var tempV = _normal.Cross(Vector3D.UnitX);
            if (tempV.IsZero())
            {
                tempV = _normal.Cross(Vector3D.UnitY);
            }
            _vDir = tempV.Normalize();
            _uDir = _vDir.Cross(_normal).Normalize();
            if (_normal.Z > 0.0)
            {
                // Align standard XY for +Z normal
                _uDir = Vector3D.UnitX;
                _vDir = Vector3D.UnitY;
            }
        }
    }

    public PlaneSurface3D(Point3D origin, Vector3D uDir, Vector3D vDir)
    {
        if (uDir.IsZero() || vDir.IsZero())
        {
            throw new ArgumentException("Plane direction vectors cannot be zero.");
        }

        _origin = origin;
        _uDir = uDir.Normalize();
        var normal = _uDir.Cross(vDir);
        if (normal.IsZero())
        {
            throw new ArgumentException("Direction vectors cannot be collinear.");
        }
        _normal = normal.Normalize();
        _vDir = _normal.Cross(_uDir).Normalize();
    }

    public Vector3D NormalAtPoint(Point3D point) => _normal;

    public Point3D PointAt(double u, double v) => _origin + (_uDir * u) + (_vDir * v);

    public Vector3D NormalAt(double u, double v) => _normal;

    public Vector3D TangentAt(double u, double v) => _uDir;
}
