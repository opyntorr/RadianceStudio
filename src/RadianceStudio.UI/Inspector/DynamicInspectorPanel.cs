namespace RadianceStudio.UI.Inspector;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.Core.Display;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.UI.Ribbon;
using RadianceStudio.UI.Theme;
using RadianceStudio.UI.ViewModels;
using Rhino;
using Rhino.DocObjects;

/// <summary>
/// Right-docked reactive panel replacing Rhino command prompts with interactive sliders,
/// millimetric text steppers (0.01 mm resolution), and 60 FPS GPU viewport feedback via RadiancePreviewConduit.
/// Features a prominent Action Bar (Apply Enter / Cancel Esc) and automatic 1-click dynamic parameter reloads upon selection.
/// </summary>
public class DynamicInspectorPanel : Panel
{
    public const int DefaultPanelWidth = 320;

    private readonly Panel _headerContainer;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Scrollable _bodyScrollable;
    private readonly StackLayout _controlsLayout;
    private readonly Panel _actionBarContainer;
    private readonly Button _applyButton;
    private readonly Button _cancelButton;

    private IInspectorViewModel? _activeViewModel;
    private readonly RadiancePreviewConduit _previewConduit;
    private ParametricGraphManager _graphManager;

    public DynamicInspectorPanel(ParametricGraphManager? graphManager = null)
    {
        Width = DefaultPanelWidth;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;
        _graphManager = graphManager ?? new ParametricGraphManager();
        _previewConduit = new RadiancePreviewConduit();

        // 1. Header
        _titleLabel = new Label
        {
            Text = "Jewelry Inspector",
            Font = JewelryDarkTheme.HeaderFont,
            TextColor = JewelryDarkTheme.AccentGold
        };

        _subtitleLabel = new Label
        {
            Text = "Select an object or tool to inspect parameters",
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary
        };

        var headerLayout = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Items = { _titleLabel, _subtitleLabel }
        };

        _headerContainer = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            Padding = new Padding(10, 8),
            Content = headerLayout
        };

        // 2. Body
        _controlsLayout = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            Padding = new Padding(10)
        };

        _bodyScrollable = new Scrollable
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark,
            Content = _controlsLayout
        };

        // 3. Action Bar (Apply / Cancel)
        _applyButton = new Button
        {
            Text = "✔ Apply (Enter)",
            Font = JewelryDarkTheme.SubheaderFont,
            BackgroundColor = JewelryDarkTheme.ActionApply,
            TextColor = JewelryDarkTheme.TextPrimary,
            Height = 38,
            Image = RibbonVectorIcons.GetApplyIcon(18),
            ImagePosition = ButtonImagePosition.Left
        };
        _applyButton.Click += (s, e) => ApplyCurrent();

        _cancelButton = new Button
        {
            Text = "✖ Cancel (Esc)",
            Font = JewelryDarkTheme.SubheaderFont,
            BackgroundColor = JewelryDarkTheme.ActionCancel,
            TextColor = JewelryDarkTheme.TextPrimary,
            Height = 38,
            Image = RibbonVectorIcons.GetCancelIcon(18),
            ImagePosition = ButtonImagePosition.Left
        };
        _cancelButton.Click += (s, e) => CancelCurrent();

        var actionLayout = new TableLayout
        {
            Padding = new Padding(8),
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(new TableCell(_cancelButton, true), new TableCell(_applyButton, true))
            }
        };

        _actionBarContainer = new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Content = actionLayout,
            Visible = false
        };

        // Assembly
        var mainLayout = new TableLayout
        {
            Rows =
            {
                new TableRow(_headerContainer),
                new TableRow(new TableCell(_bodyScrollable, true)) { ScaleHeight = true },
                new TableRow(_actionBarContainer)
            }
        };

        Content = mainLayout;

        // Key bindings
        KeyDown += OnPanelKeyDown;

        // Hook Rhino selection listeners
        HookRhinoSelectionEvents();
    }

    public IInspectorViewModel? ActiveViewModel => _activeViewModel;

    public ParametricGraphManager GraphManager
    {
        get => _graphManager;
        set => _graphManager = value;
    }

    public RadiancePreviewConduit PreviewConduit => _previewConduit;

    public void SetViewModel(IInspectorViewModel viewModel)
    {
        if (_activeViewModel != null)
        {
            _activeViewModel.Cancel();
        }

        _activeViewModel = viewModel;
        _activeViewModel.Conduit = _previewConduit;
        _activeViewModel.GraphManager = _graphManager;

        _titleLabel.Text = _activeViewModel.Title;
        _subtitleLabel.Text = _activeViewModel.IsEditMode
            ? $"Editing parametric {_activeViewModel.FeatureType}"
            : $"Creating new {_activeViewModel.FeatureType}";

        _actionBarContainer.Visible = true;

        RebuildControlTree();
        _activeViewModel.UpdatePreview();
    }

    public void ApplyCurrent()
    {
        if (_activeViewModel == null) return;

        try
        {
            var doc = RhinoDoc.ActiveDoc;
            _activeViewModel.Apply(doc);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Error applying geometry: {ex.Message}");
        }

        _actionBarContainer.Visible = false;
        _titleLabel.Text = "Jewelry Inspector";
        _subtitleLabel.Text = "Parameters applied successfully";
        _controlsLayout.Items.Clear();
        _activeViewModel = null;
    }

    public void CancelCurrent()
    {
        if (_activeViewModel == null) return;

        try
        {
            _activeViewModel.Cancel();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Error cancelling geometry: {ex.Message}");
        }

        _actionBarContainer.Visible = false;
        _titleLabel.Text = "Jewelry Inspector";
        _subtitleLabel.Text = "Operation cancelled";
        _controlsLayout.Items.Clear();
        _activeViewModel = null;
    }

    private void OnPanelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Enter)
        {
            ApplyCurrent();
            e.Handled = true;
        }
        else if (e.Key == Keys.Escape)
        {
            CancelCurrent();
            e.Handled = true;
        }
    }

    #region Rhino Selection Hooks (1-Click Dynamic Edit)

    private void HookRhinoSelectionEvents()
    {
        try
        {
            RhinoDoc.SelectObjects += OnRhinoObjectsSelected;
            RhinoDoc.DeselectObjects += OnRhinoObjectsDeselected;
        }
        catch
        {
            // Headless execution outside Rhino
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                RhinoDoc.SelectObjects -= OnRhinoObjectsSelected;
                RhinoDoc.DeselectObjects -= OnRhinoObjectsDeselected;
            }
            catch
            {
            }
        }
        base.Dispose(disposing);
    }

    private void OnRhinoObjectsSelected(object? sender, RhinoObjectSelectionEventArgs e)
    {
        if (e.RhinoObjects == null || e.RhinoObjects.Length == 0) return;

        foreach (var obj in e.RhinoObjects)
        {
            if (obj?.Geometry == null) continue;

            var metadata = ParametricMetadata.Find(obj.Geometry);
            if (metadata == null) continue;

            if (metadata.Parameters.TryGetString("FeatureType", out var featureType))
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
                    vm.LoadFromMetadata(metadata, obj.Id);
                    SetViewModel(vm);
                    break;
                }
            }
        }
    }

    private void OnRhinoObjectsDeselected(object? sender, RhinoObjectSelectionEventArgs e)
    {
        // Keep active view model for continuous slider tuning
    }

    #endregion

    #region Dynamic Control Builders

    private void RebuildControlTree()
    {
        _controlsLayout.Items.Clear();

        if (_activeViewModel == null) return;

        switch (_activeViewModel)
        {
            case RingRailViewModel railVm:
                BuildRingRailControls(railVm);
                break;

            case ShankViewModel shankVm:
                BuildShankControls(shankVm);
                break;

            case GemOnCurveViewModel gemVm:
                BuildGemOnCurveControls(gemVm);
                break;

            case ProngHeadViewModel prongVm:
                BuildProngHeadControls(prongVm);
                break;

            case BezelViewModel bezelVm:
                BuildBezelControls(bezelVm);
                break;

            case PlaceholderViewModel placeholderVm:
                BuildPlaceholderControls(placeholderVm);
                break;

            case CutterViewModel cutterVm:
                BuildCutterControls(cutterVm);
                break;

            default:
                break;
        }
    }

    private void BuildRingRailControls(RingRailViewModel vm)
    {
        // Standard
        _controlsLayout.Items.Add(CreateEnumDropDown("Sizing Standard", vm.Standard, v => vm.Standard = v));
        // Ring Size
        _controlsLayout.Items.Add(CreateCoupledSlider("Ring Size", vm.RingSize, 1.0, 16.0, 0.25, v => vm.RingSize = v));
        // Inner Diameter
        _controlsLayout.Items.Add(CreateCoupledSlider("Inner Diameter (mm)", vm.InnerDiameterMm, 10.0, 26.0, 0.01, v => vm.InnerDiameterMm = v));
        // Profile Type
        _controlsLayout.Items.Add(CreateEnumDropDown("Profile Type", vm.ProfileType, v =>
        {
            vm.ProfileType = v;
            RebuildControlTree();
        }));

        if (vm.ProfileType == RailProfileType.EuroShank)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Corner Radius (mm)", vm.CornerRadiusMm, 0.5, 4.0, 0.01, v => vm.CornerRadiusMm = v));
        }
        else if (vm.ProfileType == RailProfileType.CushionSquircle)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Squircle Exponent", vm.SuperellipseExponent, 2.0, 6.0, 0.1, v => vm.SuperellipseExponent = v));
        }
        else if (vm.ProfileType == RailProfileType.BypassOverlap)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Overlap (deg)", vm.BypassOverlapDeg, 10.0, 90.0, 1.0, v => vm.BypassOverlapDeg = v));
            _controlsLayout.Items.Add(CreateCoupledSlider("Axial Offset (mm)", vm.BypassAxialOffsetMm, 0.5, 6.0, 0.01, v => vm.BypassAxialOffsetMm = v));
            _controlsLayout.Items.Add(CreateCoupledSlider("Radial Offset (mm)", vm.BypassRadialOffsetMm, 0.5, 5.0, 0.01, v => vm.BypassRadialOffsetMm = v));
        }
    }

    private void BuildShankControls(ShankViewModel vm)
    {
        _controlsLayout.Items.Add(CreateEnumDropDown("Profile Type", vm.ProfileType, v =>
        {
            vm.ProfileType = v;
            RebuildControlTree();
        }));

        _controlsLayout.Items.Add(CreateCoupledSlider("Finger Size (US)", vm.FingerSize, 1.0, 16.0, 0.25, v => vm.FingerSize = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Bottom Width (mm)", vm.BottomWidthMm, 1.0, 10.0, 0.01, v => vm.BottomWidthMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Bottom Thickness (mm)", vm.BottomThicknessMm, 0.8, 5.0, 0.01, v => vm.BottomThicknessMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Top Width (mm)", vm.TopWidthMm, 1.0, 12.0, 0.01, v => vm.TopWidthMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Top Thickness (mm)", vm.TopThicknessMm, 0.8, 6.0, 0.01, v => vm.TopThicknessMm = v));

        if (vm.ProfileType == ShankProfileType.Cathedral)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Arch Height (mm)", vm.CathedralArchHeightMm, 0.5, 5.0, 0.01, v => vm.CathedralArchHeightMm = v));
        }
        else if (vm.ProfileType == ShankProfileType.KnifeEdge)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Apex Radius (mm)", vm.KnifeEdgeApexRadiusMm, 0.05, 1.5, 0.01, v => vm.KnifeEdgeApexRadiusMm = v));
        }

        _controlsLayout.Items.Add(CreateCheckBox("Under-Gallery Azurador", vm.HasUnderGallery, v =>
        {
            vm.HasUnderGallery = v;
            RebuildControlTree();
        }));

        if (vm.HasUnderGallery)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Gallery Wall (mm)", vm.UnderGalleryWallMm, 0.8, 2.0, 0.01, v => vm.UnderGalleryWallMm = v));
        }
    }

    private void BuildGemOnCurveControls(GemOnCurveViewModel vm)
    {
        _controlsLayout.Items.Add(CreateEnumDropDown("Gem Cut", vm.CutType, v => vm.CutType = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Gem Diameter (mm)", vm.GemDiameterMm, 0.8, 12.0, 0.01, v => vm.GemDiameterMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Inter-Gem Spacing (mm)", vm.SpacingMm, 0.0, 3.0, 0.01, v => vm.SpacingMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Start Offset (mm)", vm.StartOffsetMm, 0.0, 10.0, 0.01, v => vm.StartOffsetMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("End Offset (mm)", vm.EndOffsetMm, 0.0, 10.0, 0.01, v => vm.EndOffsetMm = v));

        var stoneCountLabel = new Label
        {
            Text = $"Stone Count: {vm.StoneCount}",
            Font = JewelryDarkTheme.SubheaderFont,
            TextColor = JewelryDarkTheme.AccentCyan
        };
        vm.PreviewUpdated += (s, e) => stoneCountLabel.Text = $"Stone Count: {vm.StoneCount}";
        _controlsLayout.Items.Add(stoneCountLabel);
    }

    private void BuildProngHeadControls(ProngHeadViewModel vm)
    {
        _controlsLayout.Items.Add(CreateEnumDropDown("Prong Style", vm.Style, v => vm.Style = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Prong Count", vm.ProngCount, 4, 8, 2, v => vm.ProngCount = (int)v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Prong Diameter (mm)", vm.ProngDiameterMm, 0.5, 2.0, 0.01, v => vm.ProngDiameterMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Prong Height (mm)", vm.ProngHeightMm, 1.0, 10.0, 0.01, v => vm.ProngHeightMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Rake Angle (deg)", vm.RakeAngleDeg, 0.0, 25.0, 0.5, v => vm.RakeAngleDeg = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Girdle Overhang (mm)", vm.GirdleOverhangMm, 0.05, 0.40, 0.01, v => vm.GirdleOverhangMm = v));
        _controlsLayout.Items.Add(CreateEnumDropDown("Gem Cut", vm.GemCut, v => vm.GemCut = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Gem Diameter (mm)", vm.GemDiameterMm, 1.0, 15.0, 0.01, v => vm.GemDiameterMm = v));
        _controlsLayout.Items.Add(CreateCheckBox("Include Base Wire Ring", vm.IncludeBaseWire, v => vm.IncludeBaseWire = v));
        _controlsLayout.Items.Add(CreateCheckBox("Include Gallery Wire Ring", vm.IncludeGalleryWire, v => vm.IncludeGalleryWire = v));
    }

    private void BuildBezelControls(BezelViewModel vm)
    {
        _controlsLayout.Items.Add(CreateEnumDropDown("Gem Cut", vm.GemCut, v => vm.GemCut = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Gem Diameter (mm)", vm.GemDiameterMm, 1.0, 15.0, 0.01, v => vm.GemDiameterMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Wall Thickness (mm)", vm.WallThicknessMm, 0.4, 2.5, 0.01, v => vm.WallThicknessMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Height Above Girdle (mm)", vm.HeightAboveGirdleMm, 0.1, 1.5, 0.01, v => vm.HeightAboveGirdleMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Seat Depth (%)", vm.SeatDepthPercent, 10.0, 80.0, 1.0, v => vm.SeatDepthPercent = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Draft Angle (deg)", vm.DraftAngleDeg, 0.0, 20.0, 0.5, v => vm.DraftAngleDeg = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Light Hole Ratio", vm.LightHoleDiameterRatio, 0.20, 0.80, 0.01, v => vm.LightHoleDiameterRatio = v));
        _controlsLayout.Items.Add(CreateCheckBox("Semi-Bezel Lateral Openings", vm.IsSemiBezel, v =>
        {
            vm.IsSemiBezel = v;
            RebuildControlTree();
        }));

        if (vm.IsSemiBezel)
        {
            _controlsLayout.Items.Add(CreateCoupledSlider("Opening Span (deg)", vm.SemiBezelOpeningAngleDeg, 20.0, 120.0, 1.0, v => vm.SemiBezelOpeningAngleDeg = v));
        }

        _controlsLayout.Items.Add(CreateCheckBox("Milgrain Rim Knurling", vm.IncludeMilgrain, v => vm.IncludeMilgrain = v));
    }

    private void BuildCutterControls(CutterViewModel vm)
    {
        _controlsLayout.Items.Add(CreateEnumDropDown("Gem Cut", vm.GemCut, v => vm.GemCut = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Gem Diameter (mm)", vm.GemDiameterMm, 0.8, 15.0, 0.01, v => vm.GemDiameterMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Girdle Clearance (mm)", vm.GirdleClearanceMm, 0.02, 0.25, 0.01, v => vm.GirdleClearanceMm = v));
        _controlsLayout.Items.Add(CreateEnumDropDown("Bearing Seat Style", vm.SeatAngle, v => vm.SeatAngle = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Crown Extension (mm)", vm.CrownHeightExtensionMm, 0.5, 4.0, 0.01, v => vm.CrownHeightExtensionMm = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Pavilion Offset (%)", vm.PavilionOffsetPercent, 0.0, 25.0, 0.5, v => vm.PavilionOffsetPercent = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Light Hole Ratio", vm.LightHoleDiameterRatio, 0.20, 0.80, 0.01, v => vm.LightHoleDiameterRatio = v));
        _controlsLayout.Items.Add(CreateCoupledSlider("Light Hole Depth (mm)", vm.LightHoleDepthExtensionMm, 0.5, 6.0, 0.01, v => vm.LightHoleDepthExtensionMm = v));
        _controlsLayout.Items.Add(CreateCheckBox("Taper Shaft Outward", vm.TaperShaft, v => vm.TaperShaft = v));
    }

    #endregion

    #region Control Helpers

    private static Panel CreateCoupledSlider(string labelText, double initialValue, double min, double max, double step, Action<double> onValueChanged)
    {
        var container = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };

        var label = new Label
        {
            Text = labelText,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary
        };

        const int scale = 100;
        int sliderMin = (int)Math.Round(min * scale);
        int sliderMax = (int)Math.Round(max * scale);
        int sliderVal = Math.Clamp((int)Math.Round(initialValue * scale), sliderMin, sliderMax);

        var slider = new Slider
        {
            MinValue = sliderMin,
            MaxValue = sliderMax,
            Value = sliderVal,
            BackgroundColor = JewelryDarkTheme.BackgroundDark
        };

        var stepper = new NumericStepper
        {
            DecimalPlaces = 2,
            Increment = step,
            MinValue = min,
            MaxValue = max,
            Value = initialValue,
            Width = 75,
            Font = JewelryDarkTheme.MonospaceFont,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary
        };

        bool updating = false;

        slider.ValueChanged += (s, e) =>
        {
            if (updating) return;
            updating = true;
            double v = slider.Value / (double)scale;
            stepper.Value = v;
            onValueChanged(v);
            updating = false;
        };

        stepper.ValueChanged += (s, e) =>
        {
            if (updating) return;
            updating = true;
            double v = stepper.Value;
            slider.Value = Math.Clamp((int)Math.Round(v * scale), sliderMin, sliderMax);
            onValueChanged(v);
            updating = false;
        };

        var row = new TableLayout
        {
            Spacing = new Size(6, 0),
            Rows =
            {
                new TableRow(new TableCell(slider, true), new TableCell(stepper, false))
            }
        };

        container.Items.Add(label);
        container.Items.Add(row);

        return new Panel
        {
            Content = container,
            Padding = new Padding(2)
        };
    }

    private static Panel CreateEnumDropDown<T>(string labelText, T initialValue, Action<T> onSelected) where T : struct, Enum
    {
        var container = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };

        var label = new Label
        {
            Text = labelText,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary
        };

        var dropDown = new DropDown
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary,
            Font = JewelryDarkTheme.BodyFont
        };

        var values = Enum.GetValues<T>();
        int selectedIndex = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var val = values[i];
            dropDown.Items.Add(new ListItem { Text = val.ToString(), Key = val.ToString() });
            if (EqualityComparer<T>.Default.Equals(val, initialValue))
            {
                selectedIndex = i;
            }
        }
        dropDown.SelectedIndex = selectedIndex;

        dropDown.SelectedIndexChanged += (s, e) =>
        {
            if (dropDown.SelectedKey != null && Enum.TryParse<T>(dropDown.SelectedKey, out var parsed))
            {
                onSelected(parsed);
            }
        };

        container.Items.Add(label);
        container.Items.Add(dropDown);

        return new Panel
        {
            Content = container,
            Padding = new Padding(2)
        };
    }

    private static Panel CreateCheckBox(string labelText, bool isChecked, Action<bool> onToggled)
    {
        var check = new CheckBox
        {
            Text = labelText,
            Checked = isChecked,
            TextColor = JewelryDarkTheme.TextPrimary,
            Font = JewelryDarkTheme.BodyFont
        };

        check.CheckedChanged += (s, e) =>
        {
            onToggled(check.Checked ?? false);
        };

        return new Panel
        {
            Content = check,
            Padding = new Padding(2, 4)
        };
    }

    private void BuildPlaceholderControls(PlaceholderViewModel vm)
    {
        var badgeLabel = new Label
        {
            Text = $"CATEGORY: {vm.Category.ToUpperInvariant()} (PREVIEW)",
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.AccentGold
        };
        _controlsLayout.Items.Add(badgeLabel);

        var descLabel = new Label
        {
            Text = vm.Description,
            Font = JewelryDarkTheme.BodyFont,
            TextColor = JewelryDarkTheme.TextSecondary,
            Wrap = WrapMode.Word
        };
        _controlsLayout.Items.Add(new Panel
        {
            BackgroundColor = JewelryDarkTheme.SurfaceCard,
            Padding = new Padding(8),
            Content = descLabel
        });

        _controlsLayout.Items.Add(CreateCoupledSlider(vm.Param1Label, vm.Param1Value, vm.Param1Min, vm.Param1Max, vm.Param1Step, v => vm.Param1Value = v));
        _controlsLayout.Items.Add(CreateCoupledSlider(vm.Param2Label, vm.Param2Value, vm.Param2Min, vm.Param2Max, vm.Param2Step, v => vm.Param2Value = v));

        if (vm.Options != null && vm.Options.Length > 0)
        {
            _controlsLayout.Items.Add(CreateStringDropDown(vm.OptionLabel, vm.Options, vm.SelectedOption, v => vm.SelectedOption = v));
        }

        var noteLabel = new Label
        {
            Text = "Roadmap: Interactive parameter preview active. Full geometry synthesis engine in progress.",
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.AccentCyan,
            Wrap = WrapMode.Word
        };
        _controlsLayout.Items.Add(noteLabel);
    }

    private static Panel CreateStringDropDown(string labelText, string[] options, string initialValue, Action<string> onSelected)
    {
        var container = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 2
        };

        var label = new Label
        {
            Text = labelText,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextSecondary
        };

        var dropDown = new DropDown
        {
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary,
            Font = JewelryDarkTheme.BodyFont
        };

        int selectedIndex = 0;
        for (int i = 0; i < options.Length; i++)
        {
            var opt = options[i];
            dropDown.Items.Add(new ListItem { Text = opt, Key = opt });
            if (string.Equals(opt, initialValue, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
            }
        }
        dropDown.SelectedIndex = selectedIndex;

        dropDown.SelectedIndexChanged += (s, e) =>
        {
            if (dropDown.SelectedKey != null)
            {
                onSelected(dropDown.SelectedKey);
            }
        };

        container.Items.Add(label);
        container.Items.Add(dropDown);

        return new Panel
        {
            Content = container,
            Padding = new Padding(2)
        };
    }

    #endregion
}
