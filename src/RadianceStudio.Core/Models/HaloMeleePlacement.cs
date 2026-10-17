namespace RadianceStudio.Core.Models;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;

/// <summary>
/// Represents a discrete halo melee gem placement with spatial orientation and micro-prong locations.
/// </summary>
public record HaloMeleePlacement
{
    /// <summary>
    /// Zero-based sequential index of the melee gem along the halo loop.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gem cut type of this melee stone.
    /// </summary>
    public GemCutType CutType { get; init; } = GemCutType.RoundBrilliant;

    /// <summary>
    /// Nominal diameter of this melee stone in millimeters.
    /// </summary>
    public double DiameterMm { get; init; } = 1.3;

    /// <summary>
    /// 3D center position of the melee stone girdle.
    /// </summary>
    public Point3D Position { get; init; } = new(0, 0, 0);

    /// <summary>
    /// Unit tangent vector along the halo perimeter curve at this placement station.
    /// </summary>
    public Vector3D Tangent { get; init; } = new(1, 0, 0);

    /// <summary>
    /// Outward unit normal vector perpendicular to the halo curve at this placement station.
    /// </summary>
    public Vector3D Normal { get; init; } = new(0, 1, 0);

    /// <summary>
    /// Unit binormal vector perpendicular to the tangent-normal plane (along the gem's optical table axis).
    /// </summary>
    public Vector3D Binormal { get; init; } = new(0, 0, 1);

    /// <summary>
    /// 4x4 row-major affine transformation matrix transforming canonical gem mesh into the placed frame.
    /// </summary>
    public double[] Matrix4x4 { get; init; } = new double[16]
    {
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    };

    /// <summary>
    /// 3D center coordinates of micro-prongs associated with this melee stone (shared prongs, inner/outer prongs).
    /// </summary>
    public IReadOnlyList<Point3D> MicroProngPositions { get; init; } = Array.Empty<Point3D>();
}
