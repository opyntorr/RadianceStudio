namespace RadianceStudio.UI.Panels;

using System;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Dedicated left-docked commercial Projects management panel.
/// Provides 1-click new project creation, full saving, and job bag asset management.
/// </summary>
public class ProjectsPanel : Panel
{
    public const int DefaultPanelWidth = 280;

    public event EventHandler<string>? CommandRequested;

    public ProjectsPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        var stack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            Padding = new Padding(6, 4)
        };

        // Header with Title and + New Project button
        var title = new Label
        {
            Text = "Projects",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };

        var btnNewProject = new Button
        {
            Text = "+ New Project",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 24
        };
        btnNewProject.Click += (s, e) => CommandRequested?.Invoke(this, "! _New");

        var headerRow = new TableLayout
        {
            Padding = new Padding(4, 2),
            Rows =
            {
                new TableRow(new TableCell(title, true), new TableCell(btnNewProject, false))
            }
        };
        stack.Items.Add(headerRow);

        // Action buttons: Full Save & Job Bag
        var btnFullSave = new Button
        {
            Text = "Full Save",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 26
        };
        btnFullSave.Click += (s, e) => CommandRequested?.Invoke(this, "! _Save");

        var btnJobBag = new Button
        {
            Text = "Job Bag",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Height = 26
        };
        btnJobBag.Click += (s, e) => CommandRequested?.Invoke(this, "! _Open");

        var buttonsRow = new TableLayout
        {
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(new TableCell(btnFullSave, true), new TableCell(btnJobBag, true))
            }
        };
        stack.Items.Add(buttonsRow);

        // Job Bag Preview Tile
        var tile = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Height = 48,
            Padding = new Padding(8, 6)
        };

        var tileLabel = new Label
        {
            Text = "Active Job: Blank Project\nJewelry Tolerances: 0.001 mm",
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary
        };
        tile.Content = tileLabel;
        stack.Items.Add(tile);

        Content = stack;
        JewelryDarkTheme.ApplyTheme(this);
    }
}
