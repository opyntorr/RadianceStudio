namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.Core.Enums;
using RadianceStudio.MCP.Tools;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Distributes stones along 3D space curves and displays canonical meshes in the document.
/// </summary>
[Guid("9823BC4F-08E1-4FA3-8D1C-D295FF6A5C02")]
public class RadGemOnCurveCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetCurveId", "GemDiameterMm", "SpacingMm", "CutType" };

    public override string EnglishName => "RadGemOnCurve";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        double gemDiameterMm = 2.0;
        double spacingMm = 0.25;
        GemCutType cutType = GemCutType.RoundBrilliant;

        // 1. Distribute gems via MCP engine (creates gem meshes in doc.Objects and registers in ParametricGraph)
        var task = JewelryMcpRegistry.Default.Tools.SetGemRowAsync(
            targetCurveId: null,
            gemDiameterMm: gemDiameterMm,
            spacingMm: spacingMm,
            cutType: cutType.ToString());
        task.GetAwaiter().GetResult();

        // 2. Open studio workspace and load GemOnCurveViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new GemOnCurveViewModel
        {
            GemDiameterMm = gemDiameterMm,
            SpacingMm = spacingMm,
            CutType = cutType
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Gems on curve created in viewport: {cutType} ({gemDiameterMm:F2} mm).");
        return Result.Success;
    }
}
