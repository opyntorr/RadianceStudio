namespace RadianceStudio.Core.Tests.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Xunit;

public class HaloEngineTests
{
    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 6.5, 1.3, 0.2, 0.15)]
    [InlineData(GemCutType.Oval, 6.0, 1.2, 0.2, 0.15)]
    [InlineData(GemCutType.Cushion, 6.0, 1.3, 0.2, 0.15)]
    [InlineData(GemCutType.Pear, 7.0, 1.1, 0.25, 0.15)]
    [InlineData(GemCutType.Emerald, 7.0, 1.3, 0.2, 0.15)]
    [InlineData(GemCutType.Princess, 5.5, 1.2, 0.2, 0.15)]
    public void CalculateHaloMeleePlacements_NonCircularStones_CalculatesValidDistribution(
        GemCutType centerCut,
        double centerDiameterMm,
        double meleeDiameterMm,
        double guardDistanceMm,
        double spacingMm)
    {
        var gem = GemDimensions.GetDefault(centerCut, centerDiameterMm);
        var parameters = new HaloParameters
        {
            MeleeDiameterMm = meleeDiameterMm,
            GuardDistanceMm = guardDistanceMm,
            SpacingMm = spacingMm,
            DropHeightMm = -0.3
        };

        var placements = HaloEngine.CalculateHaloMeleePlacements(gem, parameters);

        Assert.NotNull(placements);
        Assert.True(placements.Count >= 8, $"Melee count {placements.Count} should be >= 8 for {centerDiameterMm} mm center stone.");

        // Check index continuity
        for (int i = 0; i < placements.Count; i++)
        {
            Assert.Equal(i, placements[i].Index);
            Assert.Equal(parameters.MeleeGemCut, placements[i].CutType);
            Assert.Equal(meleeDiameterMm, placements[i].DiameterMm);
            Assert.Equal(-0.3, placements[i].Position.Z, 3);
            Assert.True(placements[i].MicroProngPositions.Count > 0);
        }
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Cushion)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    public void HaloMelee_GuaranteesStrictNonCollision_WithCenterGem(GemCutType centerCut)
    {
        double centerDiameter = 6.0;
        var gem = GemDimensions.GetDefault(centerCut, centerDiameter);
        double guardDistance = 0.25;
        double meleeDiameter = 1.3;

        var parameters = new HaloParameters
        {
            MeleeDiameterMm = meleeDiameter,
            GuardDistanceMm = guardDistance,
            SpacingMm = 0.15
        };

        var placements = HaloEngine.CalculateHaloMeleePlacements(gem, parameters);
        var centerGemOutline = GemGirdleEvaluator.SampleGirdleOutline(centerCut, gem, 360);

        foreach (var p in placements)
        {
            var pos2D = new Point3D(p.Position.X, p.Position.Y, 0.0);
            double minDistanceToCenter = centerGemOutline.Min(pt => (pos2D - pt).Length);
            double clearance = minDistanceToCenter - (meleeDiameter * 0.5);

            Assert.True(clearance >= guardDistance - 0.05,
                $"Melee at index {p.Index} has clearance {clearance:F3} mm, which is less than guard distance {guardDistance:F3} mm.");
        }
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Cushion)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    public void CreateHaloPlatformMesh_ProducesWatertightClosedManifold(GemCutType cutType)
    {
        var gem = GemDimensions.GetDefault(cutType, 6.0);
        var parameters = new HaloParameters
        {
            MeleeDiameterMm = 1.3,
            GuardDistanceMm = 0.2,
            SpacingMm = 0.15,
            TrayWallThicknessMm = 0.6,
            TrayDepthMm = 1.0,
            DropHeightMm = -0.3
        };

        var mesh = HaloEngine.CreateHaloPlatformMesh(gem, parameters);

        Assert.NotNull(mesh);
        Assert.True(mesh.Vertices.Count > 0);
        Assert.True(mesh.Faces.Count > 0);
        AssertWatertight2Manifold(mesh);
    }

    [Fact]
    public void HaloParameters_Validation_RejectsInvalidValues()
    {
        var invalidMelee = new HaloParameters { MeleeDiameterMm = -1.0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidMelee.Validate());

        var invalidGuard = new HaloParameters { GuardDistanceMm = -0.1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidGuard.Validate());

        var invalidSpacing = new HaloParameters { SpacingMm = -0.5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidSpacing.Validate());
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
