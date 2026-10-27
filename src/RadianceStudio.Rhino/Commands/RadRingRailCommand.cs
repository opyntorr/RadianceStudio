namespace RadianceStudio.Rhino.Commands;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Geometry;
using global::Rhino.Input;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a parametric ring rail curve in the document and activates the dynamic Inspector.
/// Supports standard Rhino interactive command options (Option A) and scripted execution.
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
        double thicknessMm = 1.6;
        string[] standards = { "US", "UK", "European", "Japanese" };
        string[] profiles = { "CanonicalCircle", "EuroShank", "CushionSquircle", "BypassOverlap" };
        int standardIndex = 0; // US
        int profileIndex = 0; // CanonicalCircle

        if (mode == RunMode.Interactive)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Ring Rail (Press Enter for Defaults)");
            go.AcceptNothing(true);

            int optRound = go.AddOption("Round");
            int optEuro = go.AddOption("Euro");
            int optCushion = go.AddOption("Cushion");
            int optBypass = go.AddOption("Bypass");

            var sizeOpt = new OptionDouble(sizeUs, 1.0, 25.0);
            var widthOpt = new OptionDouble(widthMm, 0.5, 20.0);
            var thickOpt = new OptionDouble(thicknessMm, 0.5, 10.0);

            int optSize = go.AddOptionDouble("Size", ref sizeOpt);
            int optStandard = go.AddOptionList("Standard", standards, standardIndex);
            int optProfile = go.AddOptionList("Profile", profiles, profileIndex);
            int optWidth = go.AddOptionDouble("Width", ref widthOpt);
            int optThick = go.AddOptionDouble("Thickness", ref thickOpt);

            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Nothing)
                {
                    // User pressed Enter to accept current settings
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }
                if (res == GetResult.Option)
                {
                    if (go.OptionIndex() == optRound) { profileIndex = 0; break; }
                    else if (go.OptionIndex() == optEuro) { profileIndex = 1; break; }
                    else if (go.OptionIndex() == optCushion) { profileIndex = 2; break; }
                    else if (go.OptionIndex() == optBypass) { profileIndex = 3; break; }
                    else if (go.OptionIndex() == optSize) sizeUs = sizeOpt.CurrentValue;
                    else if (go.OptionIndex() == optWidth) widthMm = widthOpt.CurrentValue;
                    else if (go.OptionIndex() == optThick) thicknessMm = thickOpt.CurrentValue;
                    else if (go.OptionIndex() == optStandard) standardIndex = go.Option().CurrentListOptionIndex;
                    else if (go.OptionIndex() == optProfile) profileIndex = go.Option().CurrentListOptionIndex;
                }
            }
        }

        var sizingStandard = Enum.Parse<RingSizingStandard>(standards[standardIndex]);
        if (!Enum.TryParse<RailProfileType>(profiles[profileIndex], out var profileType))
        {
            profileType = RailProfileType.CanonicalCircle;
        }

        double innerDiameterMm = RailEngine.SizeToInnerDiameterMm(sizingStandard, sizeUs);
        Guid objId = Guid.Empty;

        if (RailEngine.IsRhinoNativeAvailable && doc != null)
        {
            Curve? railCurve = RailEngine.CreateRingRail(innerDiameterMm, profileType);
            if (railCurve != null)
            {
                objId = doc.Objects.AddCurve(railCurve);
                doc.Objects.Select(objId);
                doc.Views.Redraw();
            }
        }

        // Register in Parametric Graph Manager
        var node = new FeatureNode
        {
            Id = objId != Guid.Empty ? objId : Guid.NewGuid(),
            Name = $"RingRail_{profileType}_{sizeUs:F1}",
            FeatureType = "Rail",
            Parameters = new Dictionary<string, object>
            {
                ["SizeUs"] = sizeUs,
                ["Standard"] = sizingStandard.ToString(),
                ["ProfileType"] = profileType.ToString(),
                ["InnerDiameterMm"] = innerDiameterMm,
                ["WidthMm"] = widthMm,
                ["ThicknessMm"] = thicknessMm
            }
        };
        RadianceStudioPlugIn.GraphManager.RegisterNode(node);

        // Open studio workspace and bind Dynamic Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();
        var vm = new RingRailViewModel
        {
            Standard = sizingStandard,
            RingSize = sizeUs,
            ProfileType = profileType
        };
        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Ring Rail created in viewport: {sizingStandard} Size {sizeUs:F1} (ID: {innerDiameterMm:F2} mm), Profile: {profileType}.");
        return Result.Success;
    }
}
