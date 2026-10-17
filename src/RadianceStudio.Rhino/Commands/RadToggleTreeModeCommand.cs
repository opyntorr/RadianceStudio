namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;

/// <summary>
/// Toggles the Parametric History panel mode between:
/// 1. StudioClassic: Flat, contextual object-centric list view.
/// 2. FeatureTree: Hierarchical chronological timeline with rollback bar and suppress/resume actions.
/// </summary>
[Guid("E83F019E-2B1D-47F8-94D3-B9876C6E83F0")]
public class RadToggleTreeModeCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetMode" };

    public override string EnglishName => "RadToggleTreeMode";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string targetMode = string.Empty;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Parametric Mode");

            var optMode = new OptionToggle(false, "StudioClassic", "FeatureTree");
            go.AddOptionToggle("TargetMode", ref optMode);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                targetMode = optMode.CurrentValue ? "FeatureTree" : "StudioClassic";
            }
        }

        var graph = RadianceStudioPlugIn.GraphManager;
        var history = RadianceStudioPlugIn.HistoryPanel;

        if (!string.IsNullOrWhiteSpace(targetMode))
        {
            if (string.Equals(targetMode, "FeatureTree", StringComparison.OrdinalIgnoreCase))
            {
                graph.CurrentMode = ParametricMode.FeatureTree;
            }
            else
            {
                graph.CurrentMode = ParametricMode.StudioClassic;
            }
        }
        else
        {
            graph.CurrentMode = graph.CurrentMode == ParametricMode.StudioClassic
                ? ParametricMode.FeatureTree
                : ParametricMode.StudioClassic;
        }

        if (history != null)
        {
            history.CurrentMode = graph.CurrentMode;
        }

        RhinoApp.WriteLine($"[RadianceStudio] Parametric tree mode toggled to: {graph.CurrentMode}.");
        return Result.Success;
    }
}
