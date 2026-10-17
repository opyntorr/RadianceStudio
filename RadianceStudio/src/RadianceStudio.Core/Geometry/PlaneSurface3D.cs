namespace RadianceStudio.Core.Geometry;

using System;

/// <summary>
/// Represents a flat 3D plane reference surface.
/// </summary>
public sealed class PlaneSurface3D : ISurface3D
{
    private readonly Vector3D _normal;

    public Vector3D Normal => _normal;

    public PlaneSurface3D(Vector3D normal)
    {
        if (normal.IsZero())
        {
            throw new ArgumentException("Plane normal cannot be zero.", nameof(normal));
        }

        _normal = normal.Normalize();
    }

    public Vector3D NormalAtPoint(Point3D point) => _normal;
}
