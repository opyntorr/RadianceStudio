namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Parametric geometric and distribution configuration for halo melee setting and platform generation.
/// </summary>
public record HaloParameters
{
    /// <summary>
    /// Gem cut type of the surrounding melee stones (default RoundBrilliant).
    /// </summary>
    public GemCutType MeleeGemCut { get; init; } = GemCutType.RoundBrilliant;

    /// <summary>
    /// Nominal diameter of each halo melee stone in millimeters (0.8 to 2.5 mm, default 1.3 mm).
    /// </summary>
    public double MeleeDiameterMm { get; init; } = 1.3;

    /// <summary>
    /// Clearance distance between the center gem girdle and the halo melee stones in millimeters (default 0.2 mm).
    /// </summary>
    public double GuardDistanceMm { get; init; } = 0.2;

    /// <summary>
    /// Girdle-to-girdle clearance spacing between adjacent melee stones in millimeters (default 0.15 mm).
    /// </summary>
    public double SpacingMm { get; init; } = 0.15;

    /// <summary>
    /// Vertical offset of the halo melee girdle plane relative to the center gem girdle plane in millimeters (default -0.3 mm).
    /// </summary>
    public double DropHeightMm { get; init; } = -0.3;

    /// <summary>
    /// Micro-prong tip styling for the halo melee retention.
    /// </summary>
    public ProngStyle ProngStyle { get; init; } = ProngStyle.SharedProng;

    /// <summary>
    /// Nominal thickness of the supporting platform metal wall in millimeters (default 0.6 mm).
    /// </summary>
    public double TrayWallThicknessMm { get; init; } = 0.6;

    /// <summary>
    /// Total vertical depth of the supporting metal tray/rail in millimeters (default 1.0 mm).
    /// </summary>
    public double TrayDepthMm { get; init; } = 1.0;

    /// <summary>
    /// Validates all halo parameters against physical jewelry manufacturing tolerances.
    /// </summary>
    public void Validate()
    {
        if (MeleeDiameterMm <= 0.0 || double.IsNaN(MeleeDiameterMm) || double.IsInfinity(MeleeDiameterMm))
        {
            throw new ArgumentOutOfRangeException(nameof(MeleeDiameterMm), "MeleeDiameterMm must be strictly positive.");
        }

        if (GuardDistanceMm < 0.0 || double.IsNaN(GuardDistanceMm) || double.IsInfinity(GuardDistanceMm))
        {
            throw new ArgumentOutOfRangeException(nameof(GuardDistanceMm), "GuardDistanceMm must be non-negative.");
        }

        if (SpacingMm < 0.0 || double.IsNaN(SpacingMm) || double.IsInfinity(SpacingMm))
        {
            throw new ArgumentOutOfRangeException(nameof(SpacingMm), "SpacingMm must be non-negative.");
        }

        if (TrayWallThicknessMm <= 0.0 || double.IsNaN(TrayWallThicknessMm))
        {
            throw new ArgumentOutOfRangeException(nameof(TrayWallThicknessMm), "TrayWallThicknessMm must be strictly positive.");
        }

        if (TrayDepthMm <= 0.0 || double.IsNaN(TrayDepthMm))
        {
            throw new ArgumentOutOfRangeException(nameof(TrayDepthMm), "TrayDepthMm must be strictly positive.");
        }
    }
}
