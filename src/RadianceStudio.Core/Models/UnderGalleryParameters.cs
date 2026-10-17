namespace RadianceStudio.Core.Models;

using System;

/// <summary>
/// Decorative pierce-through and openwork patterns for ring under-galleries (azuradors).
/// </summary>
public enum UnderGalleryPattern
{
    /// <summary>
    /// Smooth hollowed interior chamber without openwork piercings.
    /// </summary>
    None = 0,

    /// <summary>
    /// Hexagonal honeycomb lattice pattern for optimal light entry and weight reduction.
    /// </summary>
    Honeycomb = 1,

    /// <summary>
    /// Elegant curvilinear scrollwork / filigree pierced relief.
    /// </summary>
    Scrollwork = 2,

    /// <summary>
    /// Diagonal crisscross lattice pattern.
    /// </summary>
    Lattice = 3,

    /// <summary>
    /// Traditional circular drilled azur holes beneath stone settings.
    /// </summary>
    Circular = 4
}

/// <summary>
/// Immutable parameters defining an under-gallery (azurador) hollow chamber beneath stone settings.
/// Strictly enforces jewelry casting structural invariants: minimum wall thickness &gt;= 0.8 mm
/// and floor clearance &gt;= 0.8 mm.
/// </summary>
public record UnderGalleryParameters
{
    /// <summary>
    /// The absolute minimum allowable metal wall thickness in millimeters for reliable jewelry casting.
    /// Walls thinner than 0.80 mm risk porosity, incomplete mold filling, and structural failure.
    /// </summary>
    public const double MinimumWallThicknessMm = 0.80;

    /// <summary>
    /// The absolute minimum allowable clearance in millimeters between the azurador chamber floor
    /// and the inner finger rail to prevent finger irritation and deformation.
    /// </summary>
    public const double MinimumFloorClearanceMm = 0.80;

    private readonly double _wallThicknessMm = 0.85;
    private readonly double _floorClearanceMm = 0.85;
    private readonly double _spanDegrees = 45.0;
    private readonly double _holeDiameterMm = 1.0;
    private readonly double _spacingMm = 0.4;

    /// <summary>
    /// Initializes a new instance of <see cref="UnderGalleryParameters"/> with standard jewelry defaults
    /// compliant with the 0.8 mm casting structural invariant.
    /// </summary>
    public UnderGalleryParameters()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UnderGalleryParameters"/> with specified dimensions and pattern.
    /// </summary>
    /// <param name="wallThicknessMm">Structural metal wall thickness around chamber (&gt;= 0.80 mm).</param>
    /// <param name="floorClearanceMm">Clearance between azurador ceiling/floor and inner finger rail (&gt;= 0.80 mm).</param>
    /// <param name="pattern">Decorative openwork pattern style.</param>
    /// <param name="spanDegrees">Angular span in degrees under the stone setting (typically 20 deg to 90 deg).</param>
    /// <param name="holeDiameterMm">Nominal diameter of openwork piercings in millimeters.</param>
    /// <param name="spacingMm">Spacing between adjacent openwork piercings in millimeters.</param>
    public UnderGalleryParameters(
        double wallThicknessMm = 0.85,
        double floorClearanceMm = 0.85,
        UnderGalleryPattern pattern = UnderGalleryPattern.Honeycomb,
        double spanDegrees = 45.0,
        double holeDiameterMm = 1.0,
        double spacingMm = 0.4)
    {
        Validate(wallThicknessMm, floorClearanceMm, spanDegrees, holeDiameterMm, spacingMm);

        _wallThicknessMm = wallThicknessMm;
        _floorClearanceMm = floorClearanceMm;
        Pattern = pattern;
        _spanDegrees = spanDegrees;
        _holeDiameterMm = holeDiameterMm;
        _spacingMm = spacingMm;
    }

    /// <summary>
    /// Metal wall thickness surrounding the under-gallery chamber in millimeters.
    /// Must be strictly &gt;= 0.80 mm.
    /// </summary>
    public double WallThicknessMm
    {
        get => _wallThicknessMm;
        init
        {
            if (value < MinimumWallThicknessMm)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(WallThicknessMm),
                    value,
                    $"Under-gallery wall thickness ({value:F2} mm) must be at least {MinimumWallThicknessMm:F2} mm to prevent casting failure.");
            }
            _wallThicknessMm = value;
        }
    }

    /// <summary>
    /// Floor clearance between the azurador chamber and the inner finger rail in millimeters.
    /// Must be strictly &gt;= 0.80 mm.
    /// </summary>
    public double FloorClearanceMm
    {
        get => _floorClearanceMm;
        init
        {
            if (value < MinimumFloorClearanceMm)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(FloorClearanceMm),
                    value,
                    $"Under-gallery floor clearance ({value:F2} mm) must be at least {MinimumFloorClearanceMm:F2} mm to preserve ring rail integrity.");
            }
            _floorClearanceMm = value;
        }
    }

    /// <summary>
    /// Decorative openwork piercing pattern.
    /// </summary>
    public UnderGalleryPattern Pattern { get; init; } = UnderGalleryPattern.Honeycomb;

    /// <summary>
    /// Total angular span in degrees along the ring crown centered beneath the setting.
    /// </summary>
    public double SpanDegrees
    {
        get => _spanDegrees;
        init
        {
            if (value <= 0.0 || value > 180.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(SpanDegrees),
                    value,
                    "Span angle must be strictly positive and not exceed 180 degrees.");
            }
            _spanDegrees = value;
        }
    }

    /// <summary>
    /// Nominal diameter of pattern piercings in millimeters.
    /// </summary>
    public double HoleDiameterMm
    {
        get => _holeDiameterMm;
        init
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(HoleDiameterMm),
                    value,
                    "Hole diameter must be strictly positive.");
            }
            _holeDiameterMm = value;
        }
    }

    /// <summary>
    /// Spacing between pattern piercings in millimeters.
    /// </summary>
    public double SpacingMm
    {
        get => _spacingMm;
        init
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(SpacingMm),
                    value,
                    "Spacing must be non-negative.");
            }
            _spacingMm = value;
        }
    }

    /// <summary>
    /// Validates all under-gallery parameters against casting and manufacturing constraints.
    /// </summary>
    public static void Validate(
        double wallThicknessMm,
        double floorClearanceMm,
        double spanDegrees,
        double holeDiameterMm,
        double spacingMm)
    {
        if (wallThicknessMm < MinimumWallThicknessMm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(wallThicknessMm),
                wallThicknessMm,
                $"Under-gallery wall thickness ({wallThicknessMm:F2} mm) must be at least {MinimumWallThicknessMm:F2} mm to prevent casting failure.");
        }

        if (floorClearanceMm < MinimumFloorClearanceMm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(floorClearanceMm),
                floorClearanceMm,
                $"Under-gallery floor clearance ({floorClearanceMm:F2} mm) must be at least {MinimumFloorClearanceMm:F2} mm to preserve ring rail integrity.");
        }

        if (spanDegrees <= 0.0 || spanDegrees > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spanDegrees),
                spanDegrees,
                "Span angle must be strictly positive and not exceed 180 degrees.");
        }

        if (holeDiameterMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(holeDiameterMm),
                holeDiameterMm,
                "Hole diameter must be strictly positive.");
        }

        if (spacingMm < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spacingMm),
                spacingMm,
                "Spacing must be non-negative.");
        }
    }

    /// <summary>
    /// Validates current instance properties.
    /// </summary>
    public void Validate()
    {
        Validate(WallThicknessMm, FloorClearanceMm, SpanDegrees, HoleDiameterMm, SpacingMm);
    }
}
