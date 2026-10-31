namespace RadianceStudio.UI.Panels;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Dedicated 4-column jewelry layers manager panel:
/// Metal, Gem, Object, and User category stacks with 1-click visibility (eye),
/// lock toggles, and color chip indicators matching the commercial jewelry CAD reference.
/// </summary>
public class JewelryLayersPanel : Panel
{
    public const int DefaultPanelWidth = 260;

    public event EventHandler<(string LayerName, bool IsVisible)>? LayerVisibilityChanged;
    public event EventHandler<(string LayerName, bool IsLocked)>? LayerLockChanged;
    public event EventHandler<string>? CommandRequested;

    private readonly TableLayout _layersGrid;

    public JewelryLayersPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        // 1. Header
        var title = new Label
        {
            Text = "Layers",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };

        var btnHideAll = new Button
        {
            Text = "Ø Hide All",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 24
        };
        btnHideAll.Click += (s, e) => CommandRequested?.Invoke(this, "! -_Layer _Off * _Enter");

        var btnShowAll = new Button
        {
            Text = "👁 Show All",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 24
        };
        btnShowAll.Click += (s, e) => CommandRequested?.Invoke(this, "! -_Layer _On * _Enter");

        var headerTable = new TableLayout
        {
            Padding = new Padding(8, 6),
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(
                    new TableCell(title, true),
                    new TableCell(btnHideAll, false),
                    new TableCell(btnShowAll, false)
                )
            }
        };

        var headerPanel = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Content = headerTable
        };

        // 2. 4-Category Columns Header
        var categoryHeader = new TableLayout
        {
            Padding = new Padding(8, 4),
            Spacing = new Size(4, 0),
            Rows =
            {
                new TableRow(
                    new TableCell(CreateCategoryLabel("Metal", Color.FromArgb(0xFF, 0xD7, 0x00)), true),
                    new TableCell(CreateCategoryLabel("Gem", Color.FromArgb(0x00, 0xE5, 0xFF)), true),
                    new TableCell(CreateCategoryLabel("Object", Color.FromArgb(0xBA, 0x4E, 0x9B)), true),
                    new TableCell(CreateCategoryLabel("User", Color.FromArgb(0xE0, 0x73, 0x43)), true)
                )
            }
        };

        // 3. Layer Rows
        _layersGrid = new TableLayout
        {
            Padding = new Padding(6),
            Spacing = new Size(4, 3)
        };

        PopulateDefaultLayers();

        var scrollable = new Scrollable
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            Content = _layersGrid,
            Border = BorderType.None
        };

        // 4. Footer
        var btnMore = new Button
        {
            Text = "▼ Show Additional Layers",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Height = 26
        };
        btnMore.Click += (s, e) => CommandRequested?.Invoke(this, "! _Layer");

        var footerPanel = new Panel
        {
            Padding = new Padding(6, 4),
            Content = btnMore
        };

        var mainLayout = new TableLayout
        {
            Rows =
            {
                new TableRow(headerPanel),
                new TableRow(categoryHeader),
                new TableRow(new TableCell(scrollable, true)) { ScaleHeight = true },
                new TableRow(footerPanel)
            }
        };

        Content = mainLayout;
        JewelryDarkTheme.ApplyTheme(this);
    }

    private static Label CreateCategoryLabel(string text, Color color)
    {
        return new Label
        {
            Text = text,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = color,
            TextAlignment = TextAlignment.Center
        };
    }

    private void PopulateDefaultLayers()
    {
        var layers = new[]
        {
            ("Yellow Gold", Color.FromArgb(0xD4, 0xAF, 0x37), "Metal"),
            ("White Gold", Color.FromArgb(0xE0, 0xE0, 0xE5), "Metal"),
            ("Rose Gold", Color.FromArgb(0xB7, 0x6E, 0x79), "Metal"),
            ("Platinum", Color.FromArgb(0xC5, 0xC6, 0xC7), "Metal"),
            ("Diamond", Color.FromArgb(0x00, 0xE5, 0xFF), "Gem"),
            ("Ruby", Color.FromArgb(0xDC, 0x14, 0x3C), "Gem"),
            ("Sapphire", Color.FromArgb(0x0F, 0x52, 0xBA), "Gem"),
            ("Emerald", Color.FromArgb(0x50, 0xC8, 0x78), "Gem"),
            ("Cutters", Color.FromArgb(0xFF, 0x8C, 0x00), "Object"),
            ("Profiles", Color.FromArgb(0x99, 0x32, 0xCC), "Object"),
            ("Curves", Color.FromArgb(0x1E, 0x90, 0xFF), "Object"),
            ("Support", Color.FromArgb(0xD2, 0xB4, 0x8C), "Object"),
            ("User 1", Color.FromArgb(0xFF, 0xBF, 0x00), "User"),
            ("User 2", Color.FromArgb(0x20, 0xB2, 0xAA), "User"),
            ("User 3", Color.FromArgb(0xFF, 0x69, 0xB4), "User"),
            ("User 4", Color.FromArgb(0x7F, 0xFF, 0x00), "User")
        };

        foreach (var layer in layers)
        {
            _layersGrid.Rows.Add(CreateLayerRow(layer.Item1, layer.Item2, layer.Item3));
        }
    }

    private TableRow CreateLayerRow(string name, Color color, string category)
    {
        bool isVisible = true;
        bool isLocked = false;

        var swatch = new Panel
        {
            Width = 14,
            Height = 14,
            BackgroundColor = color
        };

        var label = new Label
        {
            Text = name,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };

        var btnEye = new Button
        {
            Text = "👁",
            Font = JewelryDarkTheme.SmallFont,
            Width = 24,
            Height = 22,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            ToolTip = $"Toggle {name} visibility"
        };

        btnEye.Click += (s, e) =>
        {
            isVisible = !isVisible;
            btnEye.Text = isVisible ? "👁" : "Ø";
            btnEye.TextColor = isVisible ? JewelryDarkTheme.TextPrimary : Color.FromArgb(0x75, 0x75, 0x75);
            LayerVisibilityChanged?.Invoke(this, (name, isVisible));
            CommandRequested?.Invoke(this, $"! -_Layer {(isVisible ? "_On" : "_Off")} \"{name}\" _Enter");
        };

        var btnLock = new Button
        {
            Text = "🔓",
            Font = JewelryDarkTheme.SmallFont,
            Width = 24,
            Height = 22,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            ToolTip = $"Toggle {name} lock"
        };

        btnLock.Click += (s, e) =>
        {
            isLocked = !isLocked;
            btnLock.Text = isLocked ? "🔒" : "🔓";
            btnLock.TextColor = isLocked ? JewelryDarkTheme.AccentGold : JewelryDarkTheme.TextPrimary;
            LayerLockChanged?.Invoke(this, (name, isLocked));
            CommandRequested?.Invoke(this, $"! -_Layer {(isLocked ? "_Lock" : "_Unlock")} \"{name}\" _Enter");
        };

        return new TableRow(
            new TableCell(btnEye, false),
            new TableCell(btnLock, false),
            new TableCell(swatch, false),
            new TableCell(label, true)
        );
    }
}
