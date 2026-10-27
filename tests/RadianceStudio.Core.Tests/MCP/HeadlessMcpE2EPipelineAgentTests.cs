namespace RadianceStudio.Core.Tests.MCP;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.MCP.Tools;
using Xunit;

/// <summary>
/// Headless MCP End-to-End Pipeline Agent Test.
/// Executes the complete 9-stage parametric jewelry lifecycle sequentially via MCP JSON-RPC tools,
/// verifying that every tool produces a valid success envelope and that the final watertight STL export
/// is binary, valid, and production-ready.
/// </summary>
public class HeadlessMcpE2EPipelineAgentTests
{
    [Fact]
    public async Task HeadlessMcpAgent_ExecutesFullJewelryLifecycle_SuccessfullyExportsWatertightSTL()
    {
        var registry = JewelryMcpRegistry.Default;
        string tempStlPath = Path.Combine(Path.GetTempPath(), $"radiance_agent_test_{Guid.NewGuid():N}.stl");

        try
        {
            // Stage 1: set_ring_rail (US Size 7.0, CanonicalCircle)
            string railArgs = JsonSerializer.Serialize(new
            {
                sizeUs = 7.0,
                standard = "US",
                profileType = "CanonicalCircle",
                widthMm = 2.4,
                thicknessMm = 1.8
            });
            string railRes = await registry.ExecuteToolAsync("set_ring_rail", railArgs);
            AssertSuccess(railRes, out var railDoc);
            Assert.True(railDoc.RootElement.GetProperty("data").TryGetProperty("objectId", out var railIdEl));
            string railId = railIdEl.GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(railId));

            // Stage 2: set_shank (ComfortFit swept along rail)
            string shankArgs = JsonSerializer.Serialize(new
            {
                targetRailId = railId,
                profileType = "ComfortFit",
                bottomWidthMm = 2.2,
                bottomThicknessMm = 1.6,
                topWidthMm = 2.8,
                topThicknessMm = 2.0,
                hasUnderGallery = false
            });
            string shankRes = await registry.ExecuteToolAsync("set_shank", shankArgs);
            AssertSuccess(shankRes, out var shankDoc);
            Assert.True(shankDoc.RootElement.GetProperty("data").TryGetProperty("objectId", out var shankIdEl));
            string shankId = shankIdEl.GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(shankId));

            // Stage 3: set_gem_row (RoundBrilliant diamonds on curve)
            string gemArgs = JsonSerializer.Serialize(new
            {
                targetCurveId = railId,
                gemDiameterMm = 2.0,
                spacingMm = 0.25,
                cutType = "RoundBrilliant",
                startOffsetMm = 0.0,
                endOffsetMm = 0.0
            });
            string gemRes = await registry.ExecuteToolAsync("set_gem_row", gemArgs);
            AssertSuccess(gemRes, out var gemDoc);
            Assert.True(gemDoc.RootElement.GetProperty("data").TryGetProperty("gemCount", out var gemCountEl));
            int gemCount = gemCountEl.GetInt32();
            Assert.True(gemCount > 0, $"Expected gemCount > 0, got {gemCount}");

            // Stage 4: set_prong_head (4-prong setting)
            string prongArgs = JsonSerializer.Serialize(new
            {
                targetMetalId = shankId,
                prongCount = 4,
                prongDiameterMm = 0.8,
                girdleOverhangMm = 0.2,
                prongTaperShaft = true
            });
            string prongRes = await registry.ExecuteToolAsync("set_prong_head", prongArgs);
            AssertSuccess(prongRes, out _);

            // Stage 5: set_cutter (Parametric gem cutters)
            string cutterArgs = JsonSerializer.Serialize(new
            {
                targetGemId = railId,
                seatStyle = "Angled70",
                girdleClearanceMm = 0.08,
                hasLightHole = true
            });
            string cutterRes = await registry.ExecuteToolAsync("set_cutter", cutterArgs);
            AssertSuccess(cutterRes, out _);

            // Stage 6: apply_boolean_cutters
            string booleanArgs = JsonSerializer.Serialize(new
            {
                targetMetalId = shankId,
                baseToleranceMm = 0.001
            });
            string booleanRes = await registry.ExecuteToolAsync("apply_boolean_cutters", booleanArgs);
            AssertSuccess(booleanRes, out _);

            // Stage 7: calculate_bom_and_weights
            string bomArgs = JsonSerializer.Serialize(new
            {
                projectTitle = "Agent Verification Solitaire Ring",
                alloy = "Gold18KYellow",
                spotPricePerGram = 75.0
            });
            string bomRes = await registry.ExecuteToolAsync("calculate_bom_and_weights", bomArgs);
            AssertSuccess(bomRes, out var bomDoc);
            var bomData = bomDoc.RootElement.GetProperty("data");
            Assert.True(bomData.TryGetProperty("metalWeights", out var weightsEl));
            Assert.True(bomData.TryGetProperty("totalCaratWeight", out var caratsEl));
            Assert.True(caratsEl.GetDouble() > 0.0);

            // Stage 8: check_wall_thickness
            string wallArgs = JsonSerializer.Serialize(new
            {
                targetObjectId = shankId,
                minGeneralWallMm = 0.80,
                minProngNeckMm = 0.60,
                onlyViolations = true
            });
            string wallRes = await registry.ExecuteToolAsync("check_wall_thickness", wallArgs);
            AssertSuccess(wallRes, out var wallDoc);
            var wallData = wallDoc.RootElement.GetProperty("data");
            Assert.True(wallData.TryGetProperty("isPass", out var isPassEl));
            Assert.True(wallData.TryGetProperty("violationsCount", out var violCountEl));

            // Stage 9: check_and_export (Watertight STL Export)
            string exportArgs = JsonSerializer.Serialize(new
            {
                outputFilePath = tempStlPath,
                targetVoxelSizeMm = 0.05,
                validateManifold = true
            });
            string exportRes = await registry.ExecuteToolAsync("check_and_export", exportArgs);
            AssertSuccess(exportRes, out var exportDoc);
            var exportData = exportDoc.RootElement.GetProperty("data");
            Assert.True(exportData.GetProperty("exportedSuccessfully").GetBoolean());
            Assert.True(exportData.GetProperty("isSolid").GetBoolean());
            Assert.True(exportData.GetProperty("isClosed").GetBoolean());
            Assert.True(exportData.GetProperty("faceCount").GetInt32() > 0);

            // Verify Physical File on Disk
            Assert.True(File.Exists(tempStlPath), $"STL file was not created at {tempStlPath}");
            var fileInfo = new FileInfo(tempStlPath);
            Assert.True(fileInfo.Length >= 84, $"STL file size ({fileInfo.Length} bytes) is less than minimal header (84 bytes)");

            using var reader = new BinaryReader(File.OpenRead(tempStlPath));
            byte[] header = reader.ReadBytes(80);
            Assert.Equal(80, header.Length);
            uint triCount = reader.ReadUInt32();
            Assert.True(triCount > 0, $"Expected STL triangle count > 0, got {triCount}");
        }
        finally
        {
            if (File.Exists(tempStlPath))
            {
                try { File.Delete(tempStlPath); } catch { }
            }
        }
    }

    private static void AssertSuccess(string responseJson, out JsonDocument doc)
    {
        Assert.False(string.IsNullOrWhiteSpace(responseJson), "Response JSON cannot be empty.");
        doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        string? status = root.GetProperty("status").GetString();
        if (status != "success")
        {
            string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            Assert.Fail($"MCP Tool failed with status '{status}': {msg}");
        }
    }
}
