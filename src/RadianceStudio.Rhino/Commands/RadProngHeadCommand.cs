namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.MCP.Tools;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates procedural prong heads (4/6/8-prongs, V-prongs) in the document.
/// </summary>
[Guid("9C775BD2-6F80-4D54-94EC-D1EFE1FF2901")]
public class RadProngHeadCommand : Command
{
    public static readonly string[] CommandParameters = { "GemId", "ProngCount", "ProngDiameterMm", "GirdleOverhangMm" };

    public override string EnglishName => "RadProngHead";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var task = JewelryMcpRegistry.Default.Tools.SetProngHeadAsync(
            gemId: null,
            cutType: "RoundBrilliant",
            gemDiameterMm: 6.5,
            prongCount: 4,
            prongStyle: "Claw",
            prongDiameterMm: 0.85,
            prongHeightMm: 4.5,
            girdleOverhangMm: 0.25);
        task.GetAwaiter().GetResult();

        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();
        var vm = new ProngHeadViewModel
        {
            ProngCount = 4,
            ProngDiameterMm = 0.85
        };
        inspector.SetViewModel(vm);

        RhinoApp.WriteLine("[RadianceStudio] Prong setting created in viewport: 4 prongs, 0.85 mm diameter.");
        return Result.Success;
    }
}
