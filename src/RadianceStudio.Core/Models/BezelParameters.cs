namespace RadianceStudio.Core.Models;

using System;

/// <summary>
/// Parametric geometric and structural configuration for bezel and semi-bezel setting generation.
/// </summary>
public record BezelParameters
{
    /// <summary>
    /// Nominal thickness of the bezel metal wall in millimeters (&gt;= 0.4 mm, default 0.8 mm).
    /// </summary>
    public double WallThicknessMm { get; init; } = 0.8;

    /// <summary>
    /// Vertical metal height extending above the gem's girdle plane in millimeters (default 0.4 mm).
    /// </summary>
    public double HeightAboveGirdleMm { get; init; } = 0.4;

    /// <summary>
    /// Percentage of the girdle thickness utilized for the bearing seat ledge (default 30%).
    /// </summary>
    public double SeatDepthPercent { get; init; } = 30.0;

    /// <summary>
    /// Outward draft taper angle in degrees for casting release (default 5 deg).
    /// </summary>
    public double DraftAngleDeg { get; init; } = 5.0;

    /// <summary>
    /// Diameter ratio of the conical central light hole in the base relative to gem diameter (default 0.5 = 50%).
    /// </summary>
    public double LightHoleDiameterRatio { get; init; } = 0.5;

    /// <summary>
    /// Whether the setting is a semi-bezel with opposing cutout arches for lateral light entry.
    /// </summary>
    public bool IsSemiBezel { get; init; } = false;

    /// <summary>
    /// Angular span in degrees for each opposing semi-bezel opening arch (default 60 deg).
    /// </summary>
    public double SemiBezelOpeningAngleDeg { get; init; } = 60.0;

    /// <summary>
    /// Whether to apply decorative milgrain knurling along the top perimeter rim.
    /// </summary>
    public bool IncludeMilgrain { get; init; } = false;

    /// <summary>
    /// Validates all bezel parameters against structural casting invariants.
    /// </summary>
    public void Validate()
    {
        if (WallThicknessMm < 0.4 || double.IsNaN(WallThicknessMm) || double.IsInfinity(WallThicknessMm))
        {
            throw new ArgumentOutOfRangeException(nameof(WallThicknessMm), $"WallThicknessMm must be at least 0.4 mm for structural casting integrity, but got {WallThicknessMm}.");
        }

        if (HeightAboveGirdleMm <= 0.0 || double.IsNaN(HeightAboveGirdleMm) || double.IsInfinity(HeightAboveGirdleMm))
        {
            throw new ArgumentOutOfRangeException(nameof(HeightAboveGirdleMm), "HeightAboveGirdleMm must be strictly positive.");
        }

        if (SeatDepthPercent <= 0.0 || SeatDepthPercent > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(SeatDepthPercent), $"SeatDepthPercent must be between 0% and 100%, but got {SeatDepthPercent}.");
        }

        if (DraftAngleDeg < 0.0 || DraftAngleDeg > 45.0)
        {
            throw new ArgumentOutOfRangeException(nameof(DraftAngleDeg), $"DraftAngleDeg must be between 0 and 45 degrees, but got {DraftAngleDeg}.");
        }

        if (LightHoleDiameterRatio <= 0.05 || LightHoleDiameterRatio >= 0.95)
        {
            throw new ArgumentOutOfRangeException(nameof(LightHoleDiameterRatio), $"LightHoleDiameterRatio must be between 0.05 and 0.95, but got {LightHoleDiameterRatio}.");
        }

        if (IsSemiBezel && (SemiBezelOpeningAngleDeg <= 0.0 || SemiBezelOpeningAngleDeg >= 180.0))
        {
            throw new ArgumentOutOfRangeException(nameof(SemiBezelOpeningAngleDeg), $"SemiBezelOpeningAngleDeg must be between 0 and 180 degrees, but got {SemiBezelOpeningAngleDeg}.");
        }
    }
}
