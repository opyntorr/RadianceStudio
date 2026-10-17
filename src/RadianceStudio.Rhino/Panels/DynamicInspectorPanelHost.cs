namespace RadianceStudio.Rhino.Panels;

using System;
using System.Runtime.InteropServices;
using RadianceStudio.UI.Inspector;

/// <summary>
/// Dockable Rhino Eto panel hosting the reactive Dynamic Inspector panel.
/// </summary>
[Guid("552C1656-787D-4FCE-A263-AC0174092E47")]
public class DynamicInspectorPanelHost : DynamicInspectorPanel
{
    public static readonly Guid PanelId = new("552C1656-787D-4FCE-A263-AC0174092E47");
    public static DynamicInspectorPanelHost? Instance { get; private set; }

    public DynamicInspectorPanelHost() : base(RadianceStudioPlugIn.GraphManager)
    {
        Instance = this;
        RadianceStudioPlugIn.InspectorPanel = this;

        // Ensure cross-link with HistoryPanel if history was opened first
        if (RadianceStudioPlugIn.HistoryPanel != null)
        {
            RadianceStudioPlugIn.HistoryPanel.InspectorPanel = this;
        }
    }
}
