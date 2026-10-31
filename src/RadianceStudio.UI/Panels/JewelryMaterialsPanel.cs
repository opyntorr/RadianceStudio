namespace RadianceStudio.UI.Panels;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Left-docked commercial jewelry Materials library panel.
/// Provides 1-click precious metal and gemstone material assignment to selected CAD models.
/// </summary>
public class JewelryMaterialsPanel : Panel
{
    public const int DefaultPanelWidth = 280;

    public event EventHandler<string>? CommandRequested;
    public event EventHandler<string>? MaterialSelected;

    public record MaterialSwatch(string Name, Color Color, string Category);

    public static readonly List<MaterialSwatch> Swatches = new()
    {
        // Precious Metals
        new("14K Yellow Gold", Color.FromArgb(0xE5, 0xC1, 0x58), "Metal"),
        new("18K Yellow Gold", Color.FromArgb(0xFF, 0xD7, 0x00), "Metal"),
        new("White Gold", Color.FromArgb(0xE0, 0xE4, 0xE8), "Metal"),
        new("Rose Gold", Color.FromArgb(0xE8, 0x9A, 0x82), "Metal"),
        new("Platinum 950", Color.FromArgb(0xD8, 0xDF, 0xE7), "Metal"),
        new("Silver 925", Color.FromArgb(0xEA, 0xEA, 0xEA), "Metal"),

        // Gemstones
        new("Diamond", Color.FromArgb(0xA6, 0xF0, 0xFF), "Gem"),
        new("Ruby", Color.FromArgb(0xE0, 0x11, 0x5F), "Gem"),
        new("Sapphire", Color.FromArgb(0x0F, 0x52, 0xBA), "Gem"),
        new("Emerald", Color.FromArgb(0x50, 0xC8, 0x78), "Gem"),
        new("Amethyst", Color.FromArgb(0x99, 0x66, 0xCC), "Gem"),
        new("Aquamarine", Color.FromArgb(0x7F, 0xFD, 0xD4), "Gem")
    };

    public JewelryMaterialsPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        var stack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Padding = new Padding(6, 4)
        };

        var title = new Label
        {
            Text = "Materials",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };
        stack.Items.Add(title);

        // Grid of material swatches (6 per row, 2 rows)
        var grid = new TableLayout
        {
            Spacing = new Size(4, 4),
            Padding = new Padding(2)
        };

        var row1 = new TableRow();
        for (int i = 0; i < 6; i++)
        {
            row1.Cells.Add(new TableCell(CreateSwatchButton(Swatches[i]), true));
        }
        grid.Rows.Add(row1);

        var row2 = new TableRow();
        for (int i = 6; i < 12; i++)
        {
            row2.Cells.Add(new TableCell(CreateSwatchButton(Swatches[i]), true));
        }
        grid.Rows.Add(row2);

        stack.Items.Add(grid);

        // Action buttons: Apply to Selection / Apply to Layer
        var btnApplySelection = new Button
        {
            Text = "Apply to Selection",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 24
        };
        btnApplySelection.Click += (s, e) => CommandRequested?.Invoke(this, "! _ApplyMaterialToSelection");

        var btnApplyLayer = new Button
        {
            Text = "Apply to Layer",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 24
        };
        btnApplyLayer.Click += (s, e) => CommandRequested?.Invoke(this, "! _ApplyMaterialToLayer");

        var actionsRow = new TableLayout
        {
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(new TableCell(btnApplySelection, true), new TableCell(btnApplyLayer, true))
            }
        };
        stack.Items.Add(actionsRow);

        Content = stack;
        JewelryDarkTheme.ApplyTheme(this);
    }

    private Control CreateSwatchButton(MaterialSwatch swatch)
    {
        var btn = new Button
        {
            BackgroundColor = swatch.Color,
            Width = 36,
            Height = 24,
            ToolTip = $"{swatch.Name} ({swatch.Category})"
        };

        btn.Click += (s, e) =>
        {
            MaterialSelected?.Invoke(this, swatch.Name);
            CommandRequested?.Invoke(this, $"! _-ApplyMaterial \"{swatch.Name}\" _Enter");
        };

        return btn;
    }
}
