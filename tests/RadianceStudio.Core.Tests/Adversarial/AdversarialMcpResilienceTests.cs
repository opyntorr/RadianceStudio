namespace RadianceStudio.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.MCP.Tools;
using Xunit;

/// <summary>
/// Adversarial stress testing for JewelryMcpRegistry and all 16 MCP jewelry tools.
/// Verifies malformed JSON, unknown tool names, null arguments, and invalid parameters
/// return valid structured JSON envelopes ({ "status": "error", "message": ... })
/// without throwing unhandled exceptions or crashing the process.
/// </summary>
public class AdversarialMcpResilienceTests
{
    private static readonly string[] All16ToolNames =
    [
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
    ];

    [Fact]
    public void McpRegistry_All16ToolsAreRegistered()
    {
        var registry = JewelryMcpRegistry.Default;
        var registeredNames = registry.GetRegisteredToolNames();

        foreach (var expectedTool in All16ToolNames)
        {
            Assert.Contains(expectedTool, registeredNames);
            var toolDef = registry.GetTool(expectedTool);
            Assert.NotNull(toolDef);
            Assert.NotNull(toolDef.Handler);
            Assert.False(string.IsNullOrWhiteSpace(toolDef.Description));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not_a_real_tool")]
    [InlineData("SET_RING_RAIL_TYPO")]
    [InlineData("system_shutdown")]
    [InlineData("../../etc/passwd")]
    public async Task McpRegistry_UnknownOrInvalidToolNames_ReturnErrorEnvelope(string invalidToolName)
    {
        var registry = JewelryMcpRegistry.Default;
        string response = await registry.ExecuteToolAsync(invalidToolName, "{}");

        AssertValidJsonEnvelope(response, expectedStatus: "error");
    }

    [Fact]
    public async Task McpRegistry_NullToolName_ReturnsErrorEnvelope()
    {
        var registry = JewelryMcpRegistry.Default;
        string response = await registry.ExecuteToolAsync(null!, "{}");

        AssertValidJsonEnvelope(response, expectedStatus: "error");
    }

    [Theory]
    [InlineData("{ invalid json")]
    [InlineData("{ \"unclosed: 123")]
    [InlineData("not json at all")]
    [InlineData("<<<xml>>>")]
    public async Task McpRegistry_MalformedJsonArguments_ReturnErrorEnvelope(string malformedJson)
    {
        var registry = JewelryMcpRegistry.Default;

        foreach (var tool in All16ToolNames)
        {
            string response = await registry.ExecuteToolAsync(tool, malformedJson);
            AssertValidJsonEnvelope(response, expectedStatus: "error");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    public async Task McpRegistry_EmptyOrNullArguments_ExecuteWithoutCrashing(string? emptyArgs)
    {
        var registry = JewelryMcpRegistry.Default;

        foreach (var tool in All16ToolNames)
        {
            string response = await registry.ExecuteToolAsync(tool, emptyArgs);
            // Must return valid JSON envelope (either success with defaults, or error if required param missing)
            AssertValidJsonEnvelope(response);
        }
    }

    [Fact]
    public async Task McpRegistry_Tool1_GetSceneInfo_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args1 = JsonSerializer.Serialize(new { filter = "nonexistent_filter_xyz_12345" });
        string resp1 = await registry.ExecuteToolAsync("get_scene_info", args1);
        AssertValidJsonEnvelope(resp1);

        string args2 = JsonSerializer.Serialize(new { filter = new string('A', 5000) });
        string resp2 = await registry.ExecuteToolAsync("get_scene_info", args2);
        AssertValidJsonEnvelope(resp2);
    }

    [Fact]
    public async Task McpRegistry_Tool2_SetRingRail_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        // SizeUs extreme negative
        string args1 = JsonSerializer.Serialize(new
        {
            sizeUs = -9999.0,
            standard = "INVALID_STANDARD",
            profileType = "UNKNOWN_PROFILE",
            widthMm = -10.0,
            thicknessMm = -10.0,
            cornerRadius = -5.0,
            superellipseExponent = -1.0,
            bypassOverlapDeg = -1000.0,
            bypassAxialOffsetMm = -50.0,
            bypassRadialOffsetMm = -50.0
        });
        string resp1 = await registry.ExecuteToolAsync("set_ring_rail", args1);
        AssertValidJsonEnvelope(resp1);

        // SizeUs extreme positive
        string args2 = JsonSerializer.Serialize(new
        {
            sizeUs = 10000.0,
            standard = "US",
            profileType = "CanonicalCircle",
            widthMm = 5.0,
            thicknessMm = 2.0
        });
        string resp2 = await registry.ExecuteToolAsync("set_ring_rail", args2);
        AssertValidJsonEnvelope(resp2);
    }

    [Fact]
    public async Task McpRegistry_Tool3_SetShank_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            railId = "NOT_A_VALID_GUID",
            profileType = "INVALID_PROFILE_TYPE",
            bottomWidthMm = -10.0,
            bottomThicknessMm = 0.01,
            topWidthMm = -20.0,
            topThicknessMm = 0.01,
            hasUnderGallery = true,
            underGalleryWallMm = -5.0,
            underGalleryClearanceMm = -5.0,
            underGallerySpanDeg = -360.0,
            crownRatio = 2.5,
            knifeEdgeApexRadiusMm = -1.0,
            cathedralArchHeightMm = -10.0
        });
        string resp = await registry.ExecuteToolAsync("set_shank", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool4_SetGemRow_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            targetCurveId = "INVALID_CURVE_GUID",
            gemDiameterMm = -50.0,
            spacingMm = -10.0,
            cutType = "NON_EXISTENT_CUT",
            startOffsetMm = -10.0,
            endOffsetMm = -10.0
        });
        string resp = await registry.ExecuteToolAsync("set_gem_row", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool5_SetPaveSurface_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            targetSurfaceId = "INVALID_SURFACE_GUID",
            stoneDiameterMm = -1.0,
            stoneSpacingMm = -0.5,
            patternType = "INVALID_PATTERN",
            borderOffsetMm = -10.0,
            beadDiameterMm = -1.0,
            beadHeightMm = -1.0,
            prongType = "INVALID_PRONG"
        });
        string resp = await registry.ExecuteToolAsync("set_pave_surface", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool6_SetProngHead_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            gemId = "INVALID_GEM_GUID",
            cutType = "INVALID_CUT",
            gemDiameterMm = -10.0,
            prongCount = 99, // only 4, 6, 8 valid
            prongStyle = "INVALID_STYLE",
            prongDiameterMm = -2.0,
            prongHeightMm = -5.0,
            girdleOverhangMm = -1.0,
            rakeAngleDeg = 999.0,
            includeBaseWire = true,
            includeGalleryWire = true
        });
        string resp = await registry.ExecuteToolAsync("set_prong_head", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool7_SetBezel_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            gemId = "INVALID_GEM_GUID",
            cutType = "INVALID_CUT",
            gemDiameterMm = -5.0,
            wallThicknessMm = -1.0,
            heightAboveGirdleMm = -2.0,
            isSemiBezel = true,
            semiBezelOpeningAngleDeg = 500.0,
            draftAngleDeg = -20.0,
            seatDepthPercent = 200.0,
            lightHoleDiameterPercent = -50.0
        });
        string resp = await registry.ExecuteToolAsync("set_bezel", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool8_SetHalo_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            gemId = "INVALID_GEM_GUID",
            centerCutType = "INVALID_CUT",
            centerGemDiameterMm = -10.0,
            meleeDiameterMm = -2.0,
            spacingMm = -1.0,
            guardDistanceMm = -1.0,
            dropHeightMm = -50.0,
            trayDepthMm = -5.0,
            trayWallThicknessMm = -2.0
        });
        string resp = await registry.ExecuteToolAsync("set_halo", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool9_SetCutter_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            gemId = "INVALID_GEM_GUID",
            cutType = "INVALID_CUT",
            gemDiameterMm = -5.0,
            girdleClearanceMm = -1.0,
            seatAngle = "INVALID_SEAT_ANGLE",
            crownHeightExtensionMm = -2.0,
            lightHoleDepthExtensionMm = -5.0,
            lightHoleDiameterRatio = -1.0,
            taperShaft = false
        });
        string resp = await registry.ExecuteToolAsync("set_cutter", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool10_ApplyBooleanCutters_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            targetMetalId = "INVALID_METAL_GUID",
            cutterIds = new[] { "INVALID_CUTTER_1", "INVALID_CUTTER_2" },
            baseToleranceMm = -0.5
        });
        string resp = await registry.ExecuteToolAsync("apply_boolean_cutters", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool11_CalculateBomAndWeights_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            projectTitle = "Extreme BOM Test",
            metalAlloy = "NON_EXISTENT_ALLOY",
            spotPricesPerGram = new Dictionary<string, double>
            {
                { "InvalidAlloy", -100.0 },
                { "NegativePrice", -50.0 }
            }
        });
        string resp = await registry.ExecuteToolAsync("calculate_bom_and_weights", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool12_CheckWallThickness_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            targetObjectId = "INVALID_OBJECT_GUID",
            minGeneralWallMm = -10.0,
            minProngNeckMm = -5.0,
            onlyViolations = true
        });
        string resp = await registry.ExecuteToolAsync("check_wall_thickness", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool13_CheckAndExport_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            outputFilePath = "C:\\Invalid:Path/?*|Illegal.stl",
            targetVoxelSizeMm = -0.1,
            validateManifold = true
        });
        string resp = await registry.ExecuteToolAsync("check_and_export", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool14_SetParametricMode_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            mode = "INVALID_PARAMETRIC_MODE_999"
        });
        string resp = await registry.ExecuteToolAsync("set_parametric_mode", args);
        AssertValidJsonEnvelope(resp);
    }

    [Fact]
    public async Task McpRegistry_Tool15_RollbackTimeline_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args1 = JsonSerializer.Serialize(new { rollbackIndex = -99999 });
        string resp1 = await registry.ExecuteToolAsync("rollback_timeline", args1);
        AssertValidJsonEnvelope(resp1);

        string args2 = JsonSerializer.Serialize(new { rollbackIndex = 99999 });
        string resp2 = await registry.ExecuteToolAsync("rollback_timeline", args2);
        AssertValidJsonEnvelope(resp2);
    }

    [Fact]
    public async Task McpRegistry_Tool16_SuppressFeature_ExtremeInputs()
    {
        var registry = JewelryMcpRegistry.Default;

        string args = JsonSerializer.Serialize(new
        {
            featureId = "NON_EXISTENT_GUID_12345",
            isSuppressed = true,
            cascadeToChildren = true
        });
        string resp = await registry.ExecuteToolAsync("suppress_feature", args);
        AssertValidJsonEnvelope(resp);
    }

    private static void AssertValidJsonEnvelope(string jsonString, string? expectedStatus = null)
    {
        Assert.False(string.IsNullOrWhiteSpace(jsonString), "Response JSON was null or empty.");

        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("status", out var statusProp), "Envelope missing 'status' property.");

        string status = statusProp.GetString()!;
        Assert.True(status == "success" || status == "error", $"Status must be 'success' or 'error', was '{status}'.");

        if (expectedStatus != null)
        {
            Assert.Equal(expectedStatus, status);
        }

        if (status == "error")
        {
            Assert.True(root.TryGetProperty("message", out var msgProp), "Error envelope missing 'message' property.");
            Assert.False(string.IsNullOrWhiteSpace(msgProp.GetString()), "Error envelope message was empty.");
        }
        else
        {
            Assert.True(root.TryGetProperty("data", out _), "Success envelope missing 'data' property.");
        }
    }
}
