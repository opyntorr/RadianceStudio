namespace RadianceStudio.Core.Enums;

/// <summary>
/// Geometric packing patterns for procedural stone placement on 3D surfaces.
/// </summary>
public enum PavePatternType
{
    /// <summary>
    /// Equilateral triangular grid providing maximum packing density.
    /// </summary>
    Hexagonal = 0,

    /// <summary>
    /// Alternating staggered linear rows.
    /// </summary>
    OffsetLinear = 1,

    /// <summary>
    /// Orthogonal Cartesian grid.
    /// </summary>
    Grid = 2,

    /// <summary>
    /// French pavé strip with U-cut scalloped borders.
    /// </summary>
    Fishtail = 3
}
