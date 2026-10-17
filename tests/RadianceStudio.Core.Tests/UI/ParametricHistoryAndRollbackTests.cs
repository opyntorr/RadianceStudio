namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Graph;
using Xunit;

/// <summary>
/// Tier 1, Tier 2 &amp; Tier 3 tests for Features 11, 12, 13:
/// Dual-Mode Parametric History, Draggable Rollback Bar, and Right-Click Suppress/Resume.
/// </summary>
public class ParametricHistoryAndRollbackTests
{
    [Fact]
    public void Feature11_DualModeParametricHistory_TogglesBetweenModes()
    {
        var graph = new ParametricGraphManager();

        // Default mode is StudioClassic
        Assert.Equal(ParametricMode.StudioClassic, graph.CurrentMode);

        // Toggle to FeatureTree
        graph.CurrentMode = ParametricMode.FeatureTree;
        Assert.Equal(ParametricMode.FeatureTree, graph.CurrentMode);

        // Toggle back to StudioClassic
        graph.CurrentMode = ParametricMode.StudioClassic;
        Assert.Equal(ParametricMode.StudioClassic, graph.CurrentMode);
    }

    [Fact]
    public void Feature11_FeatureTree_OrdersNodesChronologically()
    {
        var graph = new ParametricGraphManager();

        var node1 = new FeatureNode { Id = Guid.NewGuid(), Name = "RingRail", OrderIndex = 1 };
        var node2 = new FeatureNode { Id = Guid.NewGuid(), Name = "SweptShank", OrderIndex = 2 };
        var node3 = new FeatureNode { Id = Guid.NewGuid(), Name = "ProngHead", OrderIndex = 3 };

        // Register out of order
        graph.RegisterNode(node3);
        graph.RegisterNode(node1);
        graph.RegisterNode(node2);

        var active = graph.GetNodesInExecutionOrder();

        Assert.Equal(3, active.Count);
        Assert.Equal("RingRail", active[0].Name);
        Assert.Equal("SweptShank", active[1].Name);
        Assert.Equal("ProngHead", active[2].Name);
    }

    [Fact]
    public void Feature12_DraggableRollbackBar_FreezesDownstreamOperations()
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };

        var railNode = new FeatureNode { Id = Guid.NewGuid(), Name = "Rail", OrderIndex = 1 };
        var shankNode = new FeatureNode { Id = Guid.NewGuid(), Name = "Shank", OrderIndex = 2 };
        var gemNode = new FeatureNode { Id = Guid.NewGuid(), Name = "CenterGem", OrderIndex = 3 };
        var prongNode = new FeatureNode { Id = Guid.NewGuid(), Name = "Prongs", OrderIndex = 4 };
        var cutterNode = new FeatureNode { Id = Guid.NewGuid(), Name = "Cutters", OrderIndex = 5 };

        graph.RegisterNode(railNode);
        graph.RegisterNode(shankNode);
        graph.RegisterNode(gemNode);
        graph.RegisterNode(prongNode);
        graph.RegisterNode(cutterNode);

        var activeAll = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Equal(5, activeAll.Count);

        // Drag rollback bar to index 2 (after Shank)
        graph.RollbackIndex = 2;
        var rolledBack = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();

        Assert.Equal(2, rolledBack.Count);
        Assert.Equal("Rail", rolledBack[0].Name);
        Assert.Equal("Shank", rolledBack[1].Name);

        // Downstream features (CenterGem, Prongs, Cutters) are frozen
        Assert.DoesNotContain(rolledBack, n => n.Name == "CenterGem");
        Assert.DoesNotContain(rolledBack, n => n.Name == "Prongs");
        Assert.DoesNotContain(rolledBack, n => n.Name == "Cutters");

        // Drag rollback bar back to end (unfreeze all)
        graph.ResetRollbackToEnd();
        var unrolled = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Equal(5, unrolled.Count);
    }

    [Fact]
    public void Feature13_RightClickSuppressResume_TogglesIndividualFeatures()
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };

        var railId = Guid.NewGuid();
        var shankId = Guid.NewGuid();
        var haloId = Guid.NewGuid();

        graph.RegisterNode(new FeatureNode { Id = railId, Name = "Rail", OrderIndex = 1 });
        graph.RegisterNode(new FeatureNode { Id = shankId, Name = "Shank", OrderIndex = 2 });
        graph.RegisterNode(new FeatureNode { Id = haloId, Name = "Halo", OrderIndex = 3 });

        // Suppress Halo
        graph.SetSuppressed(haloId, true);

        var active = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Equal(2, active.Count);
        Assert.DoesNotContain(active, n => n.Id == haloId);

        // Resume Halo
        graph.SetSuppressed(haloId, false);
        var resumed = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Equal(3, resumed.Count);
        Assert.Contains(resumed, n => n.Id == haloId);
    }

    [Fact]
    public void Feature13_SuppressParentFeature_CascadesToDependentChildren()
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };

        var shankId = Guid.NewGuid();
        var gemRowId = Guid.NewGuid();
        var cutterId = Guid.NewGuid();

        var shankNode = new FeatureNode
        {
            Id = shankId,
            Name = "Shank",
            OrderIndex = 1,
            ChildIds = new List<Guid> { gemRowId }
        };

        var gemRowNode = new FeatureNode
        {
            Id = gemRowId,
            Name = "GemRow",
            OrderIndex = 2,
            ParentIds = new List<Guid> { shankId },
            ChildIds = new List<Guid> { cutterId }
        };

        var cutterNode = new FeatureNode
        {
            Id = cutterId,
            Name = "Cutters",
            OrderIndex = 3,
            ParentIds = new List<Guid> { gemRowId }
        };

        graph.RegisterNode(shankNode);
        graph.RegisterNode(gemRowNode);
        graph.RegisterNode(cutterNode);

        Assert.Equal(3, graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList().Count);

        // Suppressing the shank should cascade suppression downstream to GemRow and Cutters
        graph.SetSuppressed(shankId, true, cascadeToChildren: true);

        var active = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Empty(active);

        // Resuming shank without children
        graph.SetSuppressed(shankId, false, cascadeToChildren: false);
        var activeAfterResume = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.Single(activeAfterResume);
        Assert.Equal("Shank", activeAfterResume[0].Name);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(1000)]
    public void Feature12_RollbackIndex_HandlesBoundaryValuesGracefully(int boundaryIndex)
    {
        var graph = new ParametricGraphManager
        {
            CurrentMode = ParametricMode.FeatureTree
        };
        graph.RegisterNode(new FeatureNode { Id = Guid.NewGuid(), Name = "Feature1", OrderIndex = 1 });

        graph.RollbackIndex = boundaryIndex;

        var active = graph.GetNodesInExecutionOrder().Where(graph.ShouldEvaluateNode).ToList();
        Assert.NotNull(active);
    }
}
