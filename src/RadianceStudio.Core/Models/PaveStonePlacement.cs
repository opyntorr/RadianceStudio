namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Geometry;

/// <summary>
/// Represents a single positioned gem instance within a procedural pavé surface layout.
/// </summary>
public record PaveStonePlacement
{
    /// <summary>
    /// Sequential zero-based index of this stone within the pavé layout.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Evaluated 3D coordinates of the stone center on the surface.
    /// </summary>
    public Point3D Center { get; init; }

    /// <summary>
    /// Outward unit surface normal vector at the stone center.
    /// </summary>
    public Vector3D Normal { get; init; }

    /// <summary>
    /// Principal tangent vector at the stone center along the primary layout direction.
    /// </summary>
    public Vector3D Tangent { get; init; }

    /// <summary>
    /// 4x4 affine transformation matrix orienting and scaling the canonical gem mesh into 3D space.
    /// </summary>
    public Matrix4x4D Transform { get; init; }

    /// <summary>
    /// Outer girdle diameter of the placed stone in millimeters.
    /// </summary>
    public double DiameterMm { get; init; }

    /// <summary>
    /// Parametric U coordinate on the surface domain.
    /// </summary>
    public double U { get; init; }

    /// <summary>
    /// Parametric V coordinate on the surface domain.
    /// </summary>
    public double V { get; init; }
}
