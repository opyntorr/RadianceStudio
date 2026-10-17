namespace RadianceStudio.Core.Services;

using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
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

    /// <summary>
    /// Calculates full 3D placements and 4x4 affine transformation matrices along a space curve.
    /// </summary>
    IReadOnlyList<GemPlacement> CalculatePlacements(
        ICurve3D curve,
        ISurface3D? surface,
        GemCutType cutType,
        double gemSizeMm,
        double spacingMm,
        double startOffsetMm = 0,
        double endOffsetMm = 0,
        bool flipNormal = false,
        double rotationAngleDeg = 0);
}
