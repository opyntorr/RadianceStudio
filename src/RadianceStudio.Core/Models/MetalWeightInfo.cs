namespace RadianceStudio.Core.Models;

using System;
using RadianceStudio.Core.Enums;

/// <summary>
/// Detailed metallurgical weight breakdown and commercial cost estimate for a cast alloy.
/// </summary>
public record MetalWeightInfo
{
    public PreciousMetalAlloy Alloy { get; init; }
    public double DensityGPerCm3 { get; init; }
    public double VolumeMm3 { get; init; }
    public double WeightGrams { get; init; }
    public double WeightDwt { get; init; }
    public double WeightTroyOunces { get; init; }
    public double EstimatedCost { get; init; }

    /// <summary>
    /// Computes alloy weight conversions and cost based on solid volume in mm³ and metal density.
    /// </summary>
    /// <param name="alloy">The target precious metal alloy.</param>
    /// <param name="volumeMm3">The solid volume in cubic millimeters.</param>
    /// <param name="spotPricePerGram">Optional market spot price per gram in currency units.</param>
    public static MetalWeightInfo FromVolume(PreciousMetalAlloy alloy, double volumeMm3, double spotPricePerGram = 0.0)
    {
        if (volumeMm3 < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(volumeMm3), "Volume cannot be negative.");
        }

        double density = alloy.GetDensityGPerCm3();
        // Mass (g) = Volume (mm³) * Density (g/cm³) / 1000 (mm³/cm³)
        double grams = (volumeMm3 * density) / 1000.0;
        // 1 pennyweight (dwt) = 1.55517384 grams
        double dwt = grams / 1.55517384;
        // 1 troy ounce (oz t) = 31.1034768 grams
        double troyOz = grams / 31.1034768;
        double cost = grams * spotPricePerGram;

        return new MetalWeightInfo
        {
            Alloy = alloy,
            DensityGPerCm3 = density,
            VolumeMm3 = volumeMm3,
            WeightGrams = grams,
            WeightDwt = dwt,
            WeightTroyOunces = troyOz,
            EstimatedCost = cost
        };
    }
}
