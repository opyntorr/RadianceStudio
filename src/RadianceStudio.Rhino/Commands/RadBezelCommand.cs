namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Launches the dynamic bezel / halo setting creator in the reactive Dynamic Inspector panel.
/// Supports full and semi-bezels, draft angles, bearing ledges, and light holes.
/// </summary>
[Guid("43242045-A672-4CA7-959E-97C337D024A2")]
public class RadBezelCommand : Command
{
    public static readonly string[] CommandParameters = { "GemId", "WallThicknessMm", "HeightAboveGirdleMm", "SemiBezel" };

    public override string EnglishName => "RadBezel";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string gemId = string.Empty;
        double wallThicknessMm = 0.5;
        double heightAboveGirdleMm = 0.3;
        bool semiBezel = false;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Bezel Options");

            var optWall = new OptionDouble(wallThicknessMm);
            var optHeight = new OptionDouble(heightAboveGirdleMm);
            var optSemi = new OptionToggle(semiBezel, "Off", "On");

            go.AddOption("GemId");
            go.AddOptionDouble("WallThicknessMm", ref optWall);
            go.AddOptionDouble("HeightAboveGirdleMm", ref optHeight);
            go.AddOptionToggle("SemiBezel", ref optSemi);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                wallThicknessMm = optWall.CurrentValue;
                heightAboveGirdleMm = optHeight.CurrentValue;
                semiBezel = optSemi.CurrentValue;
            }
        }

        // Open studio workspace and load BezelViewModel into Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new BezelViewModel
        {
            WallThicknessMm = wallThicknessMm,
            HeightAboveGirdleMm = heightAboveGirdleMm,
            IsSemiBezel = semiBezel
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Bezel Creator loaded: Wall {wallThicknessMm:F2} mm, Height {heightAboveGirdleMm:F2} mm, SemiBezel: {semiBezel}.");
        return Result.Success;
    }
}
