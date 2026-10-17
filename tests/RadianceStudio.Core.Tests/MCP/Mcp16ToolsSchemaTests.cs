namespace RadianceStudio.Core.Tests.MCP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RadianceStudio.MCP.Tools;
using Xunit;

/// <summary>
/// Genuine verification tests for Features 18 &amp; 19: Expanded 16 MCP Tools and MCP JSON Schema &amp; Registry.
/// Validates registration of all 16 tools, JSON Schema validity, parameter definitions, and types directly
/// against the production JewelryMcpRegistry without mock facades.
/// </summary>
public class Mcp16ToolsSchemaTests
{
    public static readonly string[] Required16ToolNames =
    {
        "get_scene_info",
        "set_ring_rail",
        "set_shank",
        "set_gem_row",
        "set_pave_surface",
        "set_prong_head",
        "set_bezel",
        "set_halo",
        "set_cutter",
        "apply_boolean_cutters",
        "calculate_bom_and_weights",
        "check_wall_thickness",
        "check_and_export",
        "set_parametric_mode",
        "rollback_timeline",
        "suppress_feature"
    };

    [Fact]
    public void McpTools_All16RequiredTools_AreDefinedAndDistinct()
    {
        var registered = JewelryMcpRegistry.Default.GetRegisteredToolNames();

        Assert.Equal(16, Required16ToolNames.Length);
        Assert.Equal(16, registered.Count);
        Assert.Equal(16, registered.Distinct().Count());

        foreach (var tool in Required16ToolNames)
        {
            Assert.Contains(tool, registered);
        }
    }

    [Theory]
    [InlineData("get_scene_info")]
    [InlineData("set_ring_rail")]
    [InlineData("set_shank")]
    [InlineData("set_gem_row")]
    [InlineData("set_pave_surface")]
    [InlineData("set_prong_head")]
    [InlineData("set_bezel")]
    [InlineData("set_halo")]
    [InlineData("set_cutter")]
    [InlineData("apply_boolean_cutters")]
    [InlineData("calculate_bom_and_weights")]
    [InlineData("check_wall_thickness")]
    [InlineData("check_and_export")]
    [InlineData("set_parametric_mode")]
    [InlineData("rollback_timeline")]
    [InlineData("suppress_feature")]
    public void McpToolSchemas_GenerateValidJsonSchema_ForEveryTool(string toolName)
    {
        var toolDef = JewelryMcpRegistry.Default.GetTool(toolName);
        Assert.NotNull(toolDef);
        Assert.Equal(toolName, toolDef.Name);
        Assert.False(string.IsNullOrWhiteSpace(toolDef.Description));

        string schemaJson = JsonSerializer.Serialize(toolDef.InputSchema);
        Assert.False(string.IsNullOrWhiteSpace(schemaJson));

        using var doc = JsonDocument.Parse(schemaJson);
        var root = doc.RootElement;

        // Must be an object schema with properties dictionary
        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("properties", out var properties));
        Assert.Equal(JsonValueKind.Object, properties.ValueKind);
    }

    [Theory]
    [InlineData("set_ring_rail", "sizeUs", "number")]
    [InlineData("set_ring_rail", "widthMm", "number")]
    [InlineData("set_ring_rail", "thicknessMm", "number")]
    [InlineData("set_gem_row", "targetCurveId", "string")]
    [InlineData("set_gem_row", "gemDiameterMm", "number")]
    [InlineData("set_gem_row", "spacingMm", "number")]
    [InlineData("set_prong_head", "gemId", "string")]
    [InlineData("set_prong_head", "prongCount", "integer")]
    [InlineData("set_bezel", "gemId", "string")]
    [InlineData("set_bezel", "wallThicknessMm", "number")]
    [InlineData("set_cutter", "gemId", "string")]
    [InlineData("calculate_bom_and_weights", "alloy", "string")]
    [InlineData("set_parametric_mode", "mode", "string")]
    [InlineData("rollback_timeline", "rollbackIndex", "integer")]
    [InlineData("suppress_feature", "featureId", "string")]
    public void McpToolSchemas_ContainExpectedPropertiesAndTypes(string toolName, string propertyName, string expectedType)
    {
        var toolDef = JewelryMcpRegistry.Default.GetTool(toolName);
        Assert.NotNull(toolDef);

        string schemaJson = JsonSerializer.Serialize(toolDef.InputSchema);
        using var doc = JsonDocument.Parse(schemaJson);
        var properties = doc.RootElement.GetProperty("properties");

        Assert.True(properties.TryGetProperty(propertyName, out var propElement),
            $"Property '{propertyName}' missing in schema for '{toolName}'");

        Assert.Equal(expectedType, propElement.GetProperty("type").GetString());
    }

    [Fact]
    public void McpToolSchemas_ExportAll16SchemasInRegistryManifest()
    {
        string manifest = JewelryMcpRegistry.ExportSchemas();
        Assert.False(string.IsNullOrWhiteSpace(manifest));

        using var doc = JsonDocument.Parse(manifest);
        Assert.True(doc.RootElement.TryGetProperty("tools", out var toolsArray));
        Assert.Equal(16, toolsArray.GetArrayLength());

        var exportedToolNames = toolsArray.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        Assert.Equal(16, exportedToolNames.Count);
        foreach (var tool in Required16ToolNames)
        {
            Assert.Contains(tool, exportedToolNames);
        }
    }
}
