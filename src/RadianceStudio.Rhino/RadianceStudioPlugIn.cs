namespace RadianceStudio.Rhino;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.PlugIns;
using RhinoPanels = global::Rhino.UI.Panels;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Graph;
using RadianceStudio.Rhino.Panels;
using RadianceStudio.UI.History;
using RadianceStudio.UI.Inspector;
using RadianceStudio.UI.Ribbon;

/// <summary>
/// Core Rhino 8 Plugin entry point for RadianceStudio Standalone Jewelry CAD Suite.
/// Manages application scheme lifecycle, millimetric template configuration,
/// Eto UI panel docking, and command execution pipelines.
/// </summary>
[Guid("1D4FB0A4-325A-4B6A-B6B1-D1817EB7FF4B")]
public class RadianceStudioPlugIn : PlugIn
{
    public const string StudioSchemeName = "RadianceStudio";
    public const string StudioWindowTitle = "RadianceStudio - Jewelry CAD Suite";
    public static readonly Guid PlugInGuid = new("1D4FB0A4-325A-4B6A-B6B1-D1817EB7FF4B");

    private static RadianceStudioPlugIn? _instance;
    private static bool _initialWorkspaceConfigured;

    public static RadianceStudioPlugIn Instance => _instance ??= (RadianceStudioPlugIn)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(RadianceStudioPlugIn));

    public static ParametricGraphManager GraphManager { get; } = new();
    public static DynamicInspectorPanel? InspectorPanel { get; internal set; }
    public static HistoryPanel? HistoryPanel { get; internal set; }
    public static JewelryRibbon? Ribbon { get; internal set; }

    public RadianceStudioPlugIn()
    {
        _instance = this;
        EnsurePlugInId();
    }

    private void EnsurePlugInId()
    {
        try
        {
            var idField = typeof(PlugIn).GetField("m_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null)
            {
                var cur = (Guid)(idField.GetValue(this) ?? Guid.Empty);
                if (cur == Guid.Empty)
                {
                    idField.SetValue(this, PlugInGuid);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Forces plugin to load automatically at Rhino startup.
    /// </summary>
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

    /// <summary>
    /// Lifecycle startup entry point.
    /// </summary>
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        try
        {
            EnsurePlugInId();

            // Hook Rhino document and application idle lifecycle events
            RhinoDoc.NewDocument += OnNewDocument;
            RhinoDoc.EndOpenDocument += OnEndOpenDocument;
            RhinoApp.Idle += OnRhinoIdle;

            // Register Eto.Forms UI panels with Rhino Panels system
            RegisterStudioPanels();

            // If running under the RadianceStudio scheme, initialize title, units, and workspace
            if (IsRadianceStudioScheme())
            {
                SetStudioWindowTitle();

                if (RhinoDoc.ActiveDoc != null)
                {
                    ConfigureJewelryDocument(RhinoDoc.ActiveDoc);
                }

                OpenStudioWorkspace();
            }

            return LoadReturnCode.Success;
        }
        catch (Exception ex)
        {
            errorMessage = $"RadianceStudio failed to initialize: {ex.Message}";
            return LoadReturnCode.ErrorShowDialog;
        }
    }

    /// <summary>
    /// Cleans up event handlers on plugin shutdown.
    /// </summary>
    protected override void OnShutdown()
    {
        RhinoDoc.NewDocument -= OnNewDocument;
        RhinoDoc.EndOpenDocument -= OnEndOpenDocument;
        RhinoApp.Idle -= OnRhinoIdle;

        base.OnShutdown();
    }

    #region Scheme & Branding

    /// <summary>
    /// Checks whether Rhino was launched under the RadianceStudio custom scheme.
    /// </summary>
    public static bool IsRadianceStudioScheme()
    {
        try
        {
            if (string.Equals(RhinoApp.SchemeName, StudioSchemeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            string cmdLine = Environment.CommandLine;
            if (cmdLine.IndexOf(StudioSchemeName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// Sets the top-level main window title via Eto.Forms Application.Instance.MainForm.Title.
    /// </summary>
    public static void SetStudioWindowTitle()
    {
        try
        {
            if (Eto.Forms.Application.Instance?.MainForm != null)
            {
                Eto.Forms.Application.Instance.MainForm.Title = StudioWindowTitle;
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note updating window title: {ex.Message}");
        }
    }

    #endregion

    #region Document Template & Jewelry Tolerances

    /// <summary>
    /// Configures the default document template to millimetric jewelry units:
    /// - ModelUnitSystem: Millimeters
    /// - ModelAbsoluteTolerance: 0.001 mm (1 micron)
    /// - ModelRelativeTolerance: 0.001 (0.1%)
    /// - ModelAngleToleranceDegrees: 0.1 degrees
    /// - Studio Ground Plane: Enabled = true, Altitude = 0.0, ShadowOnly = true
    /// - Viewport Construction Plane: GridSpacing = 0.1 mm, SnapSpacing = 0.1 mm
    /// </summary>
    public static void ConfigureJewelryDocument(RhinoDoc? doc)
    {
        if (doc == null) return;

        try
        {
            doc.ModelUnitSystem = UnitSystem.Millimeters;
            doc.ModelAbsoluteTolerance = 0.001;
            doc.ModelRelativeTolerance = 0.001;
            doc.ModelAngleToleranceDegrees = 0.1;

            var groundPlane = doc.RenderSettings?.GroundPlane;
            if (groundPlane != null)
            {
                groundPlane.Enabled = true;
                groundPlane.Altitude = 0.0;
                groundPlane.ShadowOnly = true;
            }

            if (doc.Views != null)
            {
                foreach (var view in doc.Views)
                {
                    if (view?.ActiveViewport != null)
                    {
                        var cplane = view.ActiveViewport.GetConstructionPlane();
                        cplane.GridSpacing = 0.1;
                        cplane.SnapSpacing = 0.1;
                        view.ActiveViewport.SetConstructionPlane(cplane);
                    }
                }

                doc.Views.Redraw();
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note applying jewelry template: {ex.Message}");
        }
    }

    #endregion

    #region Panels & Workspace Docking

    /// <summary>
    /// Registers the 3 core Eto UI panels with Rhino's docking system.
    /// </summary>
    public void RegisterStudioPanels()
    {
        EnsurePlugInId();
        if (!RailEngine.IsRhinoNativeAvailable) return;

        try
        {
            RhinoPanels.RegisterPanel(this, typeof(JewelryRibbonPanel), "Radiance Ribbon", null);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note registering Ribbon panel: {ex.Message}");
        }

        try
        {
            RhinoPanels.RegisterPanel(this, typeof(DynamicInspectorPanelHost), "Jewelry Inspector", null);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note registering Inspector panel: {ex.Message}");
        }

        try
        {
            RhinoPanels.RegisterPanel(this, typeof(HistoryPanelHost), "Parametric History", null);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note registering History panel: {ex.Message}");
        }
    }

    /// <summary>
    /// Hides Rhino's default Standard Toolbars and tab groups so that only RadianceStudio's
    /// dedicated top ribbon and jewelry commands are exposed, providing a standalone commercial jewelry CAD suite experience.
    /// </summary>
    public static void HideStandardToolbars()
    {
        if (!RailEngine.IsRhinoNativeAvailable) return;

        try
        {
            if (RhinoApp.ToolbarFiles != null)
            {
                foreach (var tf in RhinoApp.ToolbarFiles)
                {
                    if (tf == null) continue;
                    for (int i = 0; i < tf.GroupCount; i++)
                    {
                        var group = tf.GetGroup(i);
                        if (group != null && (group.Name.Contains("Standard", StringComparison.OrdinalIgnoreCase) ||
                                              group.Name.Contains("Default", StringComparison.OrdinalIgnoreCase)))
                        {
                            group.Visible = false;
                        }
                    }
                }
            }

            RhinoApp.RunScript("-_ToolbarGroup \"Standard Toolbars\" Hide", echo: false);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note adjusting toolbar visibility: {ex.Message}");
        }
    }

    /// <summary>
    /// Docks and displays the full standalone jewelry UI workspace:
    /// Ribbon bar, Dynamic Inspector panel, and Parametric History panel.
    /// </summary>
    public static void OpenStudioWorkspace()
    {
        if (!RailEngine.IsRhinoNativeAvailable) return;

        try
        {
            HideStandardToolbars();

            // Open Inspector and History as docked siblings
            RhinoPanels.OpenPanel(DynamicInspectorPanelHost.PanelId);
            RhinoPanels.OpenPanelAsSibling(HistoryPanelHost.PanelId, DynamicInspectorPanelHost.PanelId);

            // Open Ribbon panel
            RhinoPanels.OpenPanel(JewelryRibbonPanel.PanelId);

            // Connect Inspector and History if both are instantiated
            if (InspectorPanel != null && HistoryPanel != null)
            {
                HistoryPanel.InspectorPanel = InspectorPanel;
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Note opening studio workspace: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper to retrieve the current active DynamicInspectorPanel, or instantiate a fallback if unattached.
    /// </summary>
    public static DynamicInspectorPanel GetOrCreateInspector()
    {
        if (InspectorPanel != null)
        {
            return InspectorPanel;
        }

        InspectorPanel = new DynamicInspectorPanel(GraphManager);
        if (HistoryPanel != null)
        {
            HistoryPanel.InspectorPanel = InspectorPanel;
        }

        return InspectorPanel;
    }

    #endregion

    #region Event Handlers

    private static void OnNewDocument(object? sender, DocumentEventArgs e)
    {
        if (IsRadianceStudioScheme())
        {
            ConfigureJewelryDocument(e.Document);
            SetStudioWindowTitle();
        }
    }

    private static void OnEndOpenDocument(object? sender, DocumentOpenEventArgs e)
    {
        if (IsRadianceStudioScheme())
        {
            ConfigureJewelryDocument(e.Document);
            SetStudioWindowTitle();
        }
    }

    private static void OnRhinoIdle(object? sender, EventArgs e)
    {
        if (!IsRadianceStudioScheme()) return;

        // Ensure title bar reflects RadianceStudio brand once main window is visible
        SetStudioWindowTitle();

        if (!_initialWorkspaceConfigured)
        {
            _initialWorkspaceConfigured = true;
            if (RhinoDoc.ActiveDoc != null)
            {
                ConfigureJewelryDocument(RhinoDoc.ActiveDoc);
            }
            OpenStudioWorkspace();
        }
    }

    #endregion
}
