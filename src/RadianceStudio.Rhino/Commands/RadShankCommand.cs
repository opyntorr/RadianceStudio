namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.Core.Enums;
using RadianceStudio.MCP.Tools;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a parametric swept shank solid in the document and activates the dynamic Inspector.
/// Sweeps parametric cross sections (DShape, ComfortFit, Flat, KnifeEdge, Cathedral) along the rail.
/// </summary>
[Guid("6F19A2E4-E5BE-48A1-88EB-6E04CE6B9999")]
public class RadShankCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetRailId", "ProfileType", "TaperRatio", "HollowAzurador" };

    public override string EnglishName => "RadShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        ShankProfileType profileType = ShankProfileType.ComfortFit;
        bool hollowAzurador = false;

        // 1. Create swept shank via MCP engine (creates solid in doc.Objects and registers in ParametricGraph)
        var task = JewelryMcpRegistry.Default.Tools.SetShankAsync(
            railId: null,
            profileType: profileType.ToString(),
            bottomWidthMm: 2.2,
            bottomThicknessMm: 1.6,
            topWidthMm: 2.8,
            topThicknessMm: 2.0,
            hasUnderGallery: hollowAzurador);
        task.GetAwaiter().GetResult();

        // 2. Open studio workspace and load ShankViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new ShankViewModel
        {
            ProfileType = profileType,
            HasUnderGallery = hollowAzurador
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Shank solid created in viewport: Profile {profileType}, Base 2.2x1.6 mm, Top 2.8x2.0 mm.");
        return Result.Success;
    }
}
