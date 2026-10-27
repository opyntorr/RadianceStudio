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
using RadianceStudio.Core.Models;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a parametric swept shank solid in the document and activates the dynamic Inspector.
/// Sweeps parametric cross sections (DShape, ComfortFit, Flat, KnifeEdge, Cathedral) along the rail.
/// Supports standard Rhino interactive command options (Option A) and scripted execution.
/// </summary>
[Guid("6F19A2E4-E5BE-48A1-88EB-6E04CE6B9999")]
public class RadShankCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetRailId", "ProfileType", "TaperRatio", "HollowAzurador" };

    public override string EnglishName => "RadShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string[] profiles = { "ComfortFit", "Flat", "DShape", "KnifeEdge", "Cathedral" };
        int profileIndex = 0; // ComfortFit
        double bottomWidth = 2.2;
        double bottomThickness = 1.6;
        double topWidth = 2.8;
        double topThickness = 2.0;
        bool hollowAzurador = false;

        if (mode == RunMode.Interactive)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Shank (Press Enter for Defaults)");
            go.AcceptNothing(true);

            int optComfortFit = go.AddOption("ComfortFit");
            int optFlat = go.AddOption("Flat");
            int optDShape = go.AddOption("DShape");
            int optKnifeEdge = go.AddOption("KnifeEdge");
            int optCathedral = go.AddOption("Cathedral");
            int optEuroShank = go.AddOption("EuroShank");

            var bwOpt = new OptionDouble(bottomWidth, 0.5, 20.0);
            var btOpt = new OptionDouble(bottomThickness, 0.5, 10.0);
            var twOpt = new OptionDouble(topWidth, 0.5, 20.0);
            var ttOpt = new OptionDouble(topThickness, 0.5, 10.0);
            var hollowOpt = new OptionToggle(hollowAzurador, "Off", "On");

            int optProfile = go.AddOptionList("Profile", profiles, profileIndex);
            int optBw = go.AddOptionDouble("BottomWidth", ref bwOpt);
            int optBt = go.AddOptionDouble("BottomThickness", ref btOpt);
            int optTw = go.AddOptionDouble("TopWidth", ref twOpt);
            int optTt = go.AddOptionDouble("TopThickness", ref ttOpt);
            int optHollow = go.AddOptionToggle("HollowAzurador", ref hollowOpt);

            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }
                if (res == GetResult.Option)
                {
                    if (go.OptionIndex() == optComfortFit) { profileIndex = 0; break; }
                    else if (go.OptionIndex() == optFlat) { profileIndex = 1; break; }
                    else if (go.OptionIndex() == optDShape) { profileIndex = 2; break; }
                    else if (go.OptionIndex() == optKnifeEdge) { profileIndex = 3; break; }
                    else if (go.OptionIndex() == optCathedral) { profileIndex = 4; break; }
                    else if (go.OptionIndex() == optEuroShank) { profileIndex = 2; break; }
                    else if (go.OptionIndex() == optProfile) profileIndex = go.Option().CurrentListOptionIndex;
                    else if (go.OptionIndex() == optBw) bottomWidth = bwOpt.CurrentValue;
                    else if (go.OptionIndex() == optBt) bottomThickness = btOpt.CurrentValue;
                    else if (go.OptionIndex() == optTw) topWidth = twOpt.CurrentValue;
                    else if (go.OptionIndex() == optTt) topThickness = ttOpt.CurrentValue;
                    else if (go.OptionIndex() == optHollow) hollowAzurador = hollowOpt.CurrentValue;
                }
            }
        }

        if (!Enum.TryParse<ShankProfileType>(profiles[profileIndex], out var profileType))
        {
            profileType = ShankProfileType.ComfortFit;
        }

        Guid objId = Guid.Empty;

        if (RailEngine.IsRhinoNativeAvailable && doc != null)
        {
            // Build rail curve or retrieve existing
            Curve? railCurve = null;
            foreach (var rhObj in doc.Objects)
            {
                if (rhObj.Geometry is Curve c)
                {
                    railCurve = c;
                    break;
                }
            }

            if (railCurve == null)
            {
                double defaultDiameterMm = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, 7.0);
                railCurve = RailEngine.CreateRingRail(defaultDiameterMm, RailProfileType.CanonicalCircle);
            }

            if (railCurve != null)
            {
                var shankParams = new ShankParameters
                {
                    ProfileType = profileType,
                    BottomWidthMm = bottomWidth,
                    BottomThicknessMm = bottomThickness,
                    TopWidthMm = topWidth,
                    TopThicknessMm = topThickness,
                    UnderGallery = hollowAzurador ? new UnderGalleryParameters() : null
                };

                Brep? brep = ShankEngine.CreateSweptShank(railCurve, shankParams);
                if (brep != null)
                {
                    objId = doc.Objects.AddBrep(brep);
                    doc.Objects.Select(objId);
                    doc.Views.Redraw();
                }
            }
        }

        // Register in Parametric Graph Manager
        var node = new FeatureNode
        {
            Id = objId != Guid.Empty ? objId : Guid.NewGuid(),
            Name = $"Shank_{profileType}",
            FeatureType = "Shank",
            Parameters = new Dictionary<string, object>
            {
                ["ProfileType"] = profileType.ToString(),
                ["BottomWidthMm"] = bottomWidth,
                ["BottomThicknessMm"] = bottomThickness,
                ["TopWidthMm"] = topWidth,
                ["TopThicknessMm"] = topThickness,
                ["HollowAzurador"] = hollowAzurador
            }
        };
        RadianceStudioPlugIn.GraphManager.RegisterNode(node);

        // Open studio workspace and bind Dynamic Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();
        var vm = new ShankViewModel
        {
            ProfileType = profileType,
            HasUnderGallery = hollowAzurador
        };
        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Shank solid created in viewport: Profile {profileType}, Base {bottomWidth:F1}x{bottomThickness:F1} mm, Top {topWidth:F1}x{topThickness:F1} mm.");
        return Result.Success;
    }
}
