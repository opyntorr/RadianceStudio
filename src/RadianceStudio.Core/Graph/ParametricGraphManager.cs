namespace RadianceStudio.Core.Graph;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central manager for the parametric jewelry dependency graph.
/// Evaluates node execution based on the active ParametricMode (StudioClassic vs FeatureTree).
/// </summary>
public class ParametricGraphManager
{
    private readonly Dictionary<Guid, FeatureNode> _nodes = new();
    private int _nextOrderIndex = 1;

    /// <summary>
    /// Current evaluation and interaction mode. Defaults to StudioClassic (1:1 direct workflow).
    /// </summary>
    public ParametricMode CurrentMode { get; set; } = ParametricMode.StudioClassic;

    /// <summary>
    /// Timeline cutoff index for the rollback bar in FeatureTree mode.
    /// Nodes with OrderIndex > RollbackIndex are frozen / skipped.
    /// </summary>
    public int RollbackIndex { get; set; } = int.MaxValue;

    /// <summary>
    /// Total count of registered feature nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Registers a new or updated feature node into the graph.
    /// Automatically assigns an incremental OrderIndex if not already set.
    /// </summary>
    public void RegisterNode(FeatureNode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        if (node.OrderIndex <= 0)
        {
            node.OrderIndex = _nextOrderIndex++;
        }
        else if (node.OrderIndex >= _nextOrderIndex)
        {
            _nextOrderIndex = node.OrderIndex + 1;
        }

        _nodes[node.Id] = node;

        // Establish bidirectional parent-child links
        foreach (var parentId in node.ParentIds)
        {
            if (_nodes.TryGetValue(parentId, out var parent) && !parent.ChildIds.Contains(node.Id))
            {
                parent.ChildIds.Add(node.Id);
            }
        }
    }

    /// <summary>
    /// Removes a node from the graph and cleans up parent/child relationship references.
    /// </summary>
    public bool UnregisterNode(Guid id)
    {
        if (!_nodes.TryGetValue(id, out var node))
        {
            return false;
        }

        foreach (var parentId in node.ParentIds)
        {
            if (_nodes.TryGetValue(parentId, out var parent))
            {
                parent.ChildIds.Remove(id);
            }
        }

        foreach (var childId in node.ChildIds)
        {
            if (_nodes.TryGetValue(childId, out var child))
            {
                child.ParentIds.Remove(id);
            }
        }

        return _nodes.Remove(id);
    }

    /// <summary>
    /// Retrieves a feature node by its unique identifier.
    /// </summary>
    public FeatureNode? GetNode(Guid id)
    {
        return _nodes.TryGetValue(id, out var node) ? node : null;
    }

    /// <summary>
    /// Returns all registered feature nodes sorted chronologically by their execution OrderIndex.
    /// </summary>
    public IReadOnlyList<FeatureNode> GetNodesInExecutionOrder()
    {
        return _nodes.Values.OrderBy(n => n.OrderIndex).ToList();
    }

    /// <summary>
    /// Determines whether a given feature node should be evaluated and rendered.
    /// </summary>
    public bool ShouldEvaluateNode(FeatureNode node)
    {
        if (node == null) return false;

        // In StudioClassic mode, every active feature is evaluated reactively
        if (CurrentMode == ParametricMode.StudioClassic)
        {
            return true;
        }

        // In FeatureTree mode, respect RollbackIndex, suppression, and parent evaluation state
        if (node.IsSuppressed)
        {
            return false;
        }

        if (node.OrderIndex > RollbackIndex)
        {
            return false;
        }

        // If any required parent is not evaluated, this child cannot evaluate
        foreach (var parentId in node.ParentIds)
        {
            if (_nodes.TryGetValue(parentId, out var parent))
            {
                if (!ShouldEvaluateNode(parent))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Sets the suppression state of a feature node, optionally cascading to all dependent children.
    /// </summary>
    public void SetSuppressed(Guid nodeId, bool isSuppressed, bool cascadeToChildren = true)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            return;
        }

        node.IsSuppressed = isSuppressed;

        if (cascadeToChildren && isSuppressed)
        {
            foreach (var childId in node.ChildIds)
            {
                SetSuppressed(childId, true, cascadeToChildren: true);
            }
        }
    }

    /// <summary>
    /// Resets the rollback position to the end of the timeline (evaluating all unsuppressed nodes).
    /// </summary>
    public void ResetRollbackToEnd()
    {
        RollbackIndex = int.MaxValue;
    }

    /// <summary>
    /// Clears all nodes from the graph manager.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _nextOrderIndex = 1;
        RollbackIndex = int.MaxValue;
    }
}
