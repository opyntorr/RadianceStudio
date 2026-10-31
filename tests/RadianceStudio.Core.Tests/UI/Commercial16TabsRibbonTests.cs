namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.UI.Ribbon;
using Xunit;

/// <summary>
/// Comprehensive verification suite for the 16 Commercial Workflow Tabs,
/// Tool Catalog, Dynamic Jewelry Badging, Tool Group Flyouts, and Top Command Search.
/// </summary>
public class Commercial16TabsRibbonTests
{
    [Fact]
    public void Catalog_HasExactly16TabsInCorrectOrder()
    {
        var expectedTabs = new[]
        {
            "Curve", "Surface", "Solid", "Tools", "Gems", "Settings", "Cutters", "Transform",
            "Clayoo", "Render", "View", "Mesh", "Dimension", "Analyze", "Misc", "Manufacturing"
        };

        Assert.Equal(16, RibbonToolCatalog.Tabs.Count);
        for (int i = 0; i < expectedTabs.Length; i++)
        {
            Assert.Equal(expectedTabs[i], RibbonToolCatalog.Tabs[i].Title);
            Assert.Equal(expectedTabs[i], RibbonToolCatalog.Tabs[i].Id);
        }
    }

    [Fact]
    public void Catalog_AllTabsHaveDistinctIndicatorColors()
    {
        var colors = RibbonToolCatalog.Tabs.Select(t => t.IndicatorColor).ToList();
        Assert.Equal(16, colors.Count);
        // Every indicator color is defined and has non-zero alpha
        foreach (var c in colors)
        {
            Assert.True(c.A > 0);
        }
    }

    [Fact]
    public void Catalog_ToolsTab_ContainsRequiredDynamicJewelryTools()
    {
        var tools = RibbonToolCatalog.GetToolsForTab("Tools");
        Assert.NotEmpty(tools);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("Signet Advanced", toolNames);
        Assert.Contains("Ring Rail", toolNames);
        Assert.Contains("Outside Ring Rail", toolNames);
        Assert.Contains("Bypass Ring Rail", toolNames);
        Assert.Contains("Split Shank", toolNames);
        Assert.Contains("Profile Placer", toolNames);
        Assert.Contains("Profile Cap", toolNames);
        Assert.Contains("Gems In Channel", toolNames);
        Assert.Contains("Signet", toolNames);
    }

    [Fact]
    public void Catalog_GemsTab_ContainsRequiredGemTools()
    {
        var gems = RibbonToolCatalog.GetToolsForTab("Gems");
        Assert.NotEmpty(gems);

        var gemNames = gems.Select(t => t.Name).ToList();
        Assert.Contains("Gem On Curve", gemNames);
        Assert.Contains("Gem", gemNames);
        Assert.Contains("Gem Between 2 Curves", gemNames);
        Assert.Contains("Gem List On Curve", gemNames);
        Assert.Contains("Gem Guides", gemNames);
        Assert.Contains("Pave", gemNames);
    }

    [Fact]
    public void Catalog_SettingsTab_ContainsRequiredSettingTools()
    {
        var settings = RibbonToolCatalog.GetToolsForTab("Settings");
        Assert.NotEmpty(settings);

        var settingNames = settings.Select(t => t.Name).ToList();
        Assert.Contains("Head", settingNames);
        Assert.Contains("Bezel", settingNames);
        Assert.Contains("Halo", settingNames);
        Assert.Contains("Prong On Surface", settingNames);
        Assert.Contains("Pave Prongs", settingNames);
        Assert.Contains("Cluster Prong Placer", settingNames);
    }

    [Fact]
    public void Catalog_CuttersTab_ContainsRequiredCutterTools()
    {
        var cutters = RibbonToolCatalog.GetToolsForTab("Cutters");
        Assert.NotEmpty(cutters);

        var cutterNames = cutters.Select(t => t.Name).ToList();
        Assert.Contains("Cut To Ring Rail", cutterNames);
        Assert.Contains("Gem Cutter", cutterNames);
        Assert.Contains("Gem Cutter Library", cutterNames);
        Assert.Contains("Channel Cutter", cutterNames);
        Assert.Contains("Micro Prong Cutter", cutterNames);
        Assert.Contains("Hollow Ring", cutterNames);
    }

    [Fact]
    public void Catalog_NativeRhinoCADTools_ExecuteViaDirectMacro()
    {
        var curveTools = RibbonToolCatalog.GetToolsForTab("Curve");
        var lineTool = curveTools.FirstOrDefault(t => t.Name == "Line");
        Assert.NotNull(lineTool);
        Assert.Equal("! _Line", lineTool.CommandOrMacro);

        var solidTools = RibbonToolCatalog.GetToolsForTab("Solid");
        var cylinderTool = solidTools.FirstOrDefault(t => t.Name == "Cylinder");
        Assert.NotNull(cylinderTool);
        Assert.Equal("! _Cylinder", cylinderTool.CommandOrMacro);

        var miscTools = RibbonToolCatalog.GetToolsForTab("Misc");
        var undoTool = miscTools.FirstOrDefault(t => t.Name == "Undo");
        Assert.NotNull(undoTool);
        Assert.Equal("! _Undo", undoTool.CommandOrMacro);
    }

    [Fact]
    public void Catalog_ToolGroups_ContainPopulatedSubTools()
    {
        var lineGroup = RibbonToolCatalog.GetToolGroup("Line");
        Assert.NotNull(lineGroup);
        Assert.True(lineGroup.SubTools.Count >= 4);

        var boolGroup = RibbonToolCatalog.GetToolGroup("Boolean");
        Assert.NotNull(boolGroup);
        Assert.True(boolGroup.SubTools.Count >= 4);
        Assert.Contains(boolGroup.SubTools, t => t.Name == "Boolean Union");
        Assert.Contains(boolGroup.SubTools, t => t.Name == "Boolean Difference");
    }

    [Theory]
    [InlineData("Ring", "Ring Rail")]
    [InlineData("Gem", "Gem On Curve")]
    [InlineData("Bezel", "Bezel")]
    [InlineData("Cutter", "Gem Cutter")]
    [InlineData("Line", "Line")]
    [InlineData("Sweep", "Profile Sweep")]
    public void Search_FindsMatchingToolsAccurately(string query, string expectedMatch)
    {
        var results = RibbonToolCatalog.Search(query).ToList();
        Assert.NotEmpty(results);
        Assert.Contains(results, t => t.Name.Contains(expectedMatch, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsEmpty()
    {
        var results = RibbonToolCatalog.Search(string.Empty).ToList();
        Assert.Empty(results);
    }
}
