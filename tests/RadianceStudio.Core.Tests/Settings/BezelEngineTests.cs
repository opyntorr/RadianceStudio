namespace RadianceStudio.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;
using Xunit;

public class BezelEngineTests
{
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
    public void CreateBezelMesh_SupportsAll13GemCuts_AndGeneratesWatertightManifold(GemCutType cutType)
    {
        var gem = GemDimensions.GetDefault(cutType, 5.0);
        var parameters = new BezelParameters
        {
            WallThicknessMm = 0.8,
            HeightAboveGirdleMm = 0.4,
            SeatDepthPercent = 30.0,
            DraftAngleDeg = 5.0,
            LightHoleDiameterRatio = 0.5,
            IsSemiBezel = false
        };

        var mesh = BezelEngine.CreateBezelMesh(gem, parameters);

        Assert.NotNull(mesh);
        Assert.Equal(cutType, mesh.CutType);
        Assert.True(mesh.Vertices.Count > 0);
        Assert.True(mesh.Faces.Count > 0);
        AssertWatertight2Manifold(mesh);
    }

    [Theory]
    [InlineData(0.4)]
    [InlineData(0.6)]
    [InlineData(0.8)]
    [InlineData(1.2)]
    public void Bezel_WallThicknessInvariant_EnforcesMinimumThickness(double wallThicknessMm)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 4.0);
        var parameters = new BezelParameters
        {
            WallThicknessMm = wallThicknessMm
        };

        var mesh = BezelEngine.CreateBezelMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);

        // At girdle plane Z = 0:
        // Inner radius = 2.0 mm, Outer radius >= 2.0 + wallThicknessMm
        var outerVerticesAtGirdle = mesh.Vertices
            .Where(v => Math.Abs(v.Z) < 0.05)
            .Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y))
            .ToList();

        double minOuterRadius = outerVerticesAtGirdle.Max();
        double innerRadius = gem.DiameterMm * 0.5;
        double observedThickness = minOuterRadius - innerRadius;

        Assert.True(observedThickness >= wallThicknessMm - 1e-3,
            $"Observed wall thickness {observedThickness:F3} mm must be >= specified thickness {wallThicknessMm:F3} mm.");
    }

    [Fact]
    public void BezelParameters_WallThicknessBelow04_ThrowsValidationException()
    {
        var invalidParams = new BezelParameters
        {
            WallThicknessMm = 0.35 // strictly < 0.4 mm casting minimum
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => invalidParams.Validate());
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.5)]
    [InlineData(0.7)]
    public void Bezel_LightHoleDiameterRatio_GeneratesExactBaseOpening(double ratio)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 6.0);
        var parameters = new BezelParameters
        {
            WallThicknessMm = 0.8,
            LightHoleDiameterRatio = ratio
        };

        var mesh = BezelEngine.CreateBezelMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);

        double zMin = mesh.Vertices.Min(v => v.Z);
        var baseInnerVertices = mesh.Vertices
            .Where(v => Math.Abs(v.Z - zMin) < 0.05)
            .Select(v => Math.Sqrt(v.X * v.X + v.Y * v.Y))
            .ToList();

        double minHoleRadius = baseInnerVertices.Min();
        double expectedHoleRadius = (gem.DiameterMm * 0.5) * ratio;

        Assert.Equal(expectedHoleRadius, minHoleRadius, 2);
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Princess)]
    public void SemiBezel_CreatesOpposingOpenings_WhileMaintainingManifoldTopology(GemCutType cutType)
    {
        var gem = GemDimensions.GetDefault(cutType, 5.0);
        var parameters = new BezelParameters
        {
            WallThicknessMm = 0.8,
            HeightAboveGirdleMm = 0.5,
            IsSemiBezel = true,
            SemiBezelOpeningAngleDeg = 60.0
        };

        var mesh = BezelEngine.CreateBezelMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);

        // In opening sector (around angle 0), top Z is lowered towards seat level
        var openingVertices = mesh.Vertices
            .Where(v => Math.Abs(v.Y) < 0.3 && v.X > 0)
            .ToList();

        var retainingVertices = mesh.Vertices
            .Where(v => Math.Abs(v.X) < 0.3 && v.Y > 0)
            .ToList();

        double maxZOpening = openingVertices.Max(v => v.Z);
        double maxZRetaining = retainingVertices.Max(v => v.Z);

        Assert.True(maxZOpening < maxZRetaining,
            $"Semi-bezel opening top Z ({maxZOpening:F3}) must be lower than retaining top Z ({maxZRetaining:F3}).");
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
