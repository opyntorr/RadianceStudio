namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the Display Modes quick switcher.
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99003")]
public class DisplayModesPanelHost : DisplayModesPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99003");
    public static DisplayModesPanelHost? Instance { get; private set; }

    public DisplayModesPanelHost() : base()
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
