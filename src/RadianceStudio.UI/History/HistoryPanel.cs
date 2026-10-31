namespace RadianceStudio.UI.History;

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.UI.Inspector;
using RadianceStudio.UI.Theme;
using RadianceStudio.UI.ViewModels;
using Rhino;
using Rhino.DocObjects;

/// <summary>
/// Dual-mode parametric history and timeline panel toggled via _RadToggleTreeMode:
/// 1. StudioClassic: Flat, direct object-centric list view for fast jewelry styling.
/// 2. FeatureTree: Chronological SolidWorks-style timeline with creation order (OrderIndex),
///    draggable rollback bar setting ParametricGraphManager.RollbackIndex, and right-click context menu
///    for Suppress / Resume, Edit, and Delete operations.
/// </summary>
public class HistoryPanel : Panel
{
    public const int DefaultPanelWidth = 300;

    private readonly Panel _headerContainer;
    private readonly Label _titleLabel;
    private readonly Button _toggleModeButton;
    private readonly Panel _contentContainer;

    // StudioClassic view
    private readonly GridView _classicGridView;
    private readonly List<FeatureNodeItem> _classicDataStore = new();

    // FeatureTree view
    private readonly TreeGridView _featureTreeGridView;
    private readonly TreeGridItemCollection _treeDataStore = new();
    private readonly Panel _rollbackContainer;
    private readonly Slider _rollbackSlider;
    private readonly Label _rollbackLabel;

    private ParametricGraphManager _graphManager;
    private DynamicInspectorPanel? _inspectorPanel;

    public HistoryPanel(ParametricGraphManager? graphManager = null, DynamicInspectorPanel? inspectorPanel = null)
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;
        _graphManager = graphManager ?? new ParametricGraphManager();
        _inspectorPanel = inspectorPanel;

        // 1. Header with Mode Toggle
        _titleLabel = new Label
        {
            Text = "Dynamic Groups",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };

        _toggleModeButton = new Button
        {
            Text = "FeatureTree Mode",
            Font = JewelryDarkTheme.SmallFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.AccentCyan,
            Height = 26
        };
        _toggleModeButton.Click += (s, e) => ToggleTreeMode();

        var headerLayout = new TableLayout
        {
            Padding = new Padding(8, 6),
            Rows =
            {
                new TableRow(new TableCell(_titleLabel, true), new TableCell(_toggleModeButton, false))
            }
        };

        _headerContainer = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Content = headerLayout
        };

        // 2. StudioClassic GridView setup
        _classicGridView = new GridView
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            DataStore = _classicDataStore
        };
        _classicGridView.Columns.Add(new GridColumn
        {
            HeaderText = "Feature",
            DataCell = new TextBoxCell { Binding = Binding.Property<FeatureNodeItem, string>(x => x.Name) },
            Width = 140
        });
        _classicGridView.Columns.Add(new GridColumn
        {
            HeaderText = "Type",
            DataCell = new TextBoxCell { Binding = Binding.Property<FeatureNodeItem, string>(x => x.FeatureType) },
            Width = 80
        });
        _classicGridView.Columns.Add(new GridColumn
        {
            HeaderText = "Status",
            DataCell = new TextBoxCell { Binding = Binding.Property<FeatureNodeItem, string>(x => x.Status) },
            Width = 60
        });

        _classicGridView.SelectedItemsChanged += OnClassicItemSelected;

        // 3. FeatureTree TreeGridView setup
        _featureTreeGridView = new TreeGridView
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            DataStore = _treeDataStore
        };
        _featureTreeGridView.Columns.Add(new GridColumn
        {
            HeaderText = "#",
            DataCell = new TextBoxCell { Binding = Binding.Property<TreeGridItem, string>(x => (string)x.Values[0]) },
            Width = 35
        });
        _featureTreeGridView.Columns.Add(new GridColumn
        {
            HeaderText = "Feature Hierarchy",
            DataCell = new TextBoxCell { Binding = Binding.Property<TreeGridItem, string>(x => (string)x.Values[1]) },
            Width = 150
        });
        _featureTreeGridView.Columns.Add(new GridColumn
        {
            HeaderText = "Status",
            DataCell = new TextBoxCell { Binding = Binding.Property<TreeGridItem, string>(x => (string)x.Values[2]) },
            Width = 75
        });

        _featureTreeGridView.ContextMenu = CreateContextMenu();
        _featureTreeGridView.SelectedItemChanged += OnFeatureTreeItemSelected;

        // Double-click to edit feature parameters in Dynamic Inspector
        _classicGridView.CellDoubleClick += (s, e) =>
        {
            if (GetSelectedNodeId() is Guid id)
            {
                LoadFeatureIntoInspector(id);
                FocusInspectorPanel();
            }
        };

        _featureTreeGridView.CellDoubleClick += (s, e) =>
        {
            if (GetSelectedNodeId() is Guid id)
            {
                LoadFeatureIntoInspector(id);
                FocusInspectorPanel();
            }
        };

        // 4. Rollback Bar controls
        _rollbackLabel = new Label
        {
            Text = "Rollback Bar: End of Timeline",
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.AccentGoldBright
        };

        _rollbackSlider = new Slider
        {
            MinValue = 0,
            MaxValue = 10,
            Value = 10,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated
        };
        _rollbackSlider.ValueChanged += (s, e) =>
        {
            SetRollbackPosition(_rollbackSlider.Value);
        };

        var rollbackLayout = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Padding = new Padding(6),
            Items = { _rollbackLabel, _rollbackSlider }
        };

        _rollbackContainer = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Content = rollbackLayout,
            Visible = false
        };

        // Container
        _contentContainer = new Panel
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            Content = _classicGridView
        };

        var mainLayout = new TableLayout
        {
            Rows =
            {
                new TableRow(_headerContainer),
                new TableRow(new TableCell(_contentContainer, true)) { ScaleHeight = true },
                new TableRow(_rollbackContainer)
            }
        };

        Content = mainLayout;

        // Hook Rhino events
        HookRhinoEvents();

        // Initial view refresh
        RefreshHistory();
    }

    public ParametricMode CurrentMode
    {
        get => _graphManager.CurrentMode;
        set
        {
            if (_graphManager.CurrentMode != value)
            {
                _graphManager.CurrentMode = value;
                UpdateModeUI();
                RefreshHistory();
            }
        }
    }

    public ParametricGraphManager GraphManager
    {
        get => _graphManager;
        set
        {
            _graphManager = value;
            RefreshHistory();
        }
    }

    public DynamicInspectorPanel? InspectorPanel
    {
        get => _inspectorPanel;
        set => _inspectorPanel = value;
    }

    public void ToggleTreeMode()
    {
        CurrentMode = CurrentMode == ParametricMode.StudioClassic
            ? ParametricMode.FeatureTree
            : ParametricMode.StudioClassic;
    }

    public void SetRollbackPosition(int index)
    {
        int maxOrder = _graphManager.GetNodesInExecutionOrder().LastOrDefault()?.OrderIndex ?? 0;
        if (index >= maxOrder || index < 0)
        {
            _graphManager.ResetRollbackToEnd();
            _rollbackLabel.Text = "Rollback Bar: End of Timeline";
        }
        else
        {
            _graphManager.RollbackIndex = index;
            _rollbackLabel.Text = $"Rollback Bar: Frozen past #{index}";
        }

        RefreshHistory();

        try
        {
            RhinoDoc.ActiveDoc?.Views.Redraw();
        }
        catch
        {
        }
    }

    public void RefreshHistory()
    {
        var nodes = _graphManager.GetNodesInExecutionOrder();

        // 1. Refresh StudioClassic flat list
        _classicDataStore.Clear();
        foreach (var node in nodes)
        {
            string status = node.IsSuppressed ? "Suppressed" : "Active";
            _classicDataStore.Add(new FeatureNodeItem(node.Id, node.Name, node.FeatureType, status));
        }
        _classicGridView.DataStore = _classicDataStore.ToList();

        // 2. Refresh FeatureTree hierarchy
        _treeDataStore.Clear();
        var nodeMap = new Dictionary<Guid, TreeGridItem>();

        // Max order for rollback slider
        int maxOrder = nodes.LastOrDefault()?.OrderIndex ?? 0;
        _rollbackSlider.MaxValue = Math.Max(maxOrder, 1);
        if (_graphManager.RollbackIndex == int.MaxValue)
        {
            _rollbackSlider.Value = _rollbackSlider.MaxValue;
        }

        foreach (var node in nodes)
        {
            bool isRolledBack = node.OrderIndex > _graphManager.RollbackIndex;
            string status = node.IsSuppressed ? "[Suppressed]" : (isRolledBack ? "[Frozen]" : "OK");

            var item = new TreeGridItem
            {
                Values = new object[]
                {
                    $"#{node.OrderIndex}",
                    node.Name,
                    status
                },
                Tag = node.Id
            };

            nodeMap[node.Id] = item;

            // Check if node has parents in the tree
            bool attachedToParent = false;
            foreach (var pid in node.ParentIds)
            {
                if (nodeMap.TryGetValue(pid, out var parentItem))
                {
                    parentItem.Children.Add(item);
                    attachedToParent = true;
                    break;
                }
            }

            if (!attachedToParent)
            {
                _treeDataStore.Add(item);
            }
        }
    }

    private void UpdateModeUI()
    {
        if (CurrentMode == ParametricMode.StudioClassic)
        {
            _toggleModeButton.Text = "FeatureTree Mode";
            _titleLabel.Text = "Studio Classic History";
            _contentContainer.Content = _classicGridView;
            _rollbackContainer.Visible = false;
        }
        else
        {
            _toggleModeButton.Text = "StudioClassic Mode";
            _titleLabel.Text = "FeatureTree (Timeline)";
            _contentContainer.Content = _featureTreeGridView;
            _rollbackContainer.Visible = true;
        }
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var suppressItem = new ButtonMenuItem { Text = "Suppress / Resume" };
        suppressItem.Click += (s, e) =>
        {
            if (GetSelectedNodeId() is Guid id)
            {
                var node = _graphManager.GetNode(id);
                if (node != null)
                {
                    _graphManager.SetSuppressed(id, !node.IsSuppressed);
                    RefreshHistory();
                    try
                    {
                        RhinoDoc.ActiveDoc?.Views.Redraw();
                    }
                    catch
                    {
                    }
                }
            }
        };

        var editItem = new ButtonMenuItem { Text = "Edit Parameters" };
        editItem.Click += (s, e) =>
        {
            if (GetSelectedNodeId() is Guid id)
            {
                LoadFeatureIntoInspector(id);
            }
        };

        var deleteItem = new ButtonMenuItem { Text = "Delete Feature" };
        deleteItem.Click += (s, e) =>
        {
            if (GetSelectedNodeId() is Guid id)
            {
                _graphManager.UnregisterNode(id);
                try
                {
                    RhinoDoc.ActiveDoc?.Objects.Delete(id, true);
                    RhinoDoc.ActiveDoc?.Views.Redraw();
                }
                catch
                {
                }
                RefreshHistory();
            }
        };

        menu.Items.Add(suppressItem);
        menu.Items.Add(editItem);
        menu.Items.Add(new SeparatorMenuItem());
        menu.Items.Add(deleteItem);

        return menu;
    }

    private Guid? GetSelectedNodeId()
    {
        if (CurrentMode == ParametricMode.StudioClassic)
        {
            return _classicGridView.SelectedItem is FeatureNodeItem item ? item.Id : null;
        }
        else
        {
            return _featureTreeGridView.SelectedItem is TreeGridItem item && item.Tag is Guid id ? id : null;
        }
    }

    private void OnClassicItemSelected(object? sender, EventArgs e)
    {
        if (GetSelectedNodeId() is Guid id)
        {
            HighlightAndInspect(id);
        }
    }

    private void OnFeatureTreeItemSelected(object? sender, EventArgs e)
    {
        if (GetSelectedNodeId() is Guid id)
        {
            HighlightAndInspect(id);
        }
    }

    private void HighlightAndInspect(Guid objectId)
    {
        try
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc != null)
            {
                doc.Objects.UnselectAll();
                doc.Objects.Select(objectId);
                doc.Views.Redraw();
            }
        }
        catch
        {
        }
    }

    private void LoadFeatureIntoInspector(Guid objectId)
    {
        if (_inspectorPanel == null) return;

        try
        {
            var doc = RhinoDoc.ActiveDoc;
            var obj = doc?.Objects.FindId(objectId);
            if (obj?.Geometry != null)
            {
                var metadata = ParametricMetadata.Find(obj.Geometry);
                if (metadata != null && metadata.Parameters.TryGetString("FeatureType", out var featureType))
                {
                    IInspectorViewModel? vm = featureType switch
                    {
                        "Rail" => new RingRailViewModel(),
                        "Shank" => new ShankViewModel(),
                        "GemRow" => new GemOnCurveViewModel(),
                        "Setting" => metadata.Parameters.TryGetString("SettingType", out var st) && st == "Bezel"
                            ? new BezelViewModel()
                            : new ProngHeadViewModel(),
                        "Cutter" => new CutterViewModel(),
                        _ => null
                    };

                    if (vm != null)
                    {
                        vm.LoadFromMetadata(metadata, objectId);
                        _inspectorPanel.SetViewModel(vm);
                    }
                }
            }
        }
        catch
        {
        }
    }

    private void HookRhinoEvents()
    {
        try
        {
            RhinoDoc.AddRhinoObject += (s, e) => RefreshHistory();
            RhinoDoc.DeleteRhinoObject += (s, e) => RefreshHistory();
            RhinoDoc.UndeleteRhinoObject += (s, e) => RefreshHistory();
            RhinoDoc.ReplaceRhinoObject += (s, e) => RefreshHistory();
            RhinoDoc.SelectObjects += OnRhinoObjectsSelected;
        }
        catch
        {
        }
    }

    private void OnRhinoObjectsSelected(object? sender, RhinoObjectSelectionEventArgs e)
    {
        if (e.RhinoObjects == null || e.RhinoObjects.Length == 0) return;

        foreach (var obj in e.RhinoObjects)
        {
            if (obj?.Geometry == null) continue;

            var metadata = ParametricMetadata.Find(obj.Geometry);
            if (metadata != null)
            {
                // Switch focus to the Parametric History / Operation Tree tab
                try
                {
                    Rhino.UI.Panels.OpenPanel(new Guid("8965008E-EEA4-4A86-BFA3-A2D696417531"));
                }
                catch
                {
                }

                SelectNodeInViews(obj.Id);
                break;
            }
        }
    }

    public void SelectNodeInViews(Guid objectId)
    {
        if (CurrentMode == ParametricMode.StudioClassic)
        {
            int idx = _classicDataStore.FindIndex(x => x.Id == objectId);
            if (idx >= 0)
            {
                _classicGridView.SelectedRow = idx;
            }
        }
        else
        {
            int rowIdx = 0;
            foreach (var item in _treeDataStore)
            {
                if (item is TreeGridItem tgi && tgi.Tag is Guid id && id == objectId)
                {
                    _featureTreeGridView.SelectedRow = rowIdx;
                    break;
                }
                rowIdx++;
            }
        }
    }

    private static void FocusInspectorPanel()
    {
        try
        {
            Rhino.UI.Panels.OpenPanel(new Guid("552C1656-787D-4FCE-A263-AC0174092E47"));
        }
        catch
        {
        }
    }
}

/// <summary>
/// Data model item for the StudioClassic flat list view.
/// </summary>
public record FeatureNodeItem(Guid Id, string Name, string FeatureType, string Status);
