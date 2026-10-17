namespace RadianceStudio.Core.Enums;

/// <summary>
/// Cross-section geometric profile types for parametric ring shanks.
/// </summary>
public enum ShankProfileType
{
    /// <summary>
    /// Crisp rectangular cross-section with flat inner, outer, and side faces.
    /// </summary>
    Flat = 0,

    /// <summary>
    /// Flat inner contact curve against the finger with an elliptical or arc-crowned outer surface.
    /// </summary>
    DShape = 1,

    /// <summary>
    /// Alias for DShape (traditional jewelry half-round band).
    /// </summary>
    HalfRound = DShape,

    /// <summary>
    /// Ergonomic jewelry profile with a crowned inner surface bulging toward finger center
    /// and a crowned outer surface for supreme wearability.
    /// </summary>
    ComfortFit = 2,

    /// <summary>
    /// Alias for ComfortFit.
    /// </summary>
    Comfort = ComfortFit,

    /// <summary>
    /// Central peaked ridge with dual sloping facets meeting at a filleted apex.
    /// </summary>
    KnifeEdge = 3,

    /// <summary>
    /// Alias for KnifeEdge.
    /// </summary>
    Knife = KnifeEdge,

    /// <summary>
    /// Elevated shoulder arch cross-section transitioning into the stone head junction.
    /// </summary>
    Cathedral = 4
}
