namespace RadianceStudio.UI.Ribbon;

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.Theme;

/// <summary>
/// Top horizontal ribbon bar (Height = 105) providing fast, organized access to the
/// 6 jewelry CAD workflow tabs: Rails &amp; Shanks, Gems, Settings, Cutters, Transform, and Manufacturing.
/// Built exclusively with pure Eto.Forms and procedural vector icons.
/// </summary>
public class JewelryRibbon : Panel
{
    public const int RibbonHeight = 105;

    public event EventHandler<string>? ActionTriggered;
    public event EventHandler<string>? CommandRequested;

    private readonly TabControl _tabControl;

    public JewelryRibbon()
    {
        Height = RibbonHeight;
        BackgroundColor = JewelryDarkTheme.BackgroundDark;

        _tabControl = new TabControl
        {
            BackgroundColor = JewelryDarkTheme.BackgroundDark
        };

        // Create the 6 workflow tabs
        _tabControl.Pages.Add(CreateRailsAndShanksTab());
        _tabControl.Pages.Add(CreateGemsTab());
        _tabControl.Pages.Add(CreateSettingsTab());
        _tabControl.Pages.Add(CreateCuttersTab());
        _tabControl.Pages.Add(CreateTransformTab());
        _tabControl.Pages.Add(CreateManufacturingTab());

        Content = _tabControl;
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

    #region Tab Builders

    private TabPage CreateRailsAndShanksTab()
    {
        var page = new TabPage
        {
            Text = "Rails & Shanks",
            Image = RibbonVectorIcons.GetRailsAndShanksIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        // Section: Ring Rails
        row.Items.Add(CreateSection("Ring Rails", new Control[]
        {
            CreateButton("Round", RibbonVectorIcons.GetRingRailIcon(RailProfileType.CanonicalCircle), "_RadRingRail Round", "Standard Round Ring Rail"),
            CreateButton("Euro", RibbonVectorIcons.GetRingRailIcon(RailProfileType.EuroShank), "_RadRingRail Euro", "Euro-Shank Ring Rail with Anti-Spin Corners"),
            CreateButton("Cushion", RibbonVectorIcons.GetRingRailIcon(RailProfileType.CushionSquircle), "_RadRingRail Cushion", "Cushion Squircle Ergonomic Rail"),
            CreateButton("Oval", RibbonVectorIcons.GetRingRailIcon(RailProfileType.CanonicalCircle), "_RadRingRail Oval", "Oval Ring Rail"),
            CreateButton("Square", RibbonVectorIcons.GetRingRailIcon(RailProfileType.CushionSquircle), "_RadRingRail Square", "Square Profile Ring Rail"),
            CreateButton("Bypass", RibbonVectorIcons.GetRingRailIcon(RailProfileType.BypassOverlap), "_RadRingRail Bypass", "3D Bypass Overlapping Rail")
        }));

        row.Items.Add(CreateSeparator());

        // Section: Shanks
        row.Items.Add(CreateSection("Shanks", new Control[]
        {
            CreateButton("Standard", RibbonVectorIcons.GetShankIcon(ShankProfileType.Flat), "_RadShank Flat", "Standard Flat Shank"),
            CreateButton("Euro-Shank", RibbonVectorIcons.GetShankIcon(ShankProfileType.DShape), "_RadShank EuroShank", "Euro-Shank Anti-Spin Profile"),
            CreateButton("Cathedral", RibbonVectorIcons.GetShankIcon(ShankProfileType.Cathedral), "_RadShank Cathedral", "Elevated Cathedral Arch Shank"),
            CreateButton("Bypass", RibbonVectorIcons.GetShankIcon(ShankProfileType.Flat), "_RadShank Bypass", "Spiral Bypass Shank"),
            CreateButton("D-Shape", RibbonVectorIcons.GetShankIcon(ShankProfileType.DShape), "_RadShank DShape", "D-Shape Half-Round Shank"),
            CreateButton("Comfort-Fit", RibbonVectorIcons.GetShankIcon(ShankProfileType.ComfortFit), "_RadShank ComfortFit", "Double-Crowned Comfort-Fit Shank"),
            CreateButton("Knife-Edge", RibbonVectorIcons.GetShankIcon(ShankProfileType.KnifeEdge), "_RadShank KnifeEdge", "Peaked Knife-Edge Shank")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    private TabPage CreateGemsTab()
    {
        var page = new TabPage
        {
            Text = "Gems",
            Image = RibbonVectorIcons.GetGemsIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        // Gem Loader Quick Cuts
        var cutsDropDown = new DropDown
        {
            Width = 120,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary
        };

        var cutTypes = new[]
        {
            GemCutType.RoundBrilliant, GemCutType.Princess, GemCutType.Oval,
            GemCutType.Pear, GemCutType.Emerald, GemCutType.Marquise,
            GemCutType.Cushion, GemCutType.Baguette, GemCutType.Radiant,
            GemCutType.Asscher, GemCutType.Heart, GemCutType.Trilliant,
            GemCutType.TaperedBaguette
        };

        foreach (var c in cutTypes)
        {
            cutsDropDown.Items.Add(new ListItem { Text = c.ToString(), Key = c.ToString() });
        }
        cutsDropDown.SelectedIndex = 0;

        var loadGemBtn = new Button
        {
            Text = "Load Gem",
            Image = RibbonVectorIcons.GetGemCutIcon(GemCutType.RoundBrilliant),
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.AccentCyan
        };
        loadGemBtn.Click += (s, e) =>
        {
            string cutName = cutsDropDown.SelectedKey ?? "RoundBrilliant";
            TriggerAction($"_RadGemLoader {cutName}");
        };

        var gemLoaderBox = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Items = { cutsDropDown, loadGemBtn }
        };

        row.Items.Add(CreateSection("Gem Loader (13 Cuts)", new Control[] { gemLoaderBox }));
        row.Items.Add(CreateSeparator());

        // Placement tools
        row.Items.Add(CreateSection("Stone Placement", new Control[]
        {
            CreateButton("On Curve", RibbonVectorIcons.GetGemsOnCurveIcon(), "_RadGemOnCurve", "Distribute Gems along 3D Space Curve"),
            CreateButton("Between Curves", RibbonVectorIcons.GetGemsOnCurveIcon(), "_RadGemsBetweenCurves", "Distribute Gem Row between Two Rails"),
            CreateButton("3D Surface Pavé", RibbonVectorIcons.GetPaveSurfaceIcon(), "_RadPave", "Automatic Hexagonal / Honeycomb Surface Pavé")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    private TabPage CreateSettingsTab()
    {
        var page = new TabPage
        {
            Text = "Settings",
            Image = RibbonVectorIcons.GetSettingsIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        row.Items.Add(CreateSection("Prongs", new Control[]
        {
            CreateButton("4-Prong", RibbonVectorIcons.GetProngHeadIcon(4), "_RadProngHead 4", "Parametric 4-Prong Head"),
            CreateButton("6-Prong", RibbonVectorIcons.GetProngHeadIcon(6), "_RadProngHead 6", "Classic Tiffany-style 6-Prong Head"),
            CreateButton("8-Prong", RibbonVectorIcons.GetProngHeadIcon(8), "_RadProngHead 8", "High-Security 8-Prong Head"),
            CreateButton("V-Prongs", RibbonVectorIcons.GetProngHeadIcon(4), "_RadProngHead VProng", "Corner Chevron V-Prongs for Princess / Marquise"),
            CreateButton("Shared", RibbonVectorIcons.GetProngHeadIcon(4), "_RadProngHead SharedProng", "Shared Prongs for Eternity Bands")
        }));

        row.Items.Add(CreateSeparator());

        row.Items.Add(CreateSection("Bezels & Halos", new Control[]
        {
            CreateButton("Full Bezel", RibbonVectorIcons.GetFullBezelIcon(), "_RadBezel Full", "Full 360-degree Metal Collar Bezel"),
            CreateButton("Semi-Bezel", RibbonVectorIcons.GetSemiBezelIcon(), "_RadBezel Semi", "Dual Open-Side Semi-Bezel"),
            CreateButton("Halos", RibbonVectorIcons.GetGemsOnCurveIcon(), "_RadHalo Single", "Surrounding Melee Gem Halo")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    private TabPage CreateCuttersTab()
    {
        var page = new TabPage
        {
            Text = "Cutters",
            Image = RibbonVectorIcons.GetCuttersIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        row.Items.Add(CreateSection("Seat & Hole Cutters", new Control[]
        {
            CreateButton("Gem Cutters", RibbonVectorIcons.GetCuttersIcon(), "_RadCutter GemCutter", "Parametric Seat Cutters for Stone Rows"),
            CreateButton("Light Holes", RibbonVectorIcons.GetCuttersIcon(), "_RadCutter LightHole", "Piercing Light Hole Punches"),
            CreateButton("Channel Cutters", RibbonVectorIcons.GetCuttersIcon(), "_RadCutter Channel", "Channel Groove Cutters")
        }));

        row.Items.Add(CreateSeparator());

        row.Items.Add(CreateSection("Booleans", new Control[]
        {
            CreateButton("1-Click Boolean", RibbonVectorIcons.GetBooleanDifferenceIcon(), "_RadApplyBooleanCutters", "Subtract All Active Cutters from Metal Solenoids")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    private TabPage CreateTransformTab()
    {
        var page = new TabPage
        {
            Text = "Transform",
            Image = RibbonVectorIcons.GetTransformIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        row.Items.Add(CreateSection("Array & Mirror", new Control[]
        {
            CreateButton("Polar Array", RibbonVectorIcons.GetTransformIcon(), "_RadPolarArray", "Radial Polar Array around Ring Axis"),
            CreateButton("Mirror", RibbonVectorIcons.GetTransformIcon(), "_RadMirror", "Symmetric Mirror across XZ / YZ Plane")
        }));

        row.Items.Add(CreateSeparator());

        row.Items.Add(CreateSection("Deformation", new Control[]
        {
            CreateButton("Surface Flow", RibbonVectorIcons.GetTransformIcon(), "_RadSurfaceFlow", "Flow Planar Details along 3D Ring Surface"),
            CreateButton("Comfort Deform", RibbonVectorIcons.GetTransformIcon(), "_RadComfortDeform", "Ergonomic Inside-Band Comfort Deformation")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    private TabPage CreateManufacturingTab()
    {
        var page = new TabPage
        {
            Text = "Manufacturing",
            Image = RibbonVectorIcons.GetManufacturingIcon(16),
            BackgroundColor = JewelryDarkTheme.SurfaceCard
        };

        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Padding = new Padding(4)
        };

        // Alloy Selector
        var alloyDropDown = new DropDown
        {
            Width = 140,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.AccentGold
        };
        alloyDropDown.Items.Add(new ListItem { Text = "14K Yellow Gold", Key = "14K_Yellow" });
        alloyDropDown.Items.Add(new ListItem { Text = "18K Yellow Gold", Key = "18K_Yellow" });
        alloyDropDown.Items.Add(new ListItem { Text = "14K White Gold", Key = "14K_White" });
        alloyDropDown.Items.Add(new ListItem { Text = "18K White Gold", Key = "18K_White" });
        alloyDropDown.Items.Add(new ListItem { Text = "Platinum 950", Key = "Pt950" });
        alloyDropDown.Items.Add(new ListItem { Text = "Sterling Silver 925", Key = "Ag925" });
        alloyDropDown.SelectedIndex = 0;
        alloyDropDown.SelectedIndexChanged += (s, e) =>
        {
            TriggerAction($"_RadSetAlloy {alloyDropDown.SelectedKey}");
        };

        var alloyBox = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Items =
            {
                new Label { Text = "Precious Alloy:", TextColor = JewelryDarkTheme.TextSecondary, Font = JewelryDarkTheme.SmallFont },
                alloyDropDown
            }
        };

        row.Items.Add(CreateSection("Materials", new Control[] { alloyBox }));
        row.Items.Add(CreateSeparator());

        row.Items.Add(CreateSection("Diagnostics & Export", new Control[]
        {
            CreateButton("Live BOM Report", RibbonVectorIcons.GetManufacturingIcon(), "_RadBOM", "Calculates Weights, Gem Counts, and Material Cost"),
            CreateButton("Wall Thickness", RibbonVectorIcons.GetManufacturingIcon(), "_RadWallThickness", "Diagnoses Thin Walls (< 0.8mm) for Casting Feasibility"),
            CreateButton("Hermetic STL", RibbonVectorIcons.GetManufacturingIcon(), "_RadExportSTL", "Exports Watertight 3D Print STL via ShrinkWrap")
        }));

        page.Content = new Scrollable { Content = row, BackgroundColor = JewelryDarkTheme.SurfaceCard };
        return page;
    }

    #endregion

    #region Helper Controls

    private static Control CreateSeparator()
    {
        return new Panel
        {
            Width = 1,
            BackgroundColor = JewelryDarkTheme.BorderSubtle,
            Height = 70
        };
    }

    private static Panel CreateSection(string title, IEnumerable<Control> controls)
    {
        var container = new StackLayout
        {
            Orientation = Orientation.Vertical,
            Spacing = 3,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var controlsRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };

        foreach (var c in controls)
        {
            controlsRow.Items.Add(c);
        }

        container.Items.Add(controlsRow);
        container.Items.Add(new Label
        {
            Text = title,
            Font = JewelryDarkTheme.SmallFont,
            TextColor = JewelryDarkTheme.TextMuted,
            TextAlignment = TextAlignment.Center
        });

        return new Panel
        {
            Content = container,
            Padding = new Padding(2)
        };
    }

    private Button CreateButton(string label, Image? icon, string command, string tooltip)
    {
        var btn = new Button
        {
            Text = label,
            Image = icon,
            ImagePosition = ButtonImagePosition.Above,
            ToolTip = tooltip,
            BackgroundColor = JewelryDarkTheme.SurfaceElevated,
            TextColor = JewelryDarkTheme.TextPrimary,
            Font = JewelryDarkTheme.SmallFont,
            Height = 56,
            Width = 66
        };

        btn.Click += (s, e) => TriggerAction(command);
        return btn;
    }

    private void TriggerAction(string command)
    {
        ActionTriggered?.Invoke(this, command);
        CommandRequested?.Invoke(this, command);

        // If running in Rhino environment, execute command string
        try
        {
            Rhino.RhinoApp.RunScript(command, false);
        }
        catch
        {
            // Headless / outside Rhino - event notification suffices
        }
    }

    #endregion
}
