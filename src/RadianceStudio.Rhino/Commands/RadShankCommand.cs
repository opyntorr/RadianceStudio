namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Launches the dynamic shank creator in the reactive Dynamic Inspector panel.
/// Sweeps parametric cross sections (DShape, ComfortFit, Flat, KnifeEdge, Cathedral) along the rail.
/// </summary>
[Guid("6F19A2E4-E5BE-48A1-88EB-6E04CE6B9999")]
public class RadShankCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetRailId", "ProfileType", "TaperRatio", "HollowAzurador" };

    public override string EnglishName => "RadShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string targetRailId = string.Empty;
        ShankProfileType profileType = ShankProfileType.DShape;
        double taperRatio = 0.8;
        bool hollowAzurador = false;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Shank Options");

            var optProfile = new OptionToggle(false, "DShape", "ComfortFit");
            var optTaper = new OptionDouble(taperRatio);
            var optHollow = new OptionToggle(hollowAzurador, "Off", "On");

            go.AddOption("TargetRailId");
            go.AddOptionToggle("ProfileType", ref optProfile);
            go.AddOptionDouble("TaperRatio", ref optTaper);
            go.AddOptionToggle("HollowAzurador", ref optHollow);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                profileType = optProfile.CurrentValue ? ShankProfileType.ComfortFit : ShankProfileType.DShape;
                taperRatio = optTaper.CurrentValue;
                hollowAzurador = optHollow.CurrentValue;
            }
        }

        // Open studio workspace and load ShankViewModel into the Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new ShankViewModel
        {
            ProfileType = profileType,
            HasUnderGallery = hollowAzurador
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Shank Creator loaded: Profile {profileType}, TaperRatio {taperRatio:F2}, HollowAzurador: {hollowAzurador}.");
        return Result.Success;
    }
}
