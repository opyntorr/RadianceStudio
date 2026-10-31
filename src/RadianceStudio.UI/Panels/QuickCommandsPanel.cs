namespace RadianceStudio.UI.Panels;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Left-docked Quick Commands grid panel providing instantaneous 1-click access
/// to the most frequently used CAD modeling primitives and transformations.
/// </summary>
public class QuickCommandsPanel : Panel
{
    public const int DefaultPanelWidth = 240;

    public event EventHandler<string>? CommandRequested;

    public QuickCommandsPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        var header = new Label
        {
            Text = "Quick Commands",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };

        var headerPanel = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Padding = new Padding(8, 6),
            Content = header
        };

        var grid = new TableLayout
        {
            Padding = new Padding(6),
            Spacing = new Size(4, 4)
        };

        var quickTools = new (string Name, string Macro, string Tooltip)[]
        {
            ("Line", "! _Line", "2-Point Line"),
            ("Poly", "! _Polyline", "Polyline"),
            ("Arc", "! _Arc", "3-Point Arc"),
            ("Circle", "! _Circle", "Center Radius Circle"),
            ("Rect", "! _Rectangle", "Corner Rectangle"),
            ("Extrude", "! _ExtrudeCrv", "Extrude Planar Curve"),
            ("Trim", "! _Trim", "Trim Objects"),
            ("Split", "! _Split", "Split Objects"),
            ("Join", "! _Join", "Join Objects"),
            ("Explode", "! _Explode", "Explode Polysurface/Curve"),
            ("Move", "! _Move", "3D Translation"),
            ("Rotate", "! _Rotate", "2D Rotation"),
            ("Scale", "! _Scale", "3D Uniform Scale"),
            ("Copy", "! _Copy", "Duplicate Translation"),
            ("Mirror", "! _Mirror", "Reflect Across Axis"),
            ("Group", "! _Group", "Group Objects"),
            ("Ungroup", "! _Ungroup", "Ungroup Selected"),
            ("Hide", "! _Hide", "Hide Selected Objects")
        };

        // Render as 6 rows of 3 buttons
        int colCount = 3;
        for (int r = 0; r < 6; r++)
        {
            var row = new TableRow();
            for (int c = 0; c < colCount; c++)
            {
                int idx = r * colCount + c;
                if (idx < quickTools.Length)
                {
                    var tool = quickTools[idx];
                    var btn = new Button
                    {
                        Text = tool.Name,
                        Font = JewelryDarkTheme.SmallFont,
                        BackgroundColor = JewelryDarkTheme.SurfaceElevated,
                        TextColor = JewelryDarkTheme.TextPrimary,
                        Height = 28,
                        ToolTip = $"{tool.Name}: {tool.Tooltip} ({tool.Macro})"
                    };

                    string macro = tool.Macro;
                    btn.Click += (s, e) => CommandRequested?.Invoke(this, macro);
                    row.Cells.Add(new TableCell(btn, true));
                }
                else
                {
                    row.Cells.Add(new TableCell(new Panel(), true));
                }
            }
            grid.Rows.Add(row);
        }

        var mainLayout = new TableLayout
        {
            Rows =
            {
                new TableRow(headerPanel),
                new TableRow(new TableCell(grid, true))
            }
        };

        Content = mainLayout;
        JewelryDarkTheme.ApplyTheme(this);
    }
}
