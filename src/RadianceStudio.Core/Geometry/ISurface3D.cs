namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Defines a 3D reference surface for evaluating surface positions, normals, and tangents.
/// </summary>
public interface ISurface3D
{
    /// <summary>
    /// Evaluates the unit outward normal vector of the surface at or closest to the specified 3D point.
    /// </summary>
    Vector3D NormalAtPoint(Point3D point);

    /// <summary>
    /// Evaluates the 3D surface point S(u, v) at parametric coordinates (u, v).
    /// </summary>
    Point3D PointAt(double u, double v);

    /// <summary>
    /// Evaluates the unit outward normal vector N(u, v) at parametric coordinates (u, v).
    /// </summary>
    Vector3D NormalAt(double u, double v);

    /// <summary>
    /// Evaluates the principal surface tangent vector T(u, v) along the primary u parameter direction.
    /// </summary>
    Vector3D TangentAt(double u, double v);
}
