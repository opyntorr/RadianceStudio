namespace RadianceStudio.Core.Enums;

/// <summary>
/// Specialized planar and 3D curve profiles for ring rails.
/// </summary>
public enum RailProfileType
{
    /// <summary>
    /// Planar circle of diameter D_inner centered at origin on the XY plane.
    /// </summary>
    CanonicalCircle = 0,

    /// <summary>Alias for CanonicalCircle.</summary>
    Circle = CanonicalCircle,

    /// <summary>
    /// Anti-rotation Euro-shank profile featuring a circular upper arch, corner lobes
    /// at 225 deg and 315 deg, a flat bottom span, and normalized finger circumference.
    /// </summary>
    EuroShank = 1,

    /// <summary>Alias for EuroShank.</summary>
    Euro = EuroShank,

    /// <summary>
    /// Soft squircle rail modeled via Lamé superellipse (|x/a|^n + |y/b|^n = 1) with
    /// default exponent n = 3.0, perimeter-normalized to target finger circumference.
    /// </summary>
    CushionSquircle = 2,

    /// <summary>Alias for CushionSquircle.</summary>
    Cushion = CushionSquircle,

    /// <summary>Alias for CushionSquircle.</summary>
    Squircle = CushionSquircle,

    /// <summary>
    /// 3D open spiral/helical crossover curve with angular overlap, axial clearance,
    /// and radial clearance without self-intersection.
    /// </summary>
    BypassOverlap = 3,

    /// <summary>Alias for BypassOverlap.</summary>
    Bypass = BypassOverlap
}
