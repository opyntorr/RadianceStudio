namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using RadianceStudio.UI.History;

/// <summary>
/// Dockable Rhino Eto panel hosting the dual-mode Parametric History and Timeline panel.
/// </summary>
[Guid("8965008E-EEA4-4A86-BFA3-A2D696417531")]
public class HistoryPanelHost : HistoryPanel
{
    public static readonly Guid PanelId = new("8965008E-EEA4-4A86-BFA3-A2D696417531");
    public static HistoryPanelHost? Instance { get; private set; }

    public HistoryPanelHost() : base(RadianceStudioPlugIn.GraphManager, RadianceStudioPlugIn.InspectorPanel)
    {
        Instance = this;
        RadianceStudioPlugIn.HistoryPanel = this;
    }
}
