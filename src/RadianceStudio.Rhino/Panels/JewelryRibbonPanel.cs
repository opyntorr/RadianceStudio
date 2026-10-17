namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Ribbon;

/// <summary>
/// Dockable Rhino Eto panel hosting the Jewelry Ribbon bar.
/// </summary>
[Guid("7721867F-0AC8-45F4-A0C3-BC5D500BA899")]
public class JewelryRibbonPanel : JewelryRibbon
{
    public static readonly Guid PanelId = new("7721867F-0AC8-45F4-A0C3-BC5D500BA899");
    public static JewelryRibbonPanel? Instance { get; private set; }

    public JewelryRibbonPanel() : base()
    {
        Instance = this;
        RadianceStudioPlugIn.Ribbon = this;

        // Route ribbon action requests to Rhino command execution
        CommandRequested += OnCommandRequested;
    }

    private void OnCommandRequested(object? sender, string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName)) return;

        try
        {
            RhinoApp.RunScript(commandName, echo: false);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Unable to execute ribbon command '{commandName}': {ex.Message}");
        }
    }
}
