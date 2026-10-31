namespace RadianceStudio.Rhino.Panels;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.DocObjects;
using global::Rhino.Geometry;
using RadianceStudio.UI.Panels;

/// <summary>
/// Dockable Rhino Eto panel hosting the dedicated Jewelry Properties inspector.
/// Hooks Rhino document selection lifecycle to compute live geometry classification,
/// active layer, material, and estimated 14K gold mass (density: 13.07 g/cm³).
/// </summary>
[Guid("51C420B1-18F9-4672-91DE-4A47E7D99006")]
public class JewelryPropertiesPanelHost : JewelryPropertiesPanel
{
    public static readonly Guid PanelId = new("51C420B1-18F9-4672-91DE-4A47E7D99006");
    public static JewelryPropertiesPanelHost? Instance { get; private set; }

    public JewelryPropertiesPanelHost() : base()
    {
        Instance = this;

        try
        {
            RhinoDoc.SelectObjects += OnSelectionChanged;
            RhinoDoc.DeselectObjects += OnSelectionChanged;
            RhinoDoc.DeselectAllObjects += OnDeselectAll;
        }
        catch
        {
        }
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
    {
        RefreshProperties();
    }

    private void OnDeselectAll(object? sender, RhinoDeselectAllObjectsEventArgs e)
    {
        UpdateProperties("-", 0, "-", "-", 0.0);
    }

    public void RefreshProperties()
    {
        try
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null)
            {
                UpdateProperties("-", 0, "-", "-", 0.0);
                return;
            }

            var selected = doc.Objects.GetSelectedObjects(false, false).ToList();
            if (selected.Count == 0)
            {
                UpdateProperties("-", 0, "-", "-", 0.0);
                return;
            }

            int count = selected.Count;
            string type = count == 1 ? selected[0].ObjectType.ToString() : $"{count} Objects";

            var first = selected[0];
            string layerName = doc.Layers[first.Attributes.LayerIndex]?.Name ?? "Default";
            string materialName = "Default Metal";

            // Compute volume & estimated weight in grams (14K Gold = 13.07 g/cm³)
            double totalVolumeMm3 = 0.0;
            foreach (var obj in selected)
            {
                if (obj.Geometry is Brep brep && brep.IsSolid)
                {
                    var massProps = VolumeMassProperties.Compute(brep);
                    if (massProps != null)
                    {
                        totalVolumeMm3 += massProps.Volume;
                    }
                }
                else if (obj.Geometry is Mesh mesh && mesh.IsClosed)
                {
                    var massProps = VolumeMassProperties.Compute(mesh);
                    if (massProps != null)
                    {
                        totalVolumeMm3 += massProps.Volume;
                    }
                }
            }

            double volumeCm3 = totalVolumeMm3 / 1000.0;
            double weightGrams = volumeCm3 * 13.07; // Density of 14K Gold

            UpdateProperties(type, count, layerName, materialName, weightGrams, "14K");
        }
        catch
        {
            UpdateProperties("-", 0, "-", "-", 0.0);
        }
    }
}
