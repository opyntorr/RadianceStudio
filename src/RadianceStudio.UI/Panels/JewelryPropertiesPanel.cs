namespace RadianceStudio.UI.Panels;

using System;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Right-docked commercial jewelry CAD Properties inspector.
/// Displays real-time object geometry classification, selection counts, active layer, material,
/// and estimated precious metal weight in grams (14K, 18K, Plat, Silver).
/// </summary>
public class JewelryPropertiesPanel : Panel
{
    public const int DefaultPanelWidth = 320;

    private readonly Label _typeValue;
    private readonly Label _countValue;
    private readonly Label _layerValue;
    private readonly Label _materialValue;
    private readonly Label _weightValue;

    public JewelryPropertiesPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        var stack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            Padding = new Padding(10, 8)
        };

        var title = new Label
        {
            Text = "Properties",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };
        stack.Items.Add(title);

        var table = new TableLayout
        {
            Spacing = new Size(8, 6),
            Padding = new Padding(4)
        };

        _typeValue = CreateValueLabel("-");
        _countValue = CreateValueLabel("0");
        _layerValue = CreateValueLabel("-");
        _materialValue = CreateValueLabel("-");
        _weightValue = CreateValueLabel("0.00 g (14K)");

        table.Rows.Add(CreatePropertyRow("Type", _typeValue));
        table.Rows.Add(CreatePropertyRow("Count", _countValue));
        table.Rows.Add(CreatePropertyRow("Layer", _layerValue));
        table.Rows.Add(CreatePropertyRow("Material", _materialValue));
        table.Rows.Add(CreatePropertyRow("Weight", _weightValue));

        stack.Items.Add(table);

        Content = stack;
        JewelryDarkTheme.ApplyTheme(this);
    }

    private static Label CreateValueLabel(string initialText)
    {
        return new Label
        {
            Text = initialText,
            Font = JewelryDarkTheme.BodyFont,
            TextColor = JewelryDarkTheme.TextPrimary
        };
    }

    private static TableRow CreatePropertyRow(string labelText, Label valueLabel)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary,
            Width = 80
        };

        return new TableRow(new TableCell(lbl, false), new TableCell(valueLabel, true));
    }

    public void UpdateProperties(string type, int count, string layer, string material, double weightGrams, string metalStandard = "14K")
    {
        _typeValue.Text = string.IsNullOrEmpty(type) ? "-" : type;
        _countValue.Text = count.ToString();
        _layerValue.Text = string.IsNullOrEmpty(layer) ? "-" : layer;
        _materialValue.Text = string.IsNullOrEmpty(material) ? "-" : material;
        _weightValue.Text = count > 0 ? $"{weightGrams:F2} g ({metalStandard})" : "0.00 g";
    }
}
