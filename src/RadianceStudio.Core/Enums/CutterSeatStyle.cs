namespace RadianceStudio.Core.Enums;

/// <summary>
/// Specifies the geometric profile and angle for procedural cutter bearing seats.
/// </summary>
public enum CutterSeatStyle
{
    /// <summary>
    /// Standard 70-degree bearing burr profile (35-degree half angle from axis).
    /// </summary>
    Degree70,

    /// <summary>
    /// Standard 90-degree bearing or straight burr profile (45-degree half angle from axis).
    /// </summary>
    Degree90,

    /// <summary>
    /// Planar horizontal shelf bearing step.
    /// </summary>
    FlatShelf,

    /// <summary>
    /// Conical bearing seat matching the gem's pavilion angle with facet clearance.
    /// </summary>
    Conical
}
