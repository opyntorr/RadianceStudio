namespace RadianceStudio.Core.Enums;

/// <summary>
/// Specifies the aesthetic and structural tip geometry for gem prongs.
/// </summary>
public enum ProngStyle
{
    /// <summary>
    /// Smooth hemispherical dome tip capping the prong cylinder.
    /// </summary>
    RoundDome,

    /// <summary>
    /// Truncated planar circular tip capping the prong cylinder.
    /// </summary>
    RoundFlat,

    /// <summary>
    /// Sharpened pointed claw tip wrapping over the crown facet.
    /// </summary>
    Claw,

    /// <summary>
    /// Angular chevron V-shaped profile wrapping corner facets (Princess corners, Marquise/Pear tips).
    /// </summary>
    VProng,

    /// <summary>
    /// Shared prong shared between adjacent stones.
    /// </summary>
    SharedProng
}
