namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.Core.Enums;
using RadianceStudio.MCP.Tools;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a parametric ring rail curve in the document and activates the dynamic Inspector.
/// </summary>
[Guid("1F6CA810-75C7-41E1-972E-BAF8C22F3144")]
public class RadRingRailCommand : Command
{
    public static readonly string[] CommandParameters = { "SizeUs", "WidthMm", "ThicknessMm", "ProfileType" };

    public override string EnglishName => "RadRingRail";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        double sizeUs = 7.0;
        double widthMm = 2.0;
        double thicknessMm = 1.5;
        RailProfileType profileType = RailProfileType.CanonicalCircle;

        // 1. Create ring rail via MCP engine (creates curve in doc.Objects and registers in ParametricGraph)
        var task = JewelryMcpRegistry.Default.Tools.SetRingRailAsync(
            sizeUs,
            "US",
            profileType.ToString(),
            widthMm,
            thicknessMm);
        task.GetAwaiter().GetResult();

        // 2. Open studio workspace and load RingRailViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new RingRailViewModel
        {
            Standard = RingSizingStandard.US,
            RingSize = sizeUs,
            ProfileType = profileType
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Ring Rail created in viewport: US Size {sizeUs:F1}, Width {widthMm:F1} mm, Thickness {thicknessMm:F1} mm, Profile: {profileType}.");
        return Result.Success;
    }
}
