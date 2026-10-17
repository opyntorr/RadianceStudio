namespace RadianceStudio.Core.Tests;

using RadianceStudio.Core.Engines;
using Xunit;

public class RailEngineTests
{
    [Theory]
    [InlineData(0.0, 11.63)]
    [InlineData(6.0, 16.51)]
    [InlineData(7.0, 17.32)]
    [InlineData(8.0, 18.13)]
    public void UsSizeToInnerDiameterMm_MatchesInternationalStandard(double sizeUs, double expectedDiameterMm)
    {
        double diameter = RailEngine.UsSizeToInnerDiameterMm(sizeUs);
        Assert.Equal(expectedDiameterMm, diameter, 2);
    }
}
