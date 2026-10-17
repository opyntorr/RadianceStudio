namespace RadianceStudio.Core.Services;

using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;

/// <summary>
/// Contract for calculating gem placements along curves and surface references.
/// </summary>
public interface IGemEngine
{
    /// <summary>
    /// Calculates the maximum number of stones that fit on a curve given usable length, gem diameter and spacing.
    /// </summary>
    int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm);

    /// <summary>
    /// Calculates arc-length distances for each stone center along a curve.
    /// </summary>
    IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm);
}
