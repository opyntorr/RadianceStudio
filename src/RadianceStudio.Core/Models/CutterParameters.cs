namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Parametric geometric configuration for procedural gem cutter solids.
/// Used for boolean subtraction against metal (shanks, bezels, channels, halos).
/// </summary>
public record CutterParameters
{
    /// <summary>
    /// Extra radial offset clearance around the girdle for setting tolerance in millimeters (range [0.02, 0.25], default 0.08 mm).
    /// </summary>
    public double GirdleClearanceMm { get; init; } = 0.08;

    /// <summary>
    /// Geometric style and angle of the bearing seat cut (default Degree70).
    /// </summary>
    public CutterSeatStyle SeatAngle { get; init; } = CutterSeatStyle.Degree70;

    /// <summary>
    /// Height extending upward past the gem table in millimeters to pierce through metal surfaces (default 1.5 mm).
    /// </summary>
    public double CrownHeightExtensionMm { get; init; } = 1.5;

    /// <summary>
    /// Percentage clearance offset under pavilion facets relative to gem diameter (default 5%).
    /// </summary>
    public double PavilionOffsetPercent { get; init; } = 5.0;

    /// <summary>
    /// Ratio of the piercing light hole diameter at the bottom relative to gem diameter (range [0.2, 0.8], default 0.50).
    /// </summary>
    public double LightHoleDiameterRatio { get; init; } = 0.50;

    /// <summary>
    /// Depth extending downward past the gem culet in millimeters to punch fully through metal (default 2.0 mm).
    /// </summary>
    public double LightHoleDepthExtensionMm { get; init; } = 2.0;

    /// <summary>
    /// Whether the piercing light hole shaft tapers outwards toward the bottom for casting release, or remains straight.
    /// </summary>
    public bool TaperShaft { get; init; } = false;

    /// <summary>
    /// Validates all parameters against fabrication and geometric invariants.
    /// </summary>
    public void Validate()
    {
        if (double.IsNaN(GirdleClearanceMm) || double.IsInfinity(GirdleClearanceMm) || GirdleClearanceMm < 0.02 || GirdleClearanceMm > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(GirdleClearanceMm), $"GirdleClearanceMm must be between 0.02 and 0.25 mm, but got {GirdleClearanceMm}.");
        }

        if (double.IsNaN(CrownHeightExtensionMm) || double.IsInfinity(CrownHeightExtensionMm) || CrownHeightExtensionMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(CrownHeightExtensionMm), "CrownHeightExtensionMm must be strictly positive.");
        }

        if (double.IsNaN(PavilionOffsetPercent) || double.IsInfinity(PavilionOffsetPercent) || PavilionOffsetPercent < 0.0 || PavilionOffsetPercent > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PavilionOffsetPercent), $"PavilionOffsetPercent must be between 0% and 100%, but got {PavilionOffsetPercent}.");
        }

        if (double.IsNaN(LightHoleDiameterRatio) || double.IsInfinity(LightHoleDiameterRatio) || LightHoleDiameterRatio < 0.20 || LightHoleDiameterRatio > 0.80)
        {
            throw new ArgumentOutOfRangeException(nameof(LightHoleDiameterRatio), $"LightHoleDiameterRatio must be between 0.20 and 0.80, but got {LightHoleDiameterRatio}.");
        }

        if (double.IsNaN(LightHoleDepthExtensionMm) || double.IsInfinity(LightHoleDepthExtensionMm) || LightHoleDepthExtensionMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(LightHoleDepthExtensionMm), "LightHoleDepthExtensionMm must be strictly positive.");
        }
    }
}
