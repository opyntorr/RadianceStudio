namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;

/// <summary>
/// Docks and opens the complete standalone jewelry UI workspace:
/// Jewelry Ribbon bar, reactive Dynamic Inspector panel, and Parametric History timeline.
/// </summary>
[Guid("43A7C8E0-9EFE-409B-B03C-9E28FE27DFCE")]
[CommandStyle(Style.ScriptRunner)]
public class RadStudioCommand : Command
{
    public static readonly string[] CommandParameters = { "LayoutMode" };

    public override string EnglishName => "RadStudio";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string layoutMode = "Default";

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Workspace Options");
            var optLayout = new OptionToggle(false, "Default", "Expanded");
            go.AddOptionToggle("LayoutMode", ref optLayout);
            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                layoutMode = optLayout.CurrentValue ? "Expanded" : "Default";
            }
        }

        // Apply window title and dock panels
        RadianceStudioPlugIn.SetStudioWindowTitle();
        RadianceStudioPlugIn.OpenStudioWorkspace();

        RhinoApp.WriteLine($"[RadianceStudio] Workspace active (LayoutMode: {layoutMode}).");
        return Result.Success;
    }
}
