namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Services;

/// <summary>
/// Line item in a jewelry Bill of Materials representing a cohort of gemstones with matching cut, material, and dimensions.
/// </summary>
public record GemBomLineItem
{
    /// <summary>
    /// The geometric cut type of the gemstone cohort.
    /// </summary>
    public GemCutType CutType { get; init; }

    /// <summary>
    /// The physical gem material and density profile.
    /// </summary>
    public CaratEstimator.GemMaterial GemMaterial { get; init; }

    /// <summary>
    /// Total quantity of stones in this cohort.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Average outer girdle diameter in millimeters.
    /// </summary>
    public double AverageDiameterMm { get; init; }

    /// <summary>
    /// Cumulative carat weight for all stones in this cohort.
    /// </summary>
    public double TotalCarats { get; init; }
}
