namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;

/// <summary>
/// Launches the 3D surface pavé placement workflow.
/// Calculates dense stone distribution, interstitial retention beads, and boundary festoons on freeform surfaces.
/// </summary>
[Guid("47C9A436-B321-4E80-9288-E166AE52F1D4")]
public class RadPaveCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetSurfaceId", "GemDiameterMm", "SpacingMm", "PatternType" };

    public override string EnglishName => "RadPave";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string targetSurfaceId = string.Empty;
        double gemDiameterMm = 1.3;
        double spacingMm = 0.15;
        PavePatternType patternType = PavePatternType.Hexagonal;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Pavé Options");

            var optDiameter = new OptionDouble(gemDiameterMm);
            var optSpacing = new OptionDouble(spacingMm);
            var optPattern = new OptionToggle(false, "Hexagonal", "Grid");

            go.AddOption("TargetSurfaceId");
            go.AddOptionDouble("GemDiameterMm", ref optDiameter);
            go.AddOptionDouble("SpacingMm", ref optSpacing);
            go.AddOptionToggle("PatternType", ref optPattern);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                gemDiameterMm = optDiameter.CurrentValue;
                spacingMm = optSpacing.CurrentValue;
                patternType = optPattern.CurrentValue ? PavePatternType.Grid : PavePatternType.Hexagonal;
            }
        }

        // Open studio workspace
        RadianceStudioPlugIn.OpenStudioWorkspace();

        var p = new PaveParameters
        {
            StoneDiameterMm = gemDiameterMm,
            StoneSpacingMm = spacingMm,
            PatternType = patternType
        };

        RhinoApp.WriteLine($"[RadianceStudio] 3D Surface Pavé configured: Stone Diameter {gemDiameterMm:F2} mm, Spacing {spacingMm:F2} mm, Pattern: {patternType}.");
        return Result.Success;
    }
}
