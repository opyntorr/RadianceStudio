namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the Quick Commands shortcut grid.
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99001")]
public class QuickCommandsPanelHost : QuickCommandsPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99001");
    public static QuickCommandsPanelHost? Instance { get; private set; }

    public QuickCommandsPanelHost() : base()
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
