namespace RadianceStudio.Core.Enums;

/// <summary>
/// Retention prong and border cutting styles for pavé stone settings.
/// </summary>
public enum PaveProngType
{
    /// <summary>
    /// Micro-grain spherical beads raised at interstitial centroids between stones.
    /// </summary>
    Bead = 0,

    /// <summary>
    /// Beveled mirror-finish side walls with retention beads.
    /// </summary>
    BrightCut = 1,

    /// <summary>
    /// French pavé scalloped U-notches along lateral edges.
    /// </summary>
    FishtailV = 2,

    /// <summary>
    /// Square crenellated notches along lateral setting walls.
    /// </summary>
    Castle = 3
}
