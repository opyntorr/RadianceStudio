namespace RadianceStudio.Core.Tests;

using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;
using Xunit;

public class GemDimensionsTests
{
    [Fact]
    public void RoundBrilliant_DefaultProportions_MatchCanonicalSpecification()
    {
        // Arrange
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 1.0);

        // Assert
        Assert.Equal(1.0, gem.DiameterMm);
        Assert.Equal(1.0, gem.AspectRatio);
        Assert.Equal(56.0, gem.TableWidthPercent);
        Assert.Equal(14.5, gem.CrownHeightPercent);
        Assert.Equal(43.0, gem.PavilionDepthPercent);
        Assert.Equal(2.5, gem.GirdleThicknessPercent);
        Assert.Equal(60.0, gem.TotalDepthPercent);
        Assert.Equal(0.60, gem.GetTotalDepthMm(), 3);
    }

    [Theory]
    [InlineData(1.5, 0.90)]
    [InlineData(2.0, 1.20)]
    [InlineData(5.0, 3.00)]
    public void RoundBrilliant_ScalesLinearlyWithDiameter(double diameterMm, double expectedDepthMm)
    {
        // Arrange
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, diameterMm);

        // Act
        var depth = gem.GetTotalDepthMm();

        // Assert
        Assert.Equal(expectedDepthMm, depth, 3);
    }

    [Fact]
    public void NonSquareCuts_HaveCorrectAspectRatios()
    {
        // Assert
        Assert.True(GemDimensions.GetDefault(GemCutType.Oval).AspectRatio > 1.0);
        Assert.True(GemDimensions.GetDefault(GemCutType.Pear).AspectRatio > 1.0);
        Assert.True(GemDimensions.GetDefault(GemCutType.Marquise).AspectRatio > 1.5);
        Assert.Equal(1.0, GemDimensions.GetDefault(GemCutType.Princess).AspectRatio);
    }
}
