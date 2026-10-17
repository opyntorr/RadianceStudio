namespace RadianceStudio.Core.Tests.Meshes;

using System;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

public class CanonicalGemMeshTests
{
    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 57)]
    [InlineData(GemCutType.Princess, 49)]
    [InlineData(GemCutType.Oval, 57)]
    [InlineData(GemCutType.Pear, 58)]
    [InlineData(GemCutType.Emerald, 50)]
    [InlineData(GemCutType.Marquise, 57)]
    public void CanonicalMesh_HasExactFacetCount(GemCutType cutType, int expectedFacetCount)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);

        Assert.Equal(cutType, mesh.CutType);
        Assert.Equal(1.0, mesh.DiameterMm);
        Assert.Equal(expectedFacetCount, mesh.FacetCount);
        Assert.Equal(expectedFacetCount, mesh.Faces.Count);
        Assert.Equal(expectedFacetCount, mesh.Normals.Count);
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 1.0, 1.0, 0.60)]
    [InlineData(GemCutType.Princess, 1.0, 1.0, 0.73)]
    [InlineData(GemCutType.Oval, 1.0, 1.45, 0.603)]
    [InlineData(GemCutType.Pear, 1.0, 1.55, 0.61)]
    [InlineData(GemCutType.Emerald, 1.0, 1.40, 0.66)]
    [InlineData(GemCutType.Marquise, 1.0, 1.95, 0.60)]
    public void CanonicalMesh_MatchesProportionsAndBounds(
        GemCutType cutType,
        double expectedWidth,
        double expectedLength,
        double expectedDepth)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        var (min, max) = mesh.GetBounds();

        double width = max.X - min.X;
        double length = max.Y - min.Y;
        double depth = max.Z - min.Z;

        Assert.Equal(expectedWidth, width, 2);
        Assert.Equal(expectedLength, length, 2);
        Assert.Equal(expectedDepth, depth, 2);

        // Center check: Girdle plane centered at Z = 0
        Assert.True(min.Z < 0, "Culet must be at negative Z");
        Assert.True(max.Z > 0, "Table must be at positive Z");
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 2.0)]
    [InlineData(GemCutType.Princess, 2.5)]
    [InlineData(GemCutType.Oval, 3.5)]
    [InlineData(GemCutType.Pear, 1.8)]
    [InlineData(GemCutType.Emerald, 5.0)]
    [InlineData(GemCutType.Marquise, 4.0)]
    public void ScaledMesh_ScalesLinearlyWithDiameter(GemCutType cutType, double diameterMm)
    {
        var canonical = GemMeshFactory.CreateCanonical(cutType);
        var scaled = GemMeshFactory.Create(cutType, diameterMm);

        var (cMin, cMax) = canonical.GetBounds();
        var (sMin, sMax) = scaled.GetBounds();

        double cWidth = cMax.X - cMin.X;
        double sWidth = sMax.X - sMin.X;
        double cLength = cMax.Y - cMin.Y;
        double sLength = sMax.Y - sMin.Y;
        double cDepth = cMax.Z - cMin.Z;
        double sDepth = sMax.Z - sMin.Z;

        Assert.Equal(cWidth * diameterMm, sWidth, 3);
        Assert.Equal(cLength * diameterMm, sLength, 3);
        Assert.Equal(cDepth * diameterMm, sDepth, 3);
        Assert.Equal(canonical.FacetCount, scaled.FacetCount);
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    public void Mesh_NormalsAreUnitLengthAndTablePointsUp(GemCutType cutType)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);

        foreach (var normal in mesh.Normals)
        {
            Assert.Equal(1.0, normal.Length, 4);
        }

        // Table face is Face 0: normal must point straight up (+Z)
        var tableNormal = mesh.Normals[0];
        Assert.True(tableNormal.Z > 0.99, "Table normal must align with +Z");
        Assert.True(Math.Abs(tableNormal.X) < 1e-4);
        Assert.True(Math.Abs(tableNormal.Y) < 1e-4);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Generator_ThrowsOnInvalidDiameter(double invalidDiameter)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            GemMeshFactory.Create(GemCutType.RoundBrilliant, invalidDiameter));
    }
}
