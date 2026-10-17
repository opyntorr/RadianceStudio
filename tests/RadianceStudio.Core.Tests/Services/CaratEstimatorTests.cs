namespace RadianceStudio.Core.Tests.Services;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

public class CaratEstimatorTests
{
    private static readonly GemCutType[] All13Cuts = Enum.GetValues<GemCutType>();

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    [InlineData(GemCutType.Baguette)]
    [InlineData(GemCutType.Cushion)]
    [InlineData(GemCutType.Asscher)]
    [InlineData(GemCutType.Radiant)]
    [InlineData(GemCutType.Heart)]
    [InlineData(GemCutType.Trilliant)]
    [InlineData(GemCutType.TaperedBaguette)]
    public void ComputeVolumeMm3_IsStrictlyPositive_ForAll13Cuts(GemCutType cutType)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        double volume = CaratEstimator.ComputeVolumeMm3(mesh);

        Assert.True(volume > 0.0, $"Volume for {cutType} must be strictly positive, but was {volume}");
        Assert.False(double.IsNaN(volume));
        Assert.False(double.IsInfinity(volume));
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 2.0)]
    [InlineData(GemCutType.Princess, 3.0)]
    [InlineData(GemCutType.Oval, 2.5)]
    [InlineData(GemCutType.Pear, 1.8)]
    [InlineData(GemCutType.Emerald, 4.0)]
    [InlineData(GemCutType.Marquise, 3.5)]
    [InlineData(GemCutType.Baguette, 2.2)]
    [InlineData(GemCutType.Cushion, 3.0)]
    [InlineData(GemCutType.Asscher, 2.8)]
    [InlineData(GemCutType.Radiant, 3.2)]
    [InlineData(GemCutType.Heart, 2.4)]
    [InlineData(GemCutType.Trilliant, 2.6)]
    [InlineData(GemCutType.TaperedBaguette, 2.0)]
    public void ComputeVolumeMm3_ScalesAsCubicDiameter(GemCutType cutType, double diameterMm)
    {
        var canonical = GemMeshFactory.CreateCanonical(cutType);
        var scaled = GemMeshFactory.Create(cutType, diameterMm);

        double vCanonical = CaratEstimator.ComputeVolumeMm3(canonical);
        double vScaled = CaratEstimator.ComputeVolumeMm3(scaled);

        double expectedRatio = diameterMm * diameterMm * diameterMm;
        double actualRatio = vScaled / vCanonical;

        // Verify volume scales as d^3 within 0.1% tolerance
        Assert.True(Math.Abs(actualRatio - expectedRatio) / expectedRatio < 1e-3,
            $"Expected ratio {expectedRatio}, but got {actualRatio} for cut {cutType} at scale {diameterMm}");
    }

    [Fact]
    public void EstimateCarats_RoundBrilliant_6Point5MmDiamond_IsApproximatelyOneCarat()
    {
        // A standard 6.5 mm round brilliant diamond is nominally 1.0 carat in diamond trade
        var mesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, 6.5);
        double carats = CaratEstimator.EstimateCarats(mesh, CaratEstimator.GemMaterial.Diamond);

        // Verify within ±30% tolerance (0.70 to 1.30 carats)
        Assert.InRange(carats, 0.70, 1.30);
    }

    [Fact]
    public void EstimateCarats_AllFiveMaterials_ReturnDistinctCaratsForSameMesh()
    {
        var mesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, 6.0);
        var materials = Enum.GetValues<CaratEstimator.GemMaterial>();
        var results = new Dictionary<CaratEstimator.GemMaterial, double>();

        foreach (var material in materials)
        {
            double carats = CaratEstimator.EstimateCarats(mesh, material);
            Assert.True(carats > 0.0);
            results[material] = carats;
        }

        // Verify all 5 values are strictly distinct
        var caratValues = new HashSet<double>();
        foreach (var (mat, carats) in results)
        {
            Assert.DoesNotContain(carats, caratValues);
            caratValues.Add(carats);
        }

        // Densities order: Emerald (2.72) < Moissanite (3.22) < Diamond (3.52) < Sapphire (4.00) < CubicZirconia (5.80)
        Assert.True(results[CaratEstimator.GemMaterial.Emerald] < results[CaratEstimator.GemMaterial.Moissanite]);
        Assert.True(results[CaratEstimator.GemMaterial.Moissanite] < results[CaratEstimator.GemMaterial.Diamond]);
        Assert.True(results[CaratEstimator.GemMaterial.Diamond] < results[CaratEstimator.GemMaterial.Sapphire]);
        Assert.True(results[CaratEstimator.GemMaterial.Sapphire] < results[CaratEstimator.GemMaterial.CubicZirconia]);
    }

    [Theory]
    [InlineData(CaratEstimator.GemMaterial.Diamond, 3.52)]
    [InlineData(CaratEstimator.GemMaterial.Sapphire, 4.00)]
    [InlineData(CaratEstimator.GemMaterial.Emerald, 2.72)]
    [InlineData(CaratEstimator.GemMaterial.Moissanite, 3.22)]
    [InlineData(CaratEstimator.GemMaterial.CubicZirconia, 5.80)]
    public void GetDensity_ReturnsExpectedSpecificGravity(CaratEstimator.GemMaterial material, double expectedDensity)
    {
        Assert.Equal(expectedDensity, CaratEstimator.GetDensity(material));
    }

    [Fact]
    public void Methods_ThrowOnNullMesh()
    {
        Assert.Throws<ArgumentNullException>(() => CaratEstimator.ComputeVolumeMm3(null!));
        Assert.Throws<ArgumentNullException>(() => CaratEstimator.EstimateCarats(null!));
    }
}
