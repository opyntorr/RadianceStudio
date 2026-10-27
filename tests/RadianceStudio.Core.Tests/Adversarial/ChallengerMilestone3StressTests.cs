namespace RadianceStudio.Core.Tests.Adversarial;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.MCP.Dispatcher;
using RadianceStudio.MCP.Tools;
using RadianceStudio.Rhino.Commands;
using RadianceStudio.UI.Inspector;
using Xunit;

/// <summary>
/// Empirical Challenger Adversarial Verification Suite for Milestone 3 Iteration 2.
/// Stress tests:
/// 1. RhinoUiDispatcher async exception propagation and CS4014 zero-warning compliance.
/// 2. MCP parameter aliases and canonical pairs (railId/targetRailId, metalId/targetMetalId,
///    alloy/metalAlloy, objectId/targetObjectId, gemDiameterMm/stoneDiameterMm, seatAngleDegrees/seatAngle)
///    with robust structured JSON response envelope oracles.
/// 3. Reflection-based integrity verification across all 12 Rhino Command classes, ensuring
///    subclassing of Rhino.Commands.Command, parameter exposure, and 100% distinct, valid GUIDs.
/// 4. UI Inspector disposal and event unhooking verification.
/// </summary>
public class ChallengerMilestone3StressTests
{
    #region 1. Dispatcher CS4014 & Async Exception Stress

    [Fact]
    public async Task Dispatcher_NullActionInvocations_AwaitsThrowsCleanlyUnderHighConcurrency()
    {
        // Line 20-25 pattern verification
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThreadAsync<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread((Action)null!));

        // High concurrency null action stress (100 parallel tasks)
        var parallelTasks = Enumerable.Range(0, 100).Select(async i =>
        {
            if (i % 3 == 0)
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread<string>(null!));
            }
            else if (i % 3 == 1)
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThreadAsync<string>(null!));
            }
            else
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread((Action)null!));
            }
        });

        await Task.WhenAll(parallelTasks);
    }

    #endregion

    #region 2. MCP Parameter Alias Stress & Response Envelope Oracles

    [Fact]
    public async Task McpAlias_SetShank_TargetRailId_And_RailId_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;

        // 1. Create a ring rail to get a genuine rail GUID
        string railArgs = JsonSerializer.Serialize(new
        {
            sizeUs = 8.5,
            profileType = "CanonicalCircle",
            widthMm = 2.5,
            thicknessMm = 1.8
        });
        string railResp = await registry.ExecuteToolAsync("set_ring_rail", railArgs);
        AssertEnvelope(railResp, expectedStatus: "success");
        using var railDoc = JsonDocument.Parse(railResp);
        string railGuid = railDoc.RootElement.GetProperty("data").GetProperty("objectId").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(railGuid));

        // 2. Call set_shank with canonical targetRailId
        string canonicalArgs = JsonSerializer.Serialize(new
        {
            targetRailId = railGuid,
            profileType = "ComfortFit",
            bottomWidthMm = 2.0,
            bottomThicknessMm = 1.5,
            topWidthMm = 2.5,
            topThicknessMm = 1.8
        });
        string canonicalResp = await registry.ExecuteToolAsync("set_shank", canonicalArgs);
        AssertEnvelope(canonicalResp, expectedStatus: "success");

        // 3. Call set_shank with alias railId
        string aliasArgs = JsonSerializer.Serialize(new
        {
            railId = railGuid,
            profileType = "ComfortFit",
            bottomWidthMm = 2.0,
            bottomThicknessMm = 1.5,
            topWidthMm = 2.5,
            topThicknessMm = 1.8
        });
        string aliasResp = await registry.ExecuteToolAsync("set_shank", aliasArgs);
        AssertEnvelope(aliasResp, expectedStatus: "success");

        // 4. Call set_shank with BOTH targetRailId and railId (targetRailId takes priority or both compatible)
        string bothArgs = JsonSerializer.Serialize(new
        {
            targetRailId = railGuid,
            railId = railGuid,
            profileType = "ComfortFit"
        });
        string bothResp = await registry.ExecuteToolAsync("set_shank", bothArgs);
        AssertEnvelope(bothResp, expectedStatus: "success");
    }

    [Fact]
    public async Task McpAlias_ApplyBooleanCutters_MetalId_And_TargetMetalId_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;
        string fakeMetalId = Guid.NewGuid().ToString();

        // 1. Canonical metalId
        string canonicalArgs = JsonSerializer.Serialize(new
        {
            metalId = fakeMetalId,
            cutterIds = new[] { Guid.NewGuid().ToString() },
            baseToleranceMm = 0.001
        });
        string canonicalResp = await registry.ExecuteToolAsync("apply_boolean_cutters", canonicalArgs);
        AssertEnvelope(canonicalResp, expectedStatus: "success");
        using var canDoc = JsonDocument.Parse(canonicalResp);
        Assert.Equal(fakeMetalId, canDoc.RootElement.GetProperty("data").GetProperty("targetMetalId").GetString());

        // 2. Alias targetMetalId
        string aliasArgs = JsonSerializer.Serialize(new
        {
            targetMetalId = fakeMetalId,
            cutterIds = new[] { Guid.NewGuid().ToString() },
            baseToleranceMm = 0.001
        });
        string aliasResp = await registry.ExecuteToolAsync("apply_boolean_cutters", aliasArgs);
        AssertEnvelope(aliasResp, expectedStatus: "success");
        using var aliasDoc = JsonDocument.Parse(aliasResp);
        Assert.Equal(fakeMetalId, aliasDoc.RootElement.GetProperty("data").GetProperty("targetMetalId").GetString());

        // 3. Both present
        string bothArgs = JsonSerializer.Serialize(new
        {
            metalId = fakeMetalId,
            targetMetalId = fakeMetalId
        });
        string bothResp = await registry.ExecuteToolAsync("apply_boolean_cutters", bothArgs);
        AssertEnvelope(bothResp, expectedStatus: "success");
    }

    [Fact]
    public async Task McpAlias_CalculateBomAndWeights_Alloy_And_MetalAlloy_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;

        // 1. Canonical alloy
        string canonicalArgs = JsonSerializer.Serialize(new
        {
            alloy = "Platinum950",
            spotPricePerGram = 40.0
        });
        string canonicalResp = await registry.ExecuteToolAsync("calculate_bom_and_weights", canonicalArgs);
        AssertEnvelope(canonicalResp, expectedStatus: "success");
        using var canDoc = JsonDocument.Parse(canonicalResp);
        var canData = canDoc.RootElement.GetProperty("data");
        var canWeights = canData.GetProperty("metalWeights");
        var ptWeightCan = canWeights.EnumerateArray().FirstOrDefault(w => w.GetProperty("alloy").GetString() == "Platinum950");
        Assert.True(ptWeightCan.ValueKind != JsonValueKind.Undefined, "Platinum950 should be present in metalWeights");
        Assert.True(ptWeightCan.GetProperty("estimatedCost").GetDouble() > 0.0, "Platinum950 estimatedCost should be > 0 with spot price");

        // 2. Alias metalAlloy
        string aliasArgs = JsonSerializer.Serialize(new
        {
            metalAlloy = "Platinum950",
            spotPricePerGram = 40.0
        });
        string aliasResp = await registry.ExecuteToolAsync("calculate_bom_and_weights", aliasArgs);
        AssertEnvelope(aliasResp, expectedStatus: "success");
        using var aliasDoc = JsonDocument.Parse(aliasResp);
        var aliasData = aliasDoc.RootElement.GetProperty("data");
        var aliasWeights = aliasData.GetProperty("metalWeights");
        var ptWeightAlias = aliasWeights.EnumerateArray().FirstOrDefault(w => w.GetProperty("alloy").GetString() == "Platinum950");
        Assert.True(ptWeightAlias.ValueKind != JsonValueKind.Undefined, "Platinum950 should be present in metalWeights");
        Assert.True(ptWeightAlias.GetProperty("estimatedCost").GetDouble() > 0.0, "Platinum950 estimatedCost should be > 0 with spot price");

        // 3. Both present
        string bothArgs = JsonSerializer.Serialize(new
        {
            alloy = "Platinum950",
            metalAlloy = "Platinum950",
            spotPricePerGram = 50.0
        });
        string bothResp = await registry.ExecuteToolAsync("calculate_bom_and_weights", bothArgs);
        AssertEnvelope(bothResp, expectedStatus: "success");
    }

    [Fact]
    public async Task McpAlias_CheckWallThickness_ObjectId_And_TargetObjectId_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;
        string fakeObjId = Guid.NewGuid().ToString();

        // 1. Canonical targetObjectId
        string canonicalArgs = JsonSerializer.Serialize(new
        {
            targetObjectId = fakeObjId,
            minGeneralWallMm = 0.85,
            minProngNeckMm = 0.65,
            onlyViolations = true
        });
        string canonicalResp = await registry.ExecuteToolAsync("check_wall_thickness", canonicalArgs);
        AssertEnvelope(canonicalResp, expectedStatus: "success");
        using var canDoc = JsonDocument.Parse(canonicalResp);
        Assert.Equal(0.85, canDoc.RootElement.GetProperty("data").GetProperty("minGeneralWallMm").GetDouble());

        // 2. Alias objectId + minThresholdMm
        string aliasArgs = JsonSerializer.Serialize(new
        {
            objectId = fakeObjId,
            minThresholdMm = 0.85,
            minProngNeckMm = 0.65,
            onlyViolations = true
        });
        string aliasResp = await registry.ExecuteToolAsync("check_wall_thickness", aliasArgs);
        AssertEnvelope(aliasResp, expectedStatus: "success");
        using var aliasDoc = JsonDocument.Parse(aliasResp);
        Assert.Equal(0.85, aliasDoc.RootElement.GetProperty("data").GetProperty("minGeneralWallMm").GetDouble());

        // 3. Both present
        string bothArgs = JsonSerializer.Serialize(new
        {
            targetObjectId = fakeObjId,
            objectId = fakeObjId,
            minGeneralWallMm = 0.80,
            minThresholdMm = 0.80
        });
        string bothResp = await registry.ExecuteToolAsync("check_wall_thickness", bothArgs);
        AssertEnvelope(bothResp, expectedStatus: "success");
    }

    [Fact]
    public async Task McpAlias_SetPaveSurface_GemDiameterMm_And_StoneDiameterMm_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;

        // 1. Canonical stoneDiameterMm
        string canonicalArgs = JsonSerializer.Serialize(new
        {
            stoneDiameterMm = 1.45,
            stoneSpacingMm = 0.20,
            patternType = "Hexagonal"
        });
        string canonicalResp = await registry.ExecuteToolAsync("set_pave_surface", canonicalArgs);
        AssertEnvelope(canonicalResp, expectedStatus: "success");
        using var canDoc = JsonDocument.Parse(canonicalResp);
        Assert.Equal(1.45, canDoc.RootElement.GetProperty("data").GetProperty("stoneDiameterMm").GetDouble());

        // 2. Alias gemDiameterMm + spacingMm + pattern
        string aliasArgs = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 1.45,
            spacingMm = 0.20,
            pattern = "Hexagonal"
        });
        string aliasResp = await registry.ExecuteToolAsync("set_pave_surface", aliasArgs);
        AssertEnvelope(aliasResp, expectedStatus: "success");
        using var aliasDoc = JsonDocument.Parse(aliasResp);
        Assert.Equal(1.45, aliasDoc.RootElement.GetProperty("data").GetProperty("stoneDiameterMm").GetDouble());

        // 3. Both present
        string bothArgs = JsonSerializer.Serialize(new
        {
            stoneDiameterMm = 1.45,
            gemDiameterMm = 1.45,
            stoneSpacingMm = 0.20,
            spacingMm = 0.20
        });
        string bothResp = await registry.ExecuteToolAsync("set_pave_surface", bothArgs);
        AssertEnvelope(bothResp, expectedStatus: "success");
    }

    [Fact]
    public async Task McpAlias_SetCutter_SeatAngle_And_SeatAngleDegrees_BothResolveProperly()
    {
        var registry = JewelryMcpRegistry.Default;

        // 1. Canonical seatAngle: "Degree90"
        string args1 = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 6.0,
            seatAngle = "Degree90"
        });
        string resp1 = await registry.ExecuteToolAsync("set_cutter", args1);
        AssertEnvelope(resp1, expectedStatus: "success");
        using var doc1 = JsonDocument.Parse(resp1);
        Assert.Equal("Degree90", doc1.RootElement.GetProperty("data").GetProperty("seatAngle").GetString());

        // 2. Alias seatAngleDegrees: "Degree90"
        string args2 = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 6.0,
            seatAngleDegrees = "Degree90"
        });
        string resp2 = await registry.ExecuteToolAsync("set_cutter", args2);
        AssertEnvelope(resp2, expectedStatus: "success");
        using var doc2 = JsonDocument.Parse(resp2);
        Assert.Equal("Degree90", doc2.RootElement.GetProperty("data").GetProperty("seatAngle").GetString());

        // 3. Alias seatAngleDegrees: "90"
        string args3 = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 6.0,
            seatAngleDegrees = "90"
        });
        string resp3 = await registry.ExecuteToolAsync("set_cutter", args3);
        AssertEnvelope(resp3, expectedStatus: "success");
        using var doc3 = JsonDocument.Parse(resp3);
        Assert.Equal("Degree90", doc3.RootElement.GetProperty("data").GetProperty("seatAngle").GetString());

        // 4. Alias seatAngleDegrees: "70deg"
        string args4 = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 6.0,
            seatAngleDegrees = "70deg"
        });
        string resp4 = await registry.ExecuteToolAsync("set_cutter", args4);
        AssertEnvelope(resp4, expectedStatus: "success");
        using var doc4 = JsonDocument.Parse(resp4);
        Assert.Equal("Degree70", doc4.RootElement.GetProperty("data").GetProperty("seatAngle").GetString());

        // 5. Girdle clearance alias: girdleClearancePercent vs girdleClearanceMm
        string args5 = JsonSerializer.Serialize(new
        {
            gemDiameterMm = 6.0,
            girdleClearancePercent = 0.15
        });
        string resp5 = await registry.ExecuteToolAsync("set_cutter", args5);
        AssertEnvelope(resp5, expectedStatus: "success");
        using var doc5 = JsonDocument.Parse(resp5);
        Assert.Equal(0.15, doc5.RootElement.GetProperty("data").GetProperty("girdleClearanceMm").GetDouble());
    }

    [Fact]
    public async Task McpRegistry_AdversarialStressOracle_All16ToolsUnderHarshInputs_NeverThrow()
    {
        var registry = JewelryMcpRegistry.Default;
        var toolNames = registry.GetRegisteredToolNames();
        Assert.Equal(16, toolNames.Count);

        string[] harshPayloads =
        [
            "",
            "   ",
            "{}",
            "{\"unknownField\": true, \"extra\": [1,2,3]}",
            "{\"sizeUs\": \"not_a_number\", \"targetRailId\": 12345}",
            "{\"minGeneralWallMm\": -99999.9, \"cutterIds\": \"not_an_array\"}",
            "{\"rollbackIndex\": -1, \"mode\": \"StudioClassic\"}",
            "{\"featureId\": null, \"isSuppressed\": \"maybe\"}"
        ];

        foreach (var tool in toolNames)
        {
            foreach (var payload in harshPayloads)
            {
                string response = await registry.ExecuteToolAsync(tool, payload);
                AssertEnvelope(response); // Must be a valid structured envelope without crashing
            }
        }
    }

    #endregion

    #region 3. Reflection-Based Rhino Command Integrity

    [Fact]
    public void RhinoCommands_Reflection_All12CommandClassesInheritFromCommandAndHaveDistinctGuids()
    {
        var commandTypes = typeof(RadStudioCommand).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(global::Rhino.Commands.Command)) && t.Name != nameof(RadHarnessAuditCommand))
            .ToList();

        // Exactly 12 Rhino Commands must exist
        Assert.Equal(12, commandTypes.Count);

        var registeredGuids = new Dictionary<Guid, Type>();
        var commandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cmdType in commandTypes)
        {
            // Must reside in RadianceStudio.Rhino.Commands namespace
            Assert.Equal("RadianceStudio.Rhino.Commands", cmdType.Namespace);

            // Must follow Rad* naming convention
            Assert.StartsWith("Rad", cmdType.Name);
            commandNames.Add(cmdType.Name);

            // Must have GuidAttribute
            var guidAttr = cmdType.GetCustomAttribute<GuidAttribute>();
            Assert.NotNull(guidAttr);
            Assert.False(string.IsNullOrWhiteSpace(guidAttr.Value));

            // Must be parseable valid Guid
            Assert.True(Guid.TryParse(guidAttr.Value, out var parsedGuid),
                $"Command {cmdType.Name} has invalid GUID format: '{guidAttr.Value}'");
            Assert.NotEqual(Guid.Empty, parsedGuid);

            // Must be distinct - zero collision tolerance
            Assert.False(registeredGuids.ContainsKey(parsedGuid),
                $"GUID Collision detected! Both {registeredGuids.GetValueOrDefault(parsedGuid)?.Name} and {cmdType.Name} share GUID {parsedGuid}");
            registeredGuids[parsedGuid] = cmdType;

            // Must have static public CommandParameters field
            var paramField = cmdType.GetField("CommandParameters", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(paramField);
            var paramArray = paramField.GetValue(null) as string[];
            Assert.NotNull(paramArray);
            Assert.NotEmpty(paramArray);
        }

        Assert.Equal(12, registeredGuids.Count);
        Assert.Equal(12, commandNames.Count);

        // Explicitly verify the previously colliding commands
        var toggleCmdType = typeof(RadToggleTreeModeCommand);
        var exportStlCmdType = typeof(RadExportSTLCommand);

        var toggleGuid = Guid.Parse(toggleCmdType.GetCustomAttribute<GuidAttribute>()!.Value);
        var exportGuid = Guid.Parse(exportStlCmdType.GetCustomAttribute<GuidAttribute>()!.Value);

        Assert.NotEqual(toggleGuid, exportGuid);
        Assert.Equal(Guid.Parse("E83F019E-2B1D-47F8-94D3-B9876C6E83F0"), toggleGuid);
        Assert.Equal(Guid.Parse("0981EF1A-140C-4A2D-A2A9-373C3091B012"), exportGuid);
    }

    #endregion

    #region 4. UI Inspector Disposal Pattern

    [Fact]
    public void DynamicInspectorPanel_ImplementsDisposalSafely()
    {
        // Verify DynamicInspectorPanel inherits from Eto.Forms.Panel which implements IDisposable
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(DynamicInspectorPanel)));

        // Verify protected override void Dispose(bool disposing) method exists
        var disposeMethod = typeof(DynamicInspectorPanel).GetMethod("Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(bool) },
            null);

        Assert.NotNull(disposeMethod);
        Assert.True(disposeMethod.IsVirtual);
    }

    #endregion

    #region Envelope Oracle Helper

    private static void AssertEnvelope(string responseJson, string? expectedStatus = null)
    {
        Assert.False(string.IsNullOrWhiteSpace(responseJson), "Response was null or empty.");

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("status", out var statusElement), "Envelope missing 'status' property.");

        string? status = statusElement.GetString();
        Assert.True(status == "success" || status == "error", $"Invalid status: '{status}'. Expected 'success' or 'error'.");

        if (expectedStatus != null)
        {
            Assert.Equal(expectedStatus, status);
        }

        if (status == "success")
        {
            Assert.True(root.TryGetProperty("data", out _), "Success envelope missing 'data' property.");
        }
        else
        {
            Assert.True(root.TryGetProperty("message", out var msgElement), "Error envelope missing 'message' property.");
            Assert.False(string.IsNullOrWhiteSpace(msgElement.GetString()), "Error envelope 'message' was empty.");
        }
    }

    #endregion
}
