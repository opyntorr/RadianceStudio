namespace RadianceStudio.Core.Models;

using System.Collections.Generic;
using RadianceStudio.Core.Geometry;

/// <summary>
/// Represents a micro-grain retention bead placed in an interstitial centroid or along a perimeter edge.
/// </summary>
public record PaveBeadPlacement
{
    /// <summary>
    /// Center position of the spherical bead solid in 3D space.
    /// </summary>
    public Point3D Center { get; init; }

    /// <summary>
    /// Outward surface normal vector orienting the bead dome.
    /// </summary>
    public Vector3D Normal { get; init; }

    /// <summary>
    /// Outer diameter of the spherical bead in millimeters.
    /// </summary>
    public double DiameterMm { get; init; }

    /// <summary>
    /// Stone indices retained by this bead (typically 3 or 4 for interstitial beads, 1 or 2 for perimeter beads).
    /// </summary>
    public List<int> AssociatedStoneIndices { get; init; } = new();
}
