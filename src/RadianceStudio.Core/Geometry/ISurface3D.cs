namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Defines a 3D reference surface for evaluating surface normals at given 3D positions.
/// </summary>
public interface ISurface3D
{
    /// <summary>
    /// Evaluates the unit outward normal vector of the surface at or closest to the specified 3D point.
    /// </summary>
    Vector3D NormalAtPoint(Point3D point);
}
