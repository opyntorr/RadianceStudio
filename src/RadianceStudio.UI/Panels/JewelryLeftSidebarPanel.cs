namespace RadianceStudio.UI.Panels;

using System;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Unified left-docked commercial sidebar panel hosting all 4 left workspace sections:
/// Quick Commands, Projects, 4-Column Jewelry Layers, and Display Modes.
/// Guarantees 100% docked cohesion without any floating windows in viewports.
/// </summary>
public class JewelryLeftSidebarPanel : Panel
{
    public const int DefaultPanelWidth = 280;

    public event EventHandler<string>? CommandRequested;

    public JewelryLeftSidebarPanel()
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        var stack = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Padding(4)
        };

        // Section 1: Quick Commands
        var quickCmds = new QuickCommandsPanel();
        quickCmds.CommandRequested += (s, cmd) => CommandRequested?.Invoke(this, cmd);
        stack.Items.Add(quickCmds);

        // Section 2: Projects
        stack.Items.Add(CreateProjectsSection());

        // Section 3: Jewelry Layers (4 Columns: Metal, Gem, Object, User)
        var layers = new JewelryLayersPanel();
        layers.CommandRequested += (s, cmd) => CommandRequested?.Invoke(this, cmd);
        stack.Items.Add(layers);

        // Section 4: Display Modes
        var displayModes = new DisplayModesPanel();
        displayModes.CommandRequested += (s, cmd) => CommandRequested?.Invoke(this, cmd);
        stack.Items.Add(displayModes);

        var scrollable = new Scrollable
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            Content = stack,
            Border = BorderType.None
        };

        Content = scrollable;
        JewelryDarkTheme.ApplyTheme(this);
    }

    private Control CreateProjectsSection()
    {
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
            Padding = new Padding(8, 4),
            Rows =
            {
                new TableRow(new TableCell(title, true), new TableCell(btnNewProject, false))
            }
        };

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
        btnJobBag.Click += (s, e) => CommandRequested?.Invoke(this, "! _Notes");

        var actionsRow = new TableLayout
        {
            Padding = new Padding(8, 4),
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(new TableCell(btnFullSave, true), new TableCell(btnJobBag, true))
            }
        };

        var projectsPanel = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Items =
            {
                new Panel { BackgroundColor = JewelryDarkTheme.SurfaceElevated, Content = headerRow },
                actionsRow
            }
        };

        return new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Content = projectsPanel
        };
    }
}
