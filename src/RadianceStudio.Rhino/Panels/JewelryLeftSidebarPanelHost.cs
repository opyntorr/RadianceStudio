namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the complete unified left jewelry sidebar:
/// Quick Commands, Projects, 4-Column Jewelry Layers, and Display Modes.
/// </summary>
[Guid("4F101111-2222-3333-4444-555555555502")]
public class JewelryLeftSidebarPanelHost : JewelryLeftSidebarPanel
{
    public static readonly Guid PanelId = new("4F101111-2222-3333-4444-555555555502");
    public static JewelryLeftSidebarPanelHost? Instance { get; private set; }

    public JewelryLeftSidebarPanelHost() : base()
    {
        Instance = this;
        CommandRequested += (s, cmd) =>
        {
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                try
                {
                    RhinoApp.RunScript(cmd, echo: false);
                }
                catch
                {
                }
            }
        };
    }
}
