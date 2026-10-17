namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.DocObjects;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Distributes stones along 3D space curves with 60 FPS viewport preview.
/// Couples GemEngine orthonormal frames with canonical instanced meshes.
/// </summary>
[Guid("9823BC4F-08E1-4FA3-8D1C-D295FF6A5C02")]
public class RadGemOnCurveCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetCurveId", "GemDiameterMm", "SpacingMm", "CutType" };

    public override string EnglishName => "RadGemOnCurve";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string targetCurveId = string.Empty;
        double gemDiameterMm = 2.0;
        double spacingMm = 0.2;
        GemCutType cutType = GemCutType.RoundBrilliant;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Gem on Curve Options");

            var optDiameter = new OptionDouble(gemDiameterMm);
            var optSpacing = new OptionDouble(spacingMm);
            var optCut = new OptionToggle(false, "RoundBrilliant", "Princess");

            go.AddOption("TargetCurveId");
            go.AddOptionDouble("GemDiameterMm", ref optDiameter);
            go.AddOptionDouble("SpacingMm", ref optSpacing);
            go.AddOptionToggle("CutType", ref optCut);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                gemDiameterMm = optDiameter.CurrentValue;
                spacingMm = optSpacing.CurrentValue;
                cutType = optCut.CurrentValue ? GemCutType.Princess : GemCutType.RoundBrilliant;
            }
        }

        // Open studio workspace and load GemOnCurveViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new GemOnCurveViewModel
        {
            GemDiameterMm = gemDiameterMm,
            SpacingMm = spacingMm,
            CutType = cutType
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Gem on Curve loaded: Diameter {gemDiameterMm:F2} mm, Spacing {spacingMm:F2} mm, Cut: {cutType}.");
        return Result.Success;
    }
}
