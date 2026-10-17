namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Parametric configuration for procedural pavé stone layouts on 3D surfaces.
/// Controls packing density, bead retention grains, border clearances, and surface relaxation.
/// </summary>
public record PaveParameters
{
    /// <summary>
    /// Geometric packing layout pattern across the surface domain (default Hexagonal).
    /// </summary>
    public PavePatternType PatternType { get; init; } = PavePatternType.Hexagonal;

    /// <summary>
    /// Retention prong style and border machining profile (default Bead).
    /// </summary>
    public PaveProngType ProngType { get; init; } = PaveProngType.Bead;

    /// <summary>
    /// Nominal gem diameter in millimeters (range [0.8, 3.0], default 1.3 mm).
    /// </summary>
    public double StoneDiameterMm { get; init; } = 1.3;

    /// <summary>
    /// Clear separation between adjacent stone girdles in millimeters (range [0.05, 0.40], default 0.15 mm).
    /// </summary>
    public double StoneSpacingMm { get; init; } = 0.15;

    /// <summary>
    /// Minimum clearance from boundary edges to stone girdles in millimeters (default 0.20 mm).
    /// </summary>
    public double BorderOffsetMm { get; init; } = 0.20;

    /// <summary>
    /// Diameter of spherical micro-bead retention grains in millimeters (range [0.3, 0.8], default 0.50 mm).
    /// </summary>
    public double BeadDiameterMm { get; init; } = 0.50;

    /// <summary>
    /// Total elevation height of retention beads above the metal surface in millimeters (default 0.40 mm).
    /// </summary>
    public double BeadHeightMm { get; init; } = 0.40;

    /// <summary>
    /// Number of Lloyd's Voronoi relaxation iterations to equalize stone spacing on curved surfaces (default 10).
    /// </summary>
    public int RelaxationIterations { get; init; } = 10;

    /// <summary>
    /// Gem cut profile geometry (default RoundBrilliant).
    /// </summary>
    public GemCutType CutType { get; init; } = GemCutType.RoundBrilliant;

    /// <summary>
    /// Validates all parameters against jewelry fabrication tolerances and geometric limits.
    /// </summary>
    public void Validate()
    {
        if (StoneDiameterMm < 0.8 || StoneDiameterMm > 3.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(StoneDiameterMm),
                StoneDiameterMm,
                "Stone diameter must be between 0.8 mm and 3.0 mm for pavé settings.");
        }

        if (StoneSpacingMm < 0.05 || StoneSpacingMm > 0.40)
        {
            throw new ArgumentOutOfRangeException(
                nameof(StoneSpacingMm),
                StoneSpacingMm,
                "Stone spacing must be between 0.05 mm and 0.40 mm.");
        }

        if (BorderOffsetMm < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BorderOffsetMm),
                BorderOffsetMm,
                "Border offset cannot be negative.");
        }

        if (BeadDiameterMm < 0.3 || BeadDiameterMm > 0.8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BeadDiameterMm),
                BeadDiameterMm,
                "Bead diameter must be between 0.3 mm and 0.8 mm.");
        }

        if (BeadHeightMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BeadHeightMm),
                BeadHeightMm,
                "Bead height must be strictly positive.");
        }

        if (RelaxationIterations < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RelaxationIterations),
                RelaxationIterations,
                "Relaxation iterations cannot be negative.");
        }
    }
}
