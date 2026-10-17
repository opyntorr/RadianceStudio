namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Launches the dynamic prong head setting creator in the reactive Dynamic Inspector panel.
/// Supports 4, 6, and 8 prong heads, V-prongs, and structural gallery wires.
/// </summary>
[Guid("5C8E53A2-9447-4E40-B9C0-C90DF0E9B401")]
public class RadProngHeadCommand : Command
{
    public static readonly string[] CommandParameters = { "GemId", "ProngCount", "ProngDiameterMm", "GalleryWire" };

    public override string EnglishName => "RadProngHead";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        string gemId = string.Empty;
        int prongCount = 4;
        double prongDiameterMm = 0.85;
        bool galleryWire = true;

        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Prong Head Options");

            var optCount = new OptionInteger(prongCount, 4, 8);
            var optDiameter = new OptionDouble(prongDiameterMm);
            var optGallery = new OptionToggle(galleryWire, "Off", "On");

            go.AddOption("GemId");
            go.AddOptionInteger("ProngCount", ref optCount);
            go.AddOptionDouble("ProngDiameterMm", ref optDiameter);
            go.AddOptionToggle("GalleryWire", ref optGallery);

            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                prongCount = optCount.CurrentValue;
                prongDiameterMm = optDiameter.CurrentValue;
                galleryWire = optGallery.CurrentValue;
            }
        }

        // Open studio workspace and load ProngHeadViewModel into Inspector
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new ProngHeadViewModel
        {
            ProngCount = (prongCount == 6 || prongCount == 8) ? prongCount : 4,
            ProngDiameterMm = prongDiameterMm,
            IncludeGalleryWire = galleryWire
        };

        inspector.SetViewModel(vm);

        RhinoApp.WriteLine($"[RadianceStudio] Dynamic Prong Head Creator loaded: {prongCount} Prongs, Diameter {prongDiameterMm:F2} mm, GalleryWire: {galleryWire}.");
        return Result.Success;
    }
}
