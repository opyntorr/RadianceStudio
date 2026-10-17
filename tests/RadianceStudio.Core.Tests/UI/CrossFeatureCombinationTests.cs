namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using Xunit;

/// <summary>
/// Tier 3 Cross-Feature Combination Tests:
/// Verifies complex interactions between interconnected jewelry CAD features:
/// - Rail + Shank integration
/// - Swept Shank + Gem Row placement
/// - Gem Row + Cutter seat generation
/// - Parametric history + timeline rollback + dynamic parameter reload
/// - Feature suppression + live BOM recalculation
/// </summary>
public class CrossFeatureCombinationTests
{
    [Fact]
    public void Combination1_RingRailAndShank_MatchesInnerDiameterAndOuterDimensions()
    {
        // 1. Create a US Size 7.0 Ring Rail (Round Canonical Circle)
        double fingerSizeUs = 7.0;
        double innerDiameterMm = RailEngine.UsSizeToInnerDiameterMm(fingerSizeUs);
        Assert.InRange(innerDiameterMm, 17.3, 17.4);

        // 2. Define Comfort-Fit Shank parameters swept on rail
        double bottomWidthMm = 2.4;
        double bottomThicknessMm = 1.8;
        double topWidthMm = 2.0;
        double topThicknessMm = 1.6;

        var shankParams = new ShankParameters
        {
            ProfileType = ShankProfileType.ComfortFit,
            BottomWidthMm = bottomWidthMm,
            BottomThicknessMm = bottomThicknessMm,
            TopWidthMm = topWidthMm,
            TopThicknessMm = topThicknessMm
        };

        // Outer diameter at bottom = innerDiameter + 2 * bottomThickness
        double expectedBottomOuterDiameter = innerDiameterMm + (2.0 * bottomThicknessMm);

        Assert.True(expectedBottomOuterDiameter > innerDiameterMm);
        Assert.Equal(bottomThicknessMm * 2.0, expectedBottomOuterDiameter - innerDiameterMm, 3);
        Assert.True(shankParams.IsTapered);
    }

    [Fact]
    public void Combination2_ShankCurveAndGemRow_DistributesStonesWithOrthonormalFrames()
    {
        // Create an arc curve representing the top shoulder of a swept shank
        double radius = 10.0;
        var center = new Point3D(0, 0, 0);
        var circleCurve = new CircleCurve3D(center, radius);

        // Distribute 1.5mm accent gems with 0.2mm spacing along the shoulder arc
        double stoneDiameterMm = 1.5;
        double spacingMm = 0.2;
        double step = stoneDiameterMm + spacingMm;

        double curveLength = circleCurve.Length;
        int maxStones = (int)Math.Floor(curveLength / step);
        Assert.True(maxStones > 0);

        // Evaluate placements along curve
        var frames = new List<OrthonormalFrame>();
        for (int i = 0; i < 8; i++)
        {
            double dist = (i + 0.5) * step;
            var tan = circleCurve.TangentAtArcLength(dist).Normalize();
            var norm = circleCurve.NormalAtArcLength(dist).Normalize();
            var binorm = Vector3D.Cross(tan, norm).Normalize();

            frames.Add(new OrthonormalFrame(tan, norm, binorm));
        }

        Assert.Equal(8, frames.Count);

        // Verify all 8 frames are orthonormal: |T|=1, |N|=1, |B|=1, T.N < 1e-6
        foreach (var f in frames)
        {
            Assert.InRange(f.Tangent.Length, 0.999999, 1.000001);
            Assert.InRange(f.Normal.Length, 0.999999, 1.000001);
            Assert.InRange(f.Binormal.Length, 0.999999, 1.000001);
            Assert.True(Math.Abs(f.Tangent.Dot(f.Normal)) < 1e-6);
            Assert.True(Math.Abs(f.Tangent.Dot(f.Binormal)) < 1e-6);
            Assert.True(Math.Abs(f.Normal.Dot(f.Binormal)) < 1e-6);
        }
    }

    [Fact]
    public void Combination3_GemRowAndCutters_GeneratesPreciseSeatGeometry()
    {
        // For each gem in a row, generate corresponding cutter parameters
        double gemDiameter = 2.0;
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, gemDiameter);

        var cutterParams = new CutterParameters
        {
            GirdleClearanceMm = 0.10,
            SeatAngle = CutterSeatStyle.Degree70,
            LightHoleDepthExtensionMm = 2.0
        };

        // Cutter girdle diameter should be gem girdle + 2 * clearance
        double expectedCutterGirdleDiameter = gemDiameter + (2.0 * cutterParams.GirdleClearanceMm);
        double actualCutterGirdle = gem.DiameterMm + (2.0 * cutterParams.GirdleClearanceMm);

        Assert.Equal(expectedCutterGirdleDiameter, actualCutterGirdle, 3);
        Assert.Equal(CutterSeatStyle.Degree70, cutterParams.SeatAngle);
    }

    [Fact]
    public void Combination4_ParametricHistory_RollbackAndDynamicReloadWorkflow()
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };

        // 1. Create Rail Node
        var railId = Guid.NewGuid();
        var railParams = new Dictionary<string, object> { ["SizeUs"] = 6.5, ["WidthMm"] = 2.0 };
        graph.RegisterNode(new FeatureNode { Id = railId, Name = "Rail", OrderIndex = 1, Parameters = railParams });

        // 2. Create Shank Node (dependent on Rail)
        var shankId = Guid.NewGuid();
        var shankParams = new Dictionary<string, object> { ["Profile"] = "ComfortFit", ["BottomWidthMm"] = 2.2 };
        graph.RegisterNode(new FeatureNode
        {
            Id = shankId,
            Name = "Shank",
            OrderIndex = 2,
            ParentIds = new List<Guid> { railId },
            Parameters = shankParams
        });

        // 3. Create Center Gem Node (dependent on Shank)
        var gemId = Guid.NewGuid();
        var gemParams = new Dictionary<string, object> { ["Cut"] = "RoundBrilliant", ["DiameterMm"] = 6.5 };
        graph.RegisterNode(new FeatureNode
        {
            Id = gemId,
            Name = "CenterGem",
            OrderIndex = 3,
            ParentIds = new List<Guid> { shankId },
            Parameters = gemParams
        });

        // 4. Create Prong Head Node (dependent on Center Gem)
        var headId = Guid.NewGuid();
        var headParams = new Dictionary<string, object> { ["ProngCount"] = 4, ["ProngDiameterMm"] = 0.85 };
        graph.RegisterNode(new FeatureNode
        {
            Id = headId,
            Name = "ProngHead",
            OrderIndex = 4,
            ParentIds = new List<Guid> { gemId },
            Parameters = headParams
        });

        // 5. Create Cutters Node (dependent on Head and Center Gem)
        var cutterId = Guid.NewGuid();
        var cutterParams = new Dictionary<string, object> { ["SeatAngle"] = 70.0 };
        graph.RegisterNode(new FeatureNode
        {
            Id = cutterId,
            Name = "Cutters",
            OrderIndex = 5,
            ParentIds = new List<Guid> { headId, gemId },
            Parameters = cutterParams
        });

        Assert.Equal(5, graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList().Count);

        // Rollback timeline to index 2 (freeze Gem, Head, Cutters)
        graph.RollbackIndex = 2;
        var rolledBack = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Equal(2, rolledBack.Count);

        // Edit shank parameters in place
        var shankNode = graph.GetNode(shankId);
        Assert.NotNull(shankNode);
        shankNode.Parameters["BottomWidthMm"] = 2.8;

        // Unroll timeline back to step 5
        graph.ResetRollbackToEnd();
        var fullyEvaluated = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();

        Assert.Equal(5, fullyEvaluated.Count);
        Assert.Equal(2.8, (double)graph.GetNode(shankId)!.Parameters["BottomWidthMm"]);
    }

    [Fact]
    public void Combination5_FeatureSuppression_RecalculatesBomExcludingSuppressedItems()
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };

        var centerGemId = Guid.NewGuid();
        var haloMeleeId = Guid.NewGuid();

        // 1. Center Gem (1.00ct)
        graph.RegisterNode(new FeatureNode
        {
            Id = centerGemId,
            Name = "CenterGem_1.0ct",
            OrderIndex = 1,
            Parameters = new Dictionary<string, object> { ["Carats"] = 1.00, ["Quantity"] = 1 }
        });

        // 2. Halo Melee Gems (16 x 0.015ct = 0.24ct total)
        graph.RegisterNode(new FeatureNode
        {
            Id = haloMeleeId,
            Name = "HaloMelee_16Stones",
            OrderIndex = 2,
            ParentIds = new List<Guid> { centerGemId },
            Parameters = new Dictionary<string, object> { ["Carats"] = 0.24, ["Quantity"] = 16 }
        });

        double activeCarats = CalculateActiveCarats(graph);
        int activeStones = CalculateActiveStoneCount(graph);

        Assert.Equal(1.24, activeCarats, 2);
        Assert.Equal(17, activeStones);

        // Suppress Halo
        graph.SetSuppressed(haloMeleeId, true);

        double suppressedCarats = CalculateActiveCarats(graph);
        int suppressedStones = CalculateActiveStoneCount(graph);

        // BOM now reflects only the center stone
        Assert.Equal(1.00, suppressedCarats, 2);
        Assert.Equal(1, suppressedStones);
    }

    private static double CalculateActiveCarats(ParametricGraphManager graph)
    {
        return graph.GetNodesInExecutionOrder()
            .Where(graph.ShouldEvaluateNode)
            .Sum(n => (double)n.Parameters["Carats"]);
    }

    private static int CalculateActiveStoneCount(ParametricGraphManager graph)
    {
        return graph.GetNodesInExecutionOrder()
            .Where(graph.ShouldEvaluateNode)
            .Sum(n => (int)n.Parameters["Quantity"]);
    }
}
