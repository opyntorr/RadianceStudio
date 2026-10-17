namespace RadianceStudio.Core.Graph;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single parametric operation node in the jewelry dependency graph.
/// Attached to native Rhino geometry via ParametricMetadata.
/// </summary>
public class FeatureNode
{
    /// <summary>
    /// Unique identifier for this parametric feature instance.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable feature name (e.g. "Round Ring Rail", "Cathedral Shank", "Prong Basket").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type key classifying this feature (e.g. "Rail", "Shank", "GemRow", "Setting", "Cutter").
    /// </summary>
    public string FeatureType { get; set; } = string.Empty;

    /// <summary>
    /// Chronological sequence index in the creation timeline.
    /// Used by FeatureTree mode for sequential rollback evaluation.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// Identifiers of parent features upon which this feature strictly depends.
    /// </summary>
    public List<Guid> ParentIds { get; init; } = new();

    /// <summary>
    /// Identifiers of child features that consume this feature's geometry as input.
    /// </summary>
    public List<Guid> ChildIds { get; init; } = new();

    /// <summary>
    /// Whether this feature is currently suppressed in the timeline.
    /// When true in FeatureTree mode, this feature and its dependents are skipped during evaluation.
    /// Transparent in StudioClassic mode.
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Optional dictionary of serializable parameter keys and values.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}
