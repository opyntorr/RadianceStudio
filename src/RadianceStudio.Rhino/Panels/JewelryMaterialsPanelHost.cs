namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the dedicated Jewelry Materials library panel.
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99005")]
public class JewelryMaterialsPanelHost : JewelryMaterialsPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99005");
    public static JewelryMaterialsPanelHost? Instance { get; private set; }

    public JewelryMaterialsPanelHost() : base()
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
