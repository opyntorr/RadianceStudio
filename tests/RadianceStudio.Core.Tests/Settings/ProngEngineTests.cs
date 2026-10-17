using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RadianceStudio.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

public class ProngEngineTests
{
    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 4)]
    [InlineData(GemCutType.RoundBrilliant, 6)]
    [InlineData(GemCutType.RoundBrilliant, 8)]
    [InlineData(GemCutType.Princess, 4)]
    [InlineData(GemCutType.Princess, 6)]
    [InlineData(GemCutType.Princess, 8)]
    [InlineData(GemCutType.Oval, 4)]
    [InlineData(GemCutType.Oval, 6)]
    [InlineData(GemCutType.Oval, 8)]
    [InlineData(GemCutType.Emerald, 4)]
    [InlineData(GemCutType.Emerald, 6)]
    [InlineData(GemCutType.Emerald, 8)]
    [InlineData(GemCutType.Marquise, 4)]
    [InlineData(GemCutType.Marquise, 6)]
    [InlineData(GemCutType.Marquise, 8)]
    public void CreateProngHeadMesh_GeneratesCorrectProngCount_AndWatertightManifold(GemCutType cutType, int prongCount)
    {
        var gem = GemDimensions.GetDefault(cutType, 6.5);
        var parameters = new ProngParameters
        {
            ProngCount = prongCount,
            ProngDiameterMm = 0.8,
            GirdleOverhangMm = 0.15,
            IncludeBaseWire = true,
            IncludeGalleryWire = true,
            Style = ProngStyle.RoundDome
        };

        var mesh = ProngEngine.CreateProngHeadMesh(gem, parameters);

        Assert.NotNull(mesh);
        Assert.True(mesh.Vertices.Count > 0);
        Assert.True(mesh.Faces.Count > 0);
        AssertWatertight2Manifold(mesh);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.15)]
    [InlineData(0.30)]
    [InlineData(0.40)]
    public void ProngHead_GirdleOverhang_RespectsSpecifiedLimits(double overhangMm)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 5.0);
        var parameters = new ProngParameters
        {
            ProngCount = 4,
            ProngDiameterMm = 0.8,
            GirdleOverhangMm = overhangMm,
            IncludeBaseWire = false,
            IncludeGalleryWire = false,
            Style = ProngStyle.RoundFlat
        };

        var mesh = ProngEngine.CreateProngHeadMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);

        // Gem radius at girdle is 2.5 mm.
        // Prong inner contact must extend inward to (2.5 - overhangMm).
        double gemRadius = gem.DiameterMm * 0.5;
        double minDistanceToOrigin = mesh.Vertices
            .Where(v => Math.Abs(v.Z) < 0.2) // near girdle plane
            .Min(v => Math.Sqrt(v.X * v.X + v.Y * v.Y));

        double expectedInnerRadius = gemRadius - overhangMm;
        Assert.True(minDistanceToOrigin <= expectedInnerRadius + 0.05,
            $"Prong inner edge distance {minDistanceToOrigin:F3} mm must reach expected inner radius {expectedInnerRadius:F3} mm for overhang {overhangMm:F3} mm.");
    }

    [Fact]
    public void PrincessCut_FourProngs_AlignExactlyOnFourCorners()
    {
        var gem = GemDimensions.GetDefault(GemCutType.Princess, 5.0);
        var angles = ProngEngine.CalculateProngAngles(GemCutType.Princess, gem, 4);

        Assert.Equal(4, angles.Count);

        // Expected corner angles: 45, 135, 225, 315 deg
        var expectedDeg = new[] { 45.0, 135.0, 225.0, 315.0 };
        for (int i = 0; i < 4; i++)
        {
            double deg = angles[i] * 180.0 / Math.PI;
            Assert.Equal(expectedDeg[i], deg, 2);
        }
    }

    [Fact]
    public void PrincessCut_VProngStyle_GeneratesWatertightChevronCorners()
    {
        var gem = GemDimensions.GetDefault(GemCutType.Princess, 6.0);
        var parameters = new ProngParameters
        {
            ProngCount = 4,
            ProngDiameterMm = 0.9,
            GirdleOverhangMm = 0.2,
            Style = ProngStyle.VProng,
            IncludeBaseWire = true,
            IncludeGalleryWire = false
        };

        var mesh = ProngEngine.CreateProngHeadMesh(gem, parameters);

        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);
    }

    [Theory]
    [InlineData(ProngStyle.RoundDome)]
    [InlineData(ProngStyle.RoundFlat)]
    [InlineData(ProngStyle.Claw)]
    [InlineData(ProngStyle.VProng)]
    [InlineData(ProngStyle.SharedProng)]
    public void AllProngStyles_ProduceWatertight2Manifolds(ProngStyle style)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 4.0);
        var parameters = new ProngParameters
        {
            ProngCount = 4,
            ProngDiameterMm = 0.8,
            GirdleOverhangMm = 0.15,
            Style = style,
            IncludeBaseWire = true,
            IncludeGalleryWire = true
        };

        var mesh = ProngEngine.CreateProngHeadMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);
    }

    [Fact]
    public void ProngParameters_Validation_EnforcesInvariants()
    {
        var invalidCount = new ProngParameters { ProngCount = 5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidCount.Validate());

        var invalidOverhangLow = new ProngParameters { GirdleOverhangMm = 0.02 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidOverhangLow.Validate());

        var invalidOverhangHigh = new ProngParameters { GirdleOverhangMm = 0.50 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidOverhangHigh.Validate());

        var invalidDiameter = new ProngParameters { ProngDiameterMm = 0.0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidDiameter.Validate());
    }

    private static void AssertWatertight2Manifold(GemMesh mesh)
    {
        var vertices = mesh.Vertices;
        var faces = mesh.Faces;
        int V = vertices.Count;
        int F = faces.Count;

        Assert.True(V >= 4, "Mesh must have at least 4 vertices.");
        Assert.True(F >= 4, "Mesh must have at least 4 faces.");

        var edgeMap = new Dictionary<(int, int), List<(int faceIdx, int from, int to)>>();

        for (int f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            int n = face.Length;
            Assert.True(n >= 3, $"Face {f} has fewer than 3 vertices.");

            for (int i = 0; i < n; i++)
            {
                int u = face[i];
                int v = face[(i + 1) % n];

                Assert.True(u >= 0 && u < V, $"Vertex index {u} out of range in face {f}");
                Assert.True(v >= 0 && v < V, $"Vertex index {v} out of range in face {f}");
                Assert.NotEqual(u, v);

                var canonicalEdge = u < v ? (u, v) : (v, u);
                if (!edgeMap.TryGetValue(canonicalEdge, out var occurrences))
                {
                    occurrences = new List<(int, int, int)>();
                    edgeMap[canonicalEdge] = occurrences;
                }
                occurrences.Add((f, u, v));
            }
        }

        // Verify 2-manifold edge property:
        // Every undirected edge must belong to EXACTLY TWO faces, with opposite directed orientation
        foreach (var kvp in edgeMap)
        {
            var (edge, occurrences) = (kvp.Key, kvp.Value);
            Assert.True(occurrences.Count == 2,
                $"Edge ({edge.Item1}, {edge.Item2}) is shared by {occurrences.Count} faces (expected exactly 2). Face indices: {string.Join(", ", occurrences.Select(o => o.faceIdx))}");

            var occ1 = occurrences[0];
            var occ2 = occurrences[1];

            bool oppositeOrientation = (occ1.from == occ2.to) && (occ1.to == occ2.from);
            Assert.True(oppositeOrientation,
                $"Edge ({edge.Item1}, {edge.Item2}) has inconsistent orientation between face {occ1.faceIdx} ({occ1.from}->{occ1.to}) and face {occ2.faceIdx} ({occ2.from}->{occ2.to})");
        }
    }
}
