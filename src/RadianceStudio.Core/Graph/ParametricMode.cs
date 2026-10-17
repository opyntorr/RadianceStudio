namespace RadianceStudio.Core.Graph;

/// <summary>
/// Evaluation and interaction mode for the parametric jewelry history graph.
/// </summary>
public enum ParametricMode
{
    /// <summary>
    /// Direct object-centric interaction: clicking geometry in the viewport opens its dynamic inspector.
    /// All dependent child operations re-evaluate reactively in cascade (standard jewelry workflow).
    /// </summary>
    StudioClassic,

    /// <summary>
    /// Timeline and feature-tree centric interaction: hierarchical tree with explicit execution order,
    /// draggable rollback bar, and branch suppression/unsuppression.
    /// </summary>
    FeatureTree
}
