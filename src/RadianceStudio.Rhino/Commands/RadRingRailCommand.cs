namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Launches the dynamic ring rail creator in the reactive Dynamic Inspector panel.
/// Supports interactive UI parameter adjustment and scripted parameter overrides.
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

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Ring Rail Options");

            var optSize = new OptionDouble(sizeUs);
            var optWidth = new OptionDouble(widthMm);
            var optThickness = new OptionDouble(thicknessMm);
            var optProfile = new OptionToggle(false, "Circle", "EuroShank");

            go.AddOptionDouble("SizeUs", ref optSize);
            go.AddOptionDouble("WidthMm", ref optWidth);
            go.AddOptionDouble("ThicknessMm", ref optThickness);
            go.AddOptionToggle("ProfileType", ref optProfile);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                sizeUs = optSize.CurrentValue;
                widthMm = optWidth.CurrentValue;
                thicknessMm = optThickness.CurrentValue;
                profileType = optProfile.CurrentValue ? RailProfileType.EuroShank : RailProfileType.CanonicalCircle;
            }
        }

        // Open studio workspace and load RingRailViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new RingRailViewModel
        {
            Standard = RingSizingStandard.US,
            RingSize = sizeUs,
            ProfileType = profileType
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Ring Rail Creator loaded: US Size {sizeUs:F1}, Width {widthMm:F1} mm, Thickness {thicknessMm:F1} mm, Profile: {profileType}.");
        return Result.Success;
    }
}
