namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Immutable parameters defining the cross-section profile, smoothstep tapering,
/// cathedral shoulders, and under-gallery azurador of a parametric ring shank.
/// </summary>
public record ShankParameters
{
    /// <summary>
    /// The absolute minimum allowable metal thickness in millimeters for casting integrity.
    /// </summary>
    public const double MinimumThicknessMm = 0.80;

    private readonly double _bottomWidthMm = 2.0;
    private readonly double _bottomThicknessMm = 1.6;
    private readonly double _topWidthMm = 2.6;
    private readonly double _topThicknessMm = 2.0;
    private readonly double _crownRatio = 0.25;
    private readonly double _knifeEdgeApexRadiusMm = 0.10;
    private readonly double _cathedralArchHeightMm = 2.0;

    /// <summary>
    /// Initializes a new instance of <see cref="ShankParameters"/> with standard jewelry defaults.
    /// </summary>
    public ShankParameters()
    {
    }

    /// <summary>
    /// Initializes a new uniform (non-tapered) instance of <see cref="ShankParameters"/>.
    /// </summary>
    /// <param name="profileType">Cross-section profile type.</param>
    /// <param name="widthMm">Uniform band width in millimeters (&gt; 0).</param>
    /// <param name="thicknessMm">Uniform band thickness in millimeters (&gt;= 0.8 mm).</param>
    /// <param name="underGallery">Optional under-gallery azurador parameters.</param>
    public ShankParameters(
        ShankProfileType profileType,
        double widthMm,
        double thicknessMm,
        UnderGalleryParameters? underGallery = null)
        : this(profileType, widthMm, thicknessMm, widthMm, thicknessMm, underGallery)
    {
    }

    /// <summary>
    /// Initializes a new parametric tapered instance of <see cref="ShankParameters"/>.
    /// </summary>
    /// <param name="profileType">Cross-section profile type.</param>
    /// <param name="bottomWidthMm">Band width at the base / bottom in millimeters (&gt; 0).</param>
    /// <param name="bottomThicknessMm">Band thickness at the base / bottom in millimeters (&gt;= 0.8 mm).</param>
    /// <param name="topWidthMm">Band width at the head / top in millimeters (&gt; 0).</param>
    /// <param name="topThicknessMm">Band thickness at the head / top in millimeters (&gt;= 0.8 mm).</param>
    /// <param name="underGallery">Optional under-gallery azurador parameters.</param>
    /// <param name="crownRatio">Inner crowning depth ratio for ComfortFit (&gt;= 0 and &lt; 0.5).</param>
    /// <param name="knifeEdgeApexRadiusMm">Apex fillet radius for KnifeEdge in millimeters (&gt;= 0).</param>
    /// <param name="cathedralArchHeightMm">Shoulder arch elevation for Cathedral profile in millimeters (&gt;= 0).</param>
    public ShankParameters(
        ShankProfileType profileType,
        double bottomWidthMm,
        double bottomThicknessMm,
        double topWidthMm,
        double topThicknessMm,
        UnderGalleryParameters? underGallery = null,
        double crownRatio = 0.25,
        double knifeEdgeApexRadiusMm = 0.10,
        double cathedralArchHeightMm = 2.0)
    {
        Validate(
            bottomWidthMm,
            bottomThicknessMm,
            topWidthMm,
            topThicknessMm,
            crownRatio,
            knifeEdgeApexRadiusMm,
            cathedralArchHeightMm,
            underGallery);

        ProfileType = profileType;
        _bottomWidthMm = bottomWidthMm;
        _bottomThicknessMm = bottomThicknessMm;
        _topWidthMm = topWidthMm;
        _topThicknessMm = topThicknessMm;
        UnderGallery = underGallery;
        _crownRatio = crownRatio;
        _knifeEdgeApexRadiusMm = knifeEdgeApexRadiusMm;
        _cathedralArchHeightMm = cathedralArchHeightMm;
    }

    /// <summary>
    /// The cross-section geometric profile type.
    /// </summary>
    public ShankProfileType ProfileType { get; init; } = ShankProfileType.ComfortFit;

    /// <summary>
    /// Width of the band at the base (bottom / palm side at theta = -pi/2) in millimeters.
    /// </summary>
    public double BottomWidthMm
    {
        get => _bottomWidthMm;
        init
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(BottomWidthMm),
                    value,
                    "Bottom width must be strictly positive.");
            }
            _bottomWidthMm = value;
        }
    }

    /// <summary>
    /// Thickness of the band at the base (bottom / palm side at theta = -pi/2) in millimeters.
    /// Must be strictly &gt;= 0.80 mm to satisfy jewelry structural invariants.
    /// </summary>
    public double BottomThicknessMm
    {
        get => _bottomThicknessMm;
        init
        {
            if (value < MinimumThicknessMm)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(BottomThicknessMm),
                    value,
                    $"Bottom thickness ({value:F2} mm) must be at least {MinimumThicknessMm:F2} mm.");
            }
            _bottomThicknessMm = value;
        }
    }

    /// <summary>
    /// Width of the band at the top (head / stone side at theta = pi/2) in millimeters.
    /// </summary>
    public double TopWidthMm
    {
        get => _topWidthMm;
        init
        {
            if (value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(TopWidthMm),
                    value,
                    "Top width must be strictly positive.");
            }
            _topWidthMm = value;
        }
    }

    /// <summary>
    /// Thickness of the band at the top (head / stone side at theta = pi/2) in millimeters.
    /// Must be strictly &gt;= 0.80 mm to satisfy jewelry structural invariants.
    /// </summary>
    public double TopThicknessMm
    {
        get => _topThicknessMm;
        init
        {
            if (value < MinimumThicknessMm)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(TopThicknessMm),
                    value,
                    $"Top thickness ({value:F2} mm) must be at least {MinimumThicknessMm:F2} mm.");
            }
            _topThicknessMm = value;
        }
    }

    /// <summary>
    /// Inner crowning depth ratio for ComfortFit profiles (v_in = CrownRatio * Thickness).
    /// Typically ranges from 0.15 to 0.35 (default 0.25).
    /// </summary>
    public double CrownRatio
    {
        get => _crownRatio;
        init
        {
            if (value < 0.0 || value >= 0.5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CrownRatio),
                    value,
                    "Crown ratio must be non-negative and strictly less than 0.5.");
            }
            _crownRatio = value;
        }
    }

    /// <summary>
    /// Apex fillet radius for KnifeEdge profiles in millimeters.
    /// </summary>
    public double KnifeEdgeApexRadiusMm
    {
        get => _knifeEdgeApexRadiusMm;
        init
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(KnifeEdgeApexRadiusMm),
                    value,
                    "Knife edge apex radius must be non-negative.");
            }
            _knifeEdgeApexRadiusMm = value;
        }
    }

    /// <summary>
    /// Shoulder arch elevation for Cathedral profiles in millimeters above the standard rail.
    /// </summary>
    public double CathedralArchHeightMm
    {
        get => _cathedralArchHeightMm;
        init
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CathedralArchHeightMm),
                    value,
                    "Cathedral arch height must be non-negative.");
            }
            _cathedralArchHeightMm = value;
        }
    }

    /// <summary>
    /// Optional under-gallery / azurador hollow chamber and openwork parameters.
    /// </summary>
    public UnderGalleryParameters? UnderGallery { get; init; }

    /// <summary>
    /// Indicates whether the shank dimensions taper between base and top.
    /// </summary>
    public bool IsTapered =>
        Math.Abs(BottomWidthMm - TopWidthMm) > 1e-4 ||
        Math.Abs(BottomThicknessMm - TopThicknessMm) > 1e-4;

    /// <summary>
    /// Validates all shank parameters against geometric and structural casting invariants.
    /// </summary>
    public static void Validate(
        double bottomWidthMm,
        double bottomThicknessMm,
        double topWidthMm,
        double topThicknessMm,
        double crownRatio,
        double knifeEdgeApexRadiusMm,
        double cathedralArchHeightMm,
        UnderGalleryParameters? underGallery)
    {
        if (bottomWidthMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bottomWidthMm),
                bottomWidthMm,
                "Bottom width must be strictly positive.");
        }

        if (bottomThicknessMm < MinimumThicknessMm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bottomThicknessMm),
                bottomThicknessMm,
                $"Bottom thickness ({bottomThicknessMm:F2} mm) must be at least {MinimumThicknessMm:F2} mm.");
        }

        if (topWidthMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topWidthMm),
                topWidthMm,
                "Top width must be strictly positive.");
        }

        if (topThicknessMm < MinimumThicknessMm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topThicknessMm),
                topThicknessMm,
                $"Top thickness ({topThicknessMm:F2} mm) must be at least {MinimumThicknessMm:F2} mm.");
        }

        if (crownRatio < 0.0 || crownRatio >= 0.5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(crownRatio),
                crownRatio,
                "Crown ratio must be non-negative and strictly less than 0.5.");
        }

        if (knifeEdgeApexRadiusMm < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(knifeEdgeApexRadiusMm),
                knifeEdgeApexRadiusMm,
                "Knife edge apex radius must be non-negative.");
        }

        if (cathedralArchHeightMm < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cathedralArchHeightMm),
                cathedralArchHeightMm,
                "Cathedral arch height must be non-negative.");
        }

        underGallery?.Validate();
    }

    /// <summary>
    /// Validates current instance properties.
    /// </summary>
    public void Validate()
    {
        Validate(
            BottomWidthMm,
            BottomThicknessMm,
            TopWidthMm,
            TopThicknessMm,
            CrownRatio,
            KnifeEdgeApexRadiusMm,
            CathedralArchHeightMm,
            UnderGallery);
    }
}
