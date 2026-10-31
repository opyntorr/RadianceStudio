namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;

/// <summary>
/// Execution archetype for a ribbon tool.
/// </summary>
public enum ToolExecutionType
{
    /// <summary>
    /// Executes a direct native Rhino CAD command via RhinoApp.RunScript.
    /// </summary>
    RhinoCommand,

    /// <summary>
    /// Executes an active parametric jewelry engine command with 60 FPS viewport feedback.
    /// </summary>
    DynamicJewelry,

    /// <summary>
    /// Opens a flyout sub-toolbar containing related commands.
    /// </summary>
    ToolGroup,

    /// <summary>
    /// Opens a dedicated dialog or utility window.
    /// </summary>
    Specialty,

    /// <summary>
    /// Roadmap placeholder tool with clear visual distinction.
    /// </summary>
    RoadmapPlaceholder
}

/// <summary>
/// Definition of a tool item in the commercial ribbon layout.
/// </summary>
public record ToolDefinition(
    string Id,
    string Name,
    string TabId,
    ToolExecutionType ExecutionType,
    string CommandOrMacro,
    string Description,
    bool HasLightningBadge = false,
    bool HasGroupBadge = false,
    bool IsImplemented = true,
    string? GroupKey = null
);

/// <summary>
/// Definition of a tool group containing multiple related CAD commands.
/// </summary>
public class ToolGroupDefinition
{
    public string Key { get; }
    public string Title { get; }
    public List<ToolDefinition> SubTools { get; }

    public ToolGroupDefinition(string key, string title, IEnumerable<ToolDefinition>? subTools = null)
    {
        Key = key;
        Title = title;
        SubTools = subTools != null ? new List<ToolDefinition>(subTools) : new List<ToolDefinition>();
    }
}
