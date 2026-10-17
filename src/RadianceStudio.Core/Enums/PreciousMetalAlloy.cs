namespace RadianceStudio.Core.Enums;

using System;
using System.Collections.Generic;

/// <summary>
/// Supported canonical jewelry casting alloys with standard metallurgical densities (g/cm³).
/// </summary>
public enum PreciousMetalAlloy
{
    Gold24K,
    Gold18KYellow,
    Gold18KWhite,
    Gold18KRose,
    Gold14KYellow,
    Gold14KWhite,
    Platinum950,
    Silver925,
    CastingWax
}

/// <summary>
/// Extension methods for retrieving physical metallurgical parameters of precious metal alloys.
/// </summary>
public static class PreciousMetalAlloyExtensions
{
    private static readonly Dictionary<PreciousMetalAlloy, double> Densities = new()
    {
        [PreciousMetalAlloy.Gold24K] = 19.32,
        [PreciousMetalAlloy.Gold18KYellow] = 15.58,
        [PreciousMetalAlloy.Gold18KWhite] = 15.90,
        [PreciousMetalAlloy.Gold18KRose] = 15.55,
        [PreciousMetalAlloy.Gold14KYellow] = 13.07,
        [PreciousMetalAlloy.Gold14KWhite] = 12.80,
        [PreciousMetalAlloy.Platinum950] = 21.45,
        [PreciousMetalAlloy.Silver925] = 10.36,
        [PreciousMetalAlloy.CastingWax] = 0.96
    };

    /// <summary>
    /// Gets the metallurgical density in grams per cubic centimeter (g/cm³).
    /// </summary>
    public static double GetDensityGPerCm3(this PreciousMetalAlloy alloy)
    {
        if (Densities.TryGetValue(alloy, out double density))
        {
            return density;
        }

        throw new ArgumentOutOfRangeException(nameof(alloy), alloy, "Unknown precious metal alloy.");
    }

    /// <summary>
    /// Computes the casting wax-to-metal mass multiplier factor (ratio of metal density to wax density).
    /// </summary>
    public static double GetWaxToMetalMultiplier(this PreciousMetalAlloy alloy)
    {
        double waxDensity = Densities[PreciousMetalAlloy.CastingWax];
        return alloy.GetDensityGPerCm3() / waxDensity;
    }
}
