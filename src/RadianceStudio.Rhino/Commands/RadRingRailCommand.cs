namespace RadianceStudio.Rhino.Commands;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Geometry;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a canonical round parametric ring rail and activates the lateral dynamic Inspector.
/// In interactive mode, it binds the ViewModel immediately to the lateral panel with 60 FPS viewport preview.
/// </summary>
[Guid("1F6CA810-75C7-41E1-972E-BAF8C22F3144")]
public class RadRingRailCommand : Command
{
    public static readonly string[] CommandParameters = { "SizeUs", "WidthMm", "ThicknessMm", "ProfileType" };

    public override string EnglishName => "RadRingRail";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return ExecuteRingRail(doc, mode, RailProfileType.CanonicalCircle);
    }

    internal static Result ExecuteRingRail(RhinoDoc doc, RunMode mode, RailProfileType profileType)
    {
        double sizeUs = 7.0;
        var sizingStandard = RingSizingStandard.US;

        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new RingRailViewModel
        {
            Standard = sizingStandard,
            RingSize = sizeUs,
            ProfileType = profileType
        };

        if (mode == RunMode.Scripted)
        {
            // Non-interactive automated execution: bake directly
            vm.Apply(doc);
            RhinoApp.WriteLine($"[RadianceStudio] Ring Rail created (Scripted): {sizingStandard} Size {sizeUs:F1}, Profile: {profileType}.");
            return Result.Success;
        }

        // Interactive mode: bind to lateral Inspector with live 60 FPS viewport preview
        inspector.SetViewModel(vm);
        RhinoApp.WriteLine($"[RadianceStudio] Ring Rail active in Inspector: {sizingStandard} Size {sizeUs:F1}, Profile: {profileType}. Adjust in sidebar and click Apply.");
        return Result.Success;
    }
}

/// <summary>
/// Creates a Euro-Shank parametric ring rail and activates the lateral dynamic Inspector.
/// </summary>
[Guid("2E7DA810-75C7-41E1-972E-BAF8C22F3145")]
public class RadEuroRailCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadEuroRail";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadRingRailCommand.ExecuteRingRail(doc, mode, RailProfileType.EuroShank);
    }
}

/// <summary>
/// Creates a Cushion Squircle parametric ring rail and activates the lateral dynamic Inspector.
/// </summary>
[Guid("3F8EA810-75C7-41E1-972E-BAF8C22F3146")]
public class RadCushionRailCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadCushionRail";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadRingRailCommand.ExecuteRingRail(doc, mode, RailProfileType.CushionSquircle);
    }
}

/// <summary>
/// Creates a Bypass Overlap parametric ring rail and activates the lateral dynamic Inspector.
/// </summary>
[Guid("4A9FA810-75C7-41E1-972E-BAF8C22F3147")]
public class RadBypassRailCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadBypassRail";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadRingRailCommand.ExecuteRingRail(doc, mode, RailProfileType.BypassOverlap);
    }
}
