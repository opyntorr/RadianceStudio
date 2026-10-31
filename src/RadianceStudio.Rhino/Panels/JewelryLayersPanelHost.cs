namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the 4-column Jewelry Layers manager.
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99002")]
public class JewelryLayersPanelHost : JewelryLayersPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99002");
    public static JewelryLayersPanelHost? Instance { get; private set; }

    public JewelryLayersPanelHost() : base()
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
