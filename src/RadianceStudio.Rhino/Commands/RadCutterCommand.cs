namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Applies gem cutters, bearing seat ledges, and light hole punches,
/// performing automated boolean difference subtraction against target metal solids.
/// </summary>
[Guid("29864235-CE06-4447-975E-1616AA7C8157")]
public class RadCutterCommand : Command
{
    public static readonly string[] CommandParameters = { "GemId", "SeatAngleDegrees", "GirdleClearancePercent", "LightHole" };

    public override string EnglishName => "RadCutter";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string gemId = string.Empty;
        double seatAngleDegrees = 90.0;
        double girdleClearancePercent = 10.0;
        bool lightHole = true;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Cutter Options");

            var optAngle = new OptionDouble(seatAngleDegrees);
            var optClearance = new OptionDouble(girdleClearancePercent);
            var optLight = new OptionToggle(lightHole, "Off", "On");

            go.AddOption("GemId");
            go.AddOptionDouble("SeatAngleDegrees", ref optAngle);
            go.AddOptionDouble("GirdleClearancePercent", ref optClearance);
            go.AddOptionToggle("LightHole", ref optLight);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                seatAngleDegrees = optAngle.CurrentValue;
                girdleClearancePercent = optClearance.CurrentValue;
                lightHole = optLight.CurrentValue;
            }
        }

        // Open studio workspace and load CutterViewModel into Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var seatStyle = Math.Abs(seatAngleDegrees - 70.0) < 5.0
            ? CutterSeatStyle.Degree70
            : CutterSeatStyle.Degree90;

        var vm = new CutterViewModel
        {
            SeatAngle = seatStyle,
            GirdleClearanceMm = 0.08 * (girdleClearancePercent / 10.0),
            LightHoleDiameterRatio = lightHole ? 0.50 : 0.0
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Gem Cutter loaded: Seat Angle {seatAngleDegrees:F0} deg, Clearance {girdleClearancePercent:F1}%, LightHole: {lightHole}.");
        return Result.Success;
    }
}
