namespace RadianceStudio.Core.Tests.MCP;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using RadianceStudio.MCP.Tools;
using Xunit;

/// <summary>
/// Genuine verification tests for MCP tool invocation, response envelope compliance,
/// and boundary/error handling (empty args, invalid types, missing fields, unknown tools).
/// Executes directly against the production JewelryMcpRegistry without mock facades.
/// </summary>
public class McpInvocationAndBoundaryTests
{
    [Fact]
    public async Task McpEnvelope_SuccessResponse_FollowsContract()
    {
        string args = JsonSerializer.Serialize(new
        {
            sizeUs = 6.5,
            widthMm = 2.0,
            thicknessMm = 1.5
        });

        string responseJson = await JewelryMcpRegistry.Default.ExecuteToolAsync("set_ring_rail", args);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        Assert.Equal("success", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("data", out var dataElement));
        Assert.True(dataElement.TryGetProperty("objectId", out _));
        Assert.True(dataElement.GetProperty("innerDiameterMm").GetDouble() > 0.0);
    }

    [Fact]
    public async Task McpEnvelope_ErrorResponse_FollowsContract()
    {
        // Negative size producing non-positive diameter triggers error in RailEngine
        string args = JsonSerializer.Serialize(new
        {
            sizeUs = -50.0,
            widthMm = 2.0,
            thicknessMm = 1.5
        });

        string responseJson = await JewelryMcpRegistry.Default.ExecuteToolAsync("set_ring_rail", args);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("message", out var msgElement));
        Assert.False(string.IsNullOrWhiteSpace(msgElement.GetString()));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("not json")]
    [InlineData("{\"invalid: json")]
    public async Task McpInvocation_MalformedArgumentsJson_ReturnsErrorEnvelope(string malformed)
    {
        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("set_ring_rail", malformed);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.Contains("JSON", root.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpInvocation_UnknownTool_ReturnsErrorEnvelope()
    {
        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("unknown_nonexistent_tool", "{}");

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.Contains("Unknown tool", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task McpInvocation_EmptyToolName_ReturnsErrorEnvelope()
    {
        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("", "{}");

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("error", root.GetProperty("status").GetString());
        Assert.Contains("cannot be empty", root.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-50.0)]
    [InlineData(6.5)]
    [InlineData(10.0)]
    public async Task McpInvocation_RingRailSizeBoundaries_ValidatesRange(double testSize)
    {
        string args = JsonSerializer.Serialize(new
        {
            sizeUs = testSize,
            widthMm = 2.0,
            thicknessMm = 1.5
        });

        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("set_ring_rail", args);
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        if (testSize <= -14.3) // 11.63 + (-14.3 * 0.8128) <= 0 produces non-positive diameter
        {
            Assert.Equal("error", root.GetProperty("status").GetString());
        }
        else
        {
            Assert.Equal("success", root.GetProperty("status").GetString());
            var data = root.GetProperty("data");
            Assert.True(data.GetProperty("innerDiameterMm").GetDouble() > 0.0);
        }
    }

    [Fact]
    public async Task McpInvocation_CalculateBomAndWeights_ReturnsValidMetalBreakdown()
    {
        string args = JsonSerializer.Serialize(new
        {
            alloy = "Platinum950",
            spotPricePerGram = 45.0
        });

        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("calculate_bom_and_weights", args);
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("success", root.GetProperty("status").GetString());
        var data = root.GetProperty("data");
        Assert.True(data.GetProperty("metalVolumeMm3").GetDouble() > 0.0);
        Assert.True(data.TryGetProperty("metalWeights", out var weightsArray));
        Assert.True(weightsArray.GetArrayLength() > 0);
    }

    [Fact]
    public async Task McpInvocation_SetParametricMode_SwitchesModeInEngine()
    {
        string args = JsonSerializer.Serialize(new { mode = "FeatureTree" });
        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("set_parametric_mode", args);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("success", root.GetProperty("status").GetString());
        Assert.Equal("FeatureTree", root.GetProperty("data").GetProperty("currentMode").GetString());
    }

    [Fact]
    public async Task McpInvocation_RollbackTimeline_AppliesIndex()
    {
        string args = JsonSerializer.Serialize(new { rollbackIndex = 3 });
        string response = await JewelryMcpRegistry.Default.ExecuteToolAsync("rollback_timeline", args);

        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        Assert.Equal("success", root.GetProperty("status").GetString());
        Assert.Equal(3, root.GetProperty("data").GetProperty("rollbackIndex").GetInt32());
    }
}
