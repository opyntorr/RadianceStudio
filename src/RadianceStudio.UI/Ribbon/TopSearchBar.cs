namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.UI.Theme;

/// <summary>
/// Top-level command search bar positioned in the top header.
/// Provides real-time fuzzy filtering across all 16 ribbon tabs,
/// autocomplete suggestion flyouts, and 1-click command execution.
/// </summary>
public class TopSearchBar : Panel
{
    public const int DefaultWidth = 220;
    public const int DefaultHeight = 28;

    public event EventHandler<ToolDefinition>? ToolSelected;

    private readonly TextBox _searchBox;
    private ContextMenu? _suggestionMenu;

    public TopSearchBar()
    {
        Width = DefaultWidth;
        Height = DefaultHeight;
        BackgroundColor = JewelryDarkTheme.SurfaceCard;

        _searchBox = new TextBox
        {
            PlaceholderText = "Search commands...",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary,
            Width = DefaultWidth - 10,
            Height = DefaultHeight - 4
        };

        _searchBox.TextChanged += OnSearchTextChanged;
        _searchBox.KeyDown += OnSearchKeyDown;

        var layout = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Padding(4, 2),
            Items = { _searchBox }
        };

        Content = layout;
    }

    public string Text
    {
        get => _searchBox.Text ?? string.Empty;
        set => _searchBox.Text = value;
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        string query = _searchBox.Text?.Trim() ?? string.Empty;
        if (query.Length < 2)
        {
            _suggestionMenu = null;
            return;
        }

        var results = RibbonToolCatalog.Search(query).Take(12).ToList();
        if (results.Count == 0)
        {
            _suggestionMenu = null;
            return;
        }

        ShowSuggestionMenu(results);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Escape)
        {
            _searchBox.Text = string.Empty;
            _suggestionMenu = null;
            e.Handled = true;
        }
        else if (e.Key == Keys.Enter)
        {
            string query = _searchBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(query))
            {
                var topMatch = RibbonToolCatalog.Search(query).FirstOrDefault();
                if (topMatch != null)
                {
                    ExecuteTool(topMatch);
                    e.Handled = true;
                }
            }
        }
    }

    private void ShowSuggestionMenu(List<ToolDefinition> results)
    {
        _suggestionMenu = new ContextMenu();

        foreach (var tool in results)
        {
            string badge = tool.HasLightningBadge ? "⚡ " : (tool.HasGroupBadge ? "◢ " : "");
            var item = new ButtonMenuItem
            {
                Text = $"{badge}{tool.Name}  [{tool.TabId}]"
            };

            var selectedTool = tool;
            item.Click += (s, e) => ExecuteTool(selectedTool);
            _suggestionMenu.Items.Add(item);
        }

        try
        {
            _suggestionMenu.Show(_searchBox, new PointF(0, _searchBox.Height + 2));
        }
        catch
        {
            // Context menu show might fail if control is detached in test harness
        }
    }

    private void ExecuteTool(ToolDefinition tool)
    {
        _searchBox.Text = string.Empty;
        _suggestionMenu = null;
        ToolSelected?.Invoke(this, tool);
    }
}
