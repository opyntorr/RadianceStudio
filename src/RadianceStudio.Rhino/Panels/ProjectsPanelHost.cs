namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the dedicated Projects management panel.
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99004")]
public class ProjectsPanelHost : ProjectsPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99004");
    public static ProjectsPanelHost? Instance { get; private set; }

    public ProjectsPanelHost() : base()
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
