namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.MCP.Tools;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates parametric gem cutters in the document and activates the dynamic Inspector.
/// </summary>
[Guid("3FA11738-4BD6-46B9-B33D-94EDC5484812")]
public class RadCutterCommand : Command
{
    public static readonly string[] CommandParameters = { "GemId", "SeatAngleDegrees", "GirdleClearancePercent", "HasLightHole" };

    public override string EnglishName => "RadCutter";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var task = JewelryMcpRegistry.Default.Tools.SetCutterAsync(
            gemId: null,
            cutType: "RoundBrilliant",
            gemDiameterMm: 2.0,
            girdleClearanceMm: 0.08,
            seatAngle: "Degree70",
            crownHeightExtensionMm: 1.5,
            lightHoleDepthExtensionMm: 2.0,
            lightHoleDiameterRatio: 0.50,
            taperShaft: true);
        task.GetAwaiter().GetResult();

        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();
        var vm = new CutterViewModel
        {
            GirdleClearanceMm = 0.08,
            CrownHeightExtensionMm = 1.5,
            LightHoleDepthExtensionMm = 2.0
        };
        inspector.SetViewModel(vm);

        RhinoApp.WriteLine("[RadianceStudio] Parametric cutters created in viewport with light holes.");
        return Result.Success;
    }
}
