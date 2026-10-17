namespace RadianceStudio.Core.Tests.Graph;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Graph;
using Xunit;

public class ParametricGraphManagerTests
{
    [Fact]
    public void DefaultMode_IsStudioClassic()
    {
        var manager = new ParametricGraphManager();
        Assert.Equal(ParametricMode.StudioClassic, manager.CurrentMode);
        Assert.Equal(int.MaxValue, manager.RollbackIndex);
    }

    [Fact]
    public void StudioClassic_EvaluatesAllNodesRegardlessOfOrderOrSuppression()
    {
        var manager = new ParametricGraphManager { CurrentMode = ParametricMode.StudioClassic };

        var node1 = new FeatureNode { Name = "Rail", OrderIndex = 1 };
        var node2 = new FeatureNode { Name = "Shank", OrderIndex = 2, IsSuppressed = true };
        var node3 = new FeatureNode { Name = "Gems", OrderIndex = 3 };

        manager.RegisterNode(node1);
        manager.RegisterNode(node2);
        manager.RegisterNode(node3);

        manager.RollbackIndex = 1; // Even with rollback set to 1, classic evaluates everything

        Assert.True(manager.ShouldEvaluateNode(node1));
        Assert.True(manager.ShouldEvaluateNode(node2));
        Assert.True(manager.ShouldEvaluateNode(node3));
    }

    [Fact]
    public void FeatureTreeMode_RespectsRollbackBar()
    {
        var manager = new ParametricGraphManager { CurrentMode = ParametricMode.FeatureTree };

        var rail = new FeatureNode { Name = "Rail", OrderIndex = 1 };
        var shank = new FeatureNode { Name = "Shank", OrderIndex = 2 };
        var gems = new FeatureNode { Name = "Gems", OrderIndex = 3 };
        var cutters = new FeatureNode { Name = "Cutters", OrderIndex = 4 };

        manager.RegisterNode(rail);
        manager.RegisterNode(shank);
        manager.RegisterNode(gems);
        manager.RegisterNode(cutters);

        // Rollback above gems (freeze cutters and gems)
        manager.RollbackIndex = 2;

        Assert.True(manager.ShouldEvaluateNode(rail));
        Assert.True(manager.ShouldEvaluateNode(shank));
        Assert.False(manager.ShouldEvaluateNode(gems));
        Assert.False(manager.ShouldEvaluateNode(cutters));
    }

    [Fact]
    public void FeatureTreeMode_SuppressionCascadesToChildren()
    {
        var manager = new ParametricGraphManager { CurrentMode = ParametricMode.FeatureTree };

        var rail = new FeatureNode { Name = "Rail", OrderIndex = 1 };
        manager.RegisterNode(rail);

        var gems = new FeatureNode
        {
            Name = "Gems",
            OrderIndex = 2,
            ParentIds = new List<Guid> { rail.Id }
        };
        manager.RegisterNode(gems);

        var prongs = new FeatureNode
        {
            Name = "Prongs",
            OrderIndex = 3,
            ParentIds = new List<Guid> { gems.Id }
        };
        manager.RegisterNode(prongs);

        // Suppressing gems should cascade to prongs
        manager.SetSuppressed(gems.Id, true, cascadeToChildren: true);

        Assert.True(gems.IsSuppressed);
        Assert.True(prongs.IsSuppressed);

        Assert.True(manager.ShouldEvaluateNode(rail));
        Assert.False(manager.ShouldEvaluateNode(gems));
        Assert.False(manager.ShouldEvaluateNode(prongs));
    }

    [Fact]
    public void ExecutionOrder_ReturnsNodesSortedByOrderIndex()
    {
        var manager = new ParametricGraphManager();

        var n3 = new FeatureNode { Name = "Cutters", OrderIndex = 30 };
        var n1 = new FeatureNode { Name = "Rail", OrderIndex = 10 };
        var n2 = new FeatureNode { Name = "Shank", OrderIndex = 20 };

        manager.RegisterNode(n3);
        manager.RegisterNode(n1);
        manager.RegisterNode(n2);

        var ordered = manager.GetNodesInExecutionOrder();

        Assert.Equal(3, ordered.Count);
        Assert.Equal("Rail", ordered[0].Name);
        Assert.Equal("Shank", ordered[1].Name);
        Assert.Equal("Cutters", ordered[2].Name);
    }
}
