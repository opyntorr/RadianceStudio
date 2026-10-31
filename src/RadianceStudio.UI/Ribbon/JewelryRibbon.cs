namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Top horizontal commercial ribbon bar (Height = 105) hosting the 16 workflow tabs
/// in exact structural parity with the reference design:
/// Curve, Surface, Solid, Tools, Gems, Settings, Cutters, Transform, Clayoo, Render,
/// View, Mesh, Dimension, Analyze, Misc, Manufacturing.
/// Features colored indicator dots, real-time command search bar, and tool flyouts.
/// </summary>
public class JewelryRibbon : Panel
{
    public const int RibbonHeight = 105;

    public event EventHandler<string>? ActionTriggered;
    public event EventHandler<string>? CommandRequested;

    private readonly TabControl _tabControl;
    private readonly TopSearchBar _searchBar;

    public JewelryRibbon()
    {
        Height = RibbonHeight;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        _tabControl = new TabControl
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark
        };

        // Populate all 16 tabs in exact order
        foreach (var tabDef in RibbonToolCatalog.Tabs)
        {
            _tabControl.Pages.Add(CreateTab(tabDef));
        }

        // Top Search Bar
        _searchBar = new TopSearchBar();
        _searchBar.ToolSelected += OnSearchToolSelected;

        // Assembly: TabControl scaled with SearchBar docked in header row
        var headerLayout = new TableLayout
        {
            Padding = new Padding(4, 2),
            Spacing = new Size(8, 0),
            Rows =
            {
                new TableRow(
                    new TableCell(_tabControl, true),
                    new TableCell(_searchBar, false)
                )
            }
        };

        Content = headerLayout;
        JewelryDarkTheme.ApplyTheme(this);
    }

    public int SelectedTabIndex
    {
        get => _tabControl.SelectedIndex;
        set
        {
            if (value >= 0 && value < _tabControl.Pages.Count)
            {
                _tabControl.SelectedIndex = value;
            }
        }
    }

    public void SelectTab(string tabId)
    {
        for (int i = 0; i < _tabControl.Pages.Count; i++)
        {
            if (string.Equals(_tabControl.Pages[i].Text, tabId, StringComparison.OrdinalIgnoreCase))
            {
                _tabControl.SelectedIndex = i;
                break;
            }
        }
    }

    private TabPage CreateTab(RibbonTabDefinition tabDef)
    {
        var page = new TabPage
        {
            Text = tabDef.Title,
            Image = RibbonVectorIcons.GetTabDotIcon(tabDef.IndicatorColor, 12),
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            ToolTip = tabDef.Description
        };

        var toolsLayout = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Padding = new Padding(6, 4),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var tools = RibbonToolCatalog.GetToolsForTab(tabDef.Id);
        foreach (var tool in tools)
        {
            toolsLayout.Items.Add(CreateToolButton(tool));
        }

        // Right side utility icons (Settings, Help, Dynamic Toggle)
        var utilLayout = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Padding = new Padding(2),
            Items =
            {
                CreateIconButton(RibbonVectorIcons.GetGearIcon(16), "! _Options", "Application Settings"),
                CreateIconButton(RibbonVectorIcons.GetHelpIcon(16), "! _Help", "Help and Documentation"),
                CreateIconButton(RibbonVectorIcons.GetDynamicToggleIcon(16), "_RadToggleTreeMode", "Toggle Parametric Dynamic Mode")
            }
        };

        var scrollable = new Scrollable
        {
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Content = toolsLayout,
            Border = BorderType.None
        };

        var pageLayout = new TableLayout
        {
            Rows =
            {
                new TableRow(
                    new TableCell(scrollable, true),
                    new TableCell(utilLayout, false)
                )
            }
        };

        page.Content = pageLayout;
        return page;
    }

    private Control CreateToolButton(ToolDefinition tool)
    {
        var btn = new Button
        {
            Width = 62,
            Height = 68,
            Text = FormatButtonText(tool.Name),
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = tool.IsImplemented ? JewelryDarkTheme.SurfaceElevated : Color.FromArgb(0x1C, 0x1C, 0x20),
            TextColor = tool.IsImplemented ? JewelryDarkTheme.TextPrimary : Color.FromArgb(0x72, 0x72, 0x7A),
            Image = RibbonVectorIcons.GetToolIcon(tool, 30),
            ImagePosition = ButtonImagePosition.Above,
            ToolTip = tool.IsImplemented
                ? $"{tool.Name}\n{tool.Description}\nComando: {tool.CommandOrMacro}"
                : $"{tool.Name} [Próximamente / CAD Base]\n{tool.Description}\nEjecuta: {tool.CommandOrMacro}"
        };

        btn.Click += (s, e) => OnToolClicked(tool, btn);
        return btn;
    }

    private Control CreateIconButton(Image? icon, string command, string tooltip)
    {
        var btn = new Button
        {
            Width = 22,
            Height = 22,
            Image = icon,
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            ToolTip = tooltip
        };

        btn.Click += (s, e) => CommandRequested?.Invoke(this, command);
        return btn;
    }

    private void OnToolClicked(ToolDefinition tool, Button sourceButton)
    {
        if (tool.HasGroupBadge && !string.IsNullOrEmpty(tool.GroupKey))
        {
            var group = RibbonToolCatalog.GetToolGroup(tool.GroupKey);
            if (group != null && group.SubTools.Count > 0)
            {
                ShowToolGroupMenu(group, sourceButton);
                return;
            }
        }

        // Direct command execution
        ActionTriggered?.Invoke(this, tool.CommandOrMacro);
        CommandRequested?.Invoke(this, tool.CommandOrMacro);
    }

    private void ShowToolGroupMenu(ToolGroupDefinition group, Button sourceButton)
    {
        var menu = new ContextMenu();
        foreach (var subTool in group.SubTools)
        {
            var item = new ButtonMenuItem
            {
                Text = subTool.Name,
                ToolTip = subTool.Description
            };

            var target = subTool;
            item.Click += (s, e) =>
            {
                ActionTriggered?.Invoke(this, target.CommandOrMacro);
                CommandRequested?.Invoke(this, target.CommandOrMacro);
            };

            menu.Items.Add(item);
        }

        try
        {
            menu.Show(sourceButton, new PointF(0, sourceButton.Height));
        }
        catch
        {
            // Context menu show might fail if control is detached in test harness
        }
    }

    private void OnSearchToolSelected(object? sender, ToolDefinition tool)
    {
        SelectTab(tool.TabId);
        ActionTriggered?.Invoke(this, tool.CommandOrMacro);
        CommandRequested?.Invoke(this, tool.CommandOrMacro);
    }

    private static string FormatButtonText(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        if (name.Length <= 10) return name;

        // Truncate or break words gracefully for 2-row button label
        int spaceIdx = name.IndexOf(' ');
        if (spaceIdx > 0 && spaceIdx < 10)
        {
            return name.Substring(0, spaceIdx) + "\n" + name.Substring(spaceIdx + 1);
        }

        return name.Length > 12 ? name.Substring(0, 10) + "…" : name;
    }
}
