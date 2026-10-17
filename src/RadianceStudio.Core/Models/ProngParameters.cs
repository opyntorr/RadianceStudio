namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Parametric geometric and structural configuration for prong head generation.
/// </summary>
public record ProngParameters
{
    /// <summary>
    /// Number of prongs distributed around the gem perimeter (4, 6, or 8).
    /// </summary>
    public int ProngCount { get; init; } = 4;

    /// <summary>
    /// Nominal wire diameter of each prong in millimeters (default 0.8 mm).
    /// </summary>
    public double ProngDiameterMm { get; init; } = 0.8;

    /// <summary>
    /// Total vertical height of the prong in millimeters.
    /// If &lt;= 0, height is automatically calculated from the gem's total depth plus overhang.
    /// </summary>
    public double ProngHeightMm { get; init; } = 2.0;

    /// <summary>
    /// Horizontal inward overhang of the prong tip wrapping over the gem's girdle (0.05 to 0.4 mm).
    /// </summary>
    public double GirdleOverhangMm { get; init; } = 0.15;

    /// <summary>
    /// Percent taper of the prong from base to tip (0% to 50%, default 10%).
    /// </summary>
    public double TaperPercent { get; init; } = 10.0;

    /// <summary>
    /// Outward rake inclination angle in degrees (0 to 15 deg).
    /// </summary>
    public double RakeAngleDeg { get; init; } = 0.0;

    /// <summary>
    /// Geometric tip styling for the prongs.
    /// </summary>
    public ProngStyle Style { get; init; } = ProngStyle.RoundDome;

    /// <summary>
    /// Whether to generate a structural base wire ring connecting prong bottoms.
    /// </summary>
    public bool IncludeBaseWire { get; init; } = true;

    /// <summary>
    /// Wire diameter of the structural base ring in millimeters (default 0.7 mm).
    /// </summary>
    public double BaseWireDiameterMm { get; init; } = 0.7;

    /// <summary>
    /// Whether to generate a mid-height gallery wire ring.
    /// </summary>
    public bool IncludeGalleryWire { get; init; } = false;

    /// <summary>
    /// Wire diameter of the mid-height gallery ring in millimeters (default 0.6 mm).
    /// </summary>
    public double GalleryWireDiameterMm { get; init; } = 0.6;

    /// <summary>
    /// Validates all prong parameters against physical manufacturing invariants.
    /// </summary>
    public void Validate()
    {
        if (ProngCount != 4 && ProngCount != 6 && ProngCount != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(ProngCount), $"ProngCount must be 4, 6, or 8, but got {ProngCount}.");
        }

        if (ProngDiameterMm <= 0.0 || double.IsNaN(ProngDiameterMm) || double.IsInfinity(ProngDiameterMm))
        {
            throw new ArgumentOutOfRangeException(nameof(ProngDiameterMm), "ProngDiameterMm must be strictly positive.");
        }

        if (GirdleOverhangMm < 0.05 || GirdleOverhangMm > 0.40)
        {
            throw new ArgumentOutOfRangeException(nameof(GirdleOverhangMm), $"GirdleOverhangMm must be between 0.05 and 0.40 mm, but got {GirdleOverhangMm}.");
        }

        if (TaperPercent < 0.0 || TaperPercent > 80.0)
        {
            throw new ArgumentOutOfRangeException(nameof(TaperPercent), $"TaperPercent must be between 0% and 80%, but got {TaperPercent}.");
        }

        if (RakeAngleDeg < 0.0 || RakeAngleDeg > 45.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RakeAngleDeg), $"RakeAngleDeg must be between 0 and 45 degrees, but got {RakeAngleDeg}.");
        }

        if (IncludeBaseWire && (BaseWireDiameterMm <= 0.0 || double.IsNaN(BaseWireDiameterMm)))
        {
            throw new ArgumentOutOfRangeException(nameof(BaseWireDiameterMm), "BaseWireDiameterMm must be strictly positive.");
        }

        if (IncludeGalleryWire && (GalleryWireDiameterMm <= 0.0 || double.IsNaN(GalleryWireDiameterMm)))
        {
            throw new ArgumentOutOfRangeException(nameof(GalleryWireDiameterMm), "GalleryWireDiameterMm must be strictly positive.");
        }
    }
}
