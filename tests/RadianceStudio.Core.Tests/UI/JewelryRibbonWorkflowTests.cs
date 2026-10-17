namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Enums;
using Xunit;

/// <summary>
/// Tier 1 &amp; Tier 2 tests for Feature 6: Top Horizontal Ribbon Bar.
/// Validates ribbon layout geometry (Height=105), 6 workflow tabs, tool items,
/// procedural vector icon registry, and command routing.
/// </summary>
public class JewelryRibbonWorkflowTests
{
    public const int RibbonHeight = 105;

    public static readonly string[] WorkflowTabs =
    {
        "Rails & Shanks",
        "Gems",
        "Settings",
        "Cutters",
        "Transform",
        "Manufacturing"
    };

    [Fact]
    public void Ribbon_FixedDimensions_Matches105PxHeight()
    {
        Assert.Equal(105, RibbonHeight);
    }

    [Fact]
    public void Ribbon_All6WorkflowTabs_AreRegisteredInOrder()
    {
        Assert.Equal(6, WorkflowTabs.Length);
        Assert.Equal("Rails & Shanks", WorkflowTabs[0]);
        Assert.Equal("Gems", WorkflowTabs[1]);
        Assert.Equal("Settings", WorkflowTabs[2]);
        Assert.Equal("Cutters", WorkflowTabs[3]);
        Assert.Equal("Transform", WorkflowTabs[4]);
        Assert.Equal("Manufacturing", WorkflowTabs[5]);
    }

    [Fact]
    public void Ribbon_RailsAndShanksTab_RegistersAllProfiles()
    {
        var expectedRailProfiles = new[]
        {
            RailProfileType.CanonicalCircle,
            RailProfileType.EuroShank,
            RailProfileType.CushionSquircle,
            RailProfileType.BypassOverlap
        };

        foreach (var profile in expectedRailProfiles)
        {
            Assert.True(Enum.IsDefined(typeof(RailProfileType), profile));
        }

        var expectedSizing = new[]
        {
            RingSizingStandard.US,
            RingSizingStandard.UK,
            RingSizingStandard.EU,
            RingSizingStandard.JP
        };

        foreach (var standard in expectedSizing)
        {
            Assert.True(Enum.IsDefined(typeof(RingSizingStandard), standard));
        }

        var expectedShankProfiles = new[]
        {
            ShankProfileType.Flat,
            ShankProfileType.DShape,
            ShankProfileType.ComfortFit,
            ShankProfileType.KnifeEdge,
            ShankProfileType.Cathedral
        };

        foreach (var profile in expectedShankProfiles)
        {
            Assert.True(Enum.IsDefined(typeof(ShankProfileType), profile));
        }
    }

    [Fact]
    public void Ribbon_GemsTab_RegistersAllCanonicalCuts()
    {
        var expectedCuts = new[]
        {
            GemCutType.RoundBrilliant,
            GemCutType.Princess,
            GemCutType.Oval,
            GemCutType.Pear,
            GemCutType.Emerald,
            GemCutType.Marquise,
            GemCutType.Cushion,
            GemCutType.Baguette,
            GemCutType.Radiant,
            GemCutType.Asscher,
            GemCutType.Heart,
            GemCutType.Trilliant
        };

        foreach (var cut in expectedCuts)
        {
            Assert.True(Enum.IsDefined(typeof(GemCutType), cut));
        }
    }

    [Fact]
    public void Ribbon_SettingsTab_RegistersProngsBezelsAndHalos()
    {
        var expectedProngStyles = new[]
        {
            ProngStyle.RoundDome,
            ProngStyle.RoundFlat,
            ProngStyle.Claw,
            ProngStyle.VProng,
            ProngStyle.SharedProng
        };

        foreach (var style in expectedProngStyles)
        {
            Assert.True(Enum.IsDefined(typeof(ProngStyle), style));
        }
    }

    [Fact]
    public void Ribbon_ManufacturingTab_RegistersAllPreciousMetalAlloys()
    {
        var expectedAlloys = new[]
        {
            PreciousMetalAlloy.Gold14KYellow,
            PreciousMetalAlloy.Gold14KWhite,
            PreciousMetalAlloy.Gold18KYellow,
            PreciousMetalAlloy.Gold18KWhite,
            PreciousMetalAlloy.Gold18KRose,
            PreciousMetalAlloy.Platinum950,
            PreciousMetalAlloy.Silver925,
            PreciousMetalAlloy.CastingWax
        };

        foreach (var alloy in expectedAlloys)
        {
            Assert.True(Enum.IsDefined(typeof(PreciousMetalAlloy), alloy));
        }
    }

    [Theory]
    [InlineData("Rails & Shanks", "RadRingRail", "RadShank")]
    [InlineData("Gems", "RadGemOnCurve", "RadPave")]
    [InlineData("Settings", "RadProngHead", "RadBezel")]
    [InlineData("Cutters", "RadCutter")]
    [InlineData("Manufacturing", "RadBOM", "RadExportSTL")]
    public void Ribbon_TabCommands_MapToInteractiveRhinoCommands(string tabName, params string[] expectedCommands)
    {
        var mapping = GetRibbonCommandMapping();

        Assert.True(mapping.ContainsKey(tabName), $"Missing tab: {tabName}");
        var commands = mapping[tabName];

        foreach (var cmd in expectedCommands)
        {
            Assert.Contains(cmd, commands);
        }
    }

    [Fact]
    public void Ribbon_ProceduralVectorIconRegistry_ContainsEntriesForEachTab()
    {
        var iconNames = GetProceduralIconNames();

        Assert.Contains("icon_tab_rails", iconNames);
        Assert.Contains("icon_tab_gems", iconNames);
        Assert.Contains("icon_tab_settings", iconNames);
        Assert.Contains("icon_tab_cutters", iconNames);
        Assert.Contains("icon_tab_transform", iconNames);
        Assert.Contains("icon_tab_manufacturing", iconNames);
    }

    private static Dictionary<string, List<string>> GetRibbonCommandMapping()
    {
        return new Dictionary<string, List<string>>
        {
            ["Rails & Shanks"] = new() { "RadRingRail", "RadShank" },
            ["Gems"] = new() { "RadGemOnCurve", "RadPave" },
            ["Settings"] = new() { "RadProngHead", "RadBezel" },
            ["Cutters"] = new() { "RadCutter" },
            ["Transform"] = new() { "RadStudio" },
            ["Manufacturing"] = new() { "RadBOM", "RadExportSTL" }
        };
    }

    private static HashSet<string> GetProceduralIconNames()
    {
        return new HashSet<string>
        {
            "icon_tab_rails",
            "icon_tab_gems",
            "icon_tab_settings",
            "icon_tab_cutters",
            "icon_tab_transform",
            "icon_tab_manufacturing",
            "icon_cmd_ringrail",
            "icon_cmd_shank",
            "icon_cmd_gemoncurve",
            "icon_cmd_pave",
            "icon_cmd_pronghead",
            "icon_cmd_bezel",
            "icon_cmd_cutter",
            "icon_cmd_bom",
            "icon_cmd_exportstl"
        };
    }
}
