namespace RadianceStudio.MCP.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Definition of an MCP tool including its name, description, JSON schema, and execution delegate.
/// </summary>
public record McpToolDefinition
{
    /// <summary>
    /// The unique name of the MCP tool (e.g. "set_ring_rail").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Human- and LLM-readable description of what the tool accomplishes.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Valid JSON Schema object defining required parameters and types.
    /// </summary>
    public object InputSchema { get; init; } = new();

    /// <summary>
    /// Asynchronous execution delegate accepting parsed JSON arguments and returning JSON string.
    /// </summary>
    public Func<JsonElement, Task<string>> Handler { get; init; } = null!;
}

/// <summary>
/// Central registry for all 16 jewelry MCP tools.
/// Maps tool names to delegate handlers, generates valid MCP JSON Schemas,
/// and executes tools with structured JSON response envelopes:
/// { "status": "success", "data": ... } or { "status": "error", "message": ... }.
/// </summary>
public class JewelryMcpRegistry
{
    private static readonly Lazy<JewelryMcpRegistry> _defaultInstance = new(() => new JewelryMcpRegistry());

    /// <summary>
    /// Gets the default shared singleton registry instance.
    /// </summary>
    public static JewelryMcpRegistry Default => _defaultInstance.Value;

    private readonly Dictionary<string, McpToolDefinition> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly JewelryMcpTools _mcpTools;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JewelryMcpRegistry"/> class with all 16 tools registered.
    /// </summary>
    /// <param name="mcpTools">Optional underlying tools provider instance.</param>
    public JewelryMcpRegistry(JewelryMcpTools? mcpTools = null)
    {
        _mcpTools = mcpTools ?? new JewelryMcpTools();
        RegisterAllTools();
    }

    /// <summary>
    /// Gets the underlying <see cref="JewelryMcpTools"/> instance.
    /// </summary>
    public JewelryMcpTools Tools => _mcpTools;

    /// <summary>
    /// Registers a custom tool definition into the registry.
    /// </summary>
    public void RegisterTool(McpToolDefinition tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (string.IsNullOrWhiteSpace(tool.Name)) throw new ArgumentException("Tool name cannot be empty.", nameof(tool));
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Retrieves a registered tool definition by name, or null if not found.
    /// </summary>
    public McpToolDefinition? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName)) return null;
        return _tools.TryGetValue(toolName, out var tool) ? tool : null;
    }

    /// <summary>
    /// Returns a list of all registered tool names.
    /// </summary>
    public IReadOnlyList<string> GetRegisteredToolNames()
    {
        return _tools.Keys.ToList();
    }

    /// <summary>
    /// Static convenience method returning all registered tool names from the default registry.
    /// </summary>
    public static IReadOnlyList<string> RegisteredToolNames => Default.GetRegisteredToolNames();

    /// <summary>
    /// Generates valid JSON Schema definitions for all 16 registered tools according to MCP specifications.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>JSON string containing the tools catalog and their input schemas.</returns>
    public string GenerateJsonSchemas(bool indented = true)
    {
        var toolList = _tools.Values.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = t.InputSchema
        }).ToList();

        var catalog = new
        {
            tools = toolList
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(catalog, options);
    }

    /// <summary>
    /// Static convenience method to generate JSON schemas from the default registry.
    /// </summary>
    public static string ExportSchemas(bool indented = true) => Default.GenerateJsonSchemas(indented);

    /// <summary>
    /// Executes an MCP tool by name with the given JSON arguments string.
    /// Guarantees a structured JSON response envelope:
    /// { "status": "success", "data": ... } or { "status": "error", "message": ... }.
    /// </summary>
    /// <param name="toolName">The tool name (e.g. "set_ring_rail").</param>
    /// <param name="argumentsJson">JSON string arguments object.</param>
    /// <returns>Structured JSON response string.</returns>
    public async Task<string> ExecuteToolAsync(string toolName, string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return ErrorEnvelope("Tool name cannot be empty.");
        }

        if (!_tools.TryGetValue(toolName, out var toolDef))
        {
            return ErrorEnvelope($"Unknown tool '{toolName}'. Supported tools: {string.Join(", ", _tools.Keys)}.");
        }

        JsonDocument? doc = null;
        JsonElement rootElement;

        try
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                doc = JsonDocument.Parse("{}");
            }
            else
            {
                doc = JsonDocument.Parse(argumentsJson);
            }
            rootElement = doc.RootElement;
        }
        catch (JsonException ex)
        {
            return ErrorEnvelope($"Invalid JSON arguments: {ex.Message}");
        }

        try
        {
            var rawResult = await toolDef.Handler(rootElement).ConfigureAwait(false);
            return NormalizeEnvelope(rawResult);
        }
        catch (Exception ex)
        {
            return ErrorEnvelope(ex.Message);
        }
        finally
        {
            doc?.Dispose();
        }
    }

    /// <summary>
    /// Static convenience method to execute an MCP tool on the default registry.
    /// </summary>
    public static Task<string> ExecuteAsync(string toolName, string? argumentsJson) =>
        Default.ExecuteToolAsync(toolName, argumentsJson);

    private static string ErrorEnvelope(string message)
    {
        return JsonSerializer.Serialize(new
        {
            status = "error",
            message
        }, JsonOptions);
    }

    private static string NormalizeEnvelope(string rawResult)
    {
        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return JsonSerializer.Serialize(new
            {
                status = "success",
                data = new { }
            }, JsonOptions);
        }

        try
        {
            using var doc = JsonDocument.Parse(rawResult);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("status", out _))
            {
                // Already properly formatted in envelope
                return rawResult;
            }

            // Wrap arbitrary object into data
            return JsonSerializer.Serialize(new
            {
                status = "success",
                data = doc.RootElement.Clone()
            }, JsonOptions);
        }
        catch
        {
            return JsonSerializer.Serialize(new
            {
                status = "success",
                data = rawResult
            }, JsonOptions);
        }
    }

    #region Argument Parsing Helpers

    private static string? GetString(JsonElement elem, string propName, string? defaultValue = null, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
                if (prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined) return prop.ToString();
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp))
                {
                    if (aliasProp.ValueKind == JsonValueKind.String) return aliasProp.GetString();
                    if (aliasProp.ValueKind != JsonValueKind.Null && aliasProp.ValueKind != JsonValueKind.Undefined) return aliasProp.ToString();
                }
            }
        }
        return defaultValue;
    }

    private static double GetDouble(JsonElement elem, string propName, double defaultValue = 0.0, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out double val))
                {
                    return val;
                }
                if (prop.ValueKind == JsonValueKind.String &&
                    double.TryParse(prop.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                {
                    return parsed;
                }
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp))
                {
                    if (aliasProp.ValueKind == JsonValueKind.Number && aliasProp.TryGetDouble(out double val))
                    {
                        return val;
                    }
                    if (aliasProp.ValueKind == JsonValueKind.String &&
                        double.TryParse(aliasProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        return defaultValue;
    }

    private static int GetInt(JsonElement elem, string propName, int defaultValue = 0, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int val))
                {
                    return val;
                }
                if (prop.ValueKind == JsonValueKind.String &&
                    int.TryParse(prop.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int parsed))
                {
                    return parsed;
                }
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp))
                {
                    if (aliasProp.ValueKind == JsonValueKind.Number && aliasProp.TryGetInt32(out int val))
                    {
                        return val;
                    }
                    if (aliasProp.ValueKind == JsonValueKind.String &&
                        int.TryParse(aliasProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        return defaultValue;
    }

    private static bool GetBool(JsonElement elem, string propName, bool defaultValue = false, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True) return true;
                if (prop.ValueKind == JsonValueKind.False) return false;
                if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out bool parsed))
                {
                    return parsed;
                }
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp))
                {
                    if (aliasProp.ValueKind == JsonValueKind.True) return true;
                    if (aliasProp.ValueKind == JsonValueKind.False) return false;
                    if (aliasProp.ValueKind == JsonValueKind.String && bool.TryParse(aliasProp.GetString(), out bool parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        return defaultValue;
    }

    private static List<string>? GetStringList(JsonElement elem, string propName, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in prop.EnumerateArray())
                {
                    string? s = item.GetString();
                    if (s != null) list.Add(s);
                }
                return list;
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp) && aliasProp.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in aliasProp.EnumerateArray())
                    {
                        string? s = item.GetString();
                        if (s != null) list.Add(s);
                    }
                    return list;
                }
            }
        }
        return null;
    }

    private static Dictionary<string, double>? GetDoubleDictionary(JsonElement elem, string propName, params string[] aliases)
    {
        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, double>();
                foreach (var item in prop.EnumerateObject())
                {
                    if (item.Value.ValueKind == JsonValueKind.Number && item.Value.TryGetDouble(out double d))
                    {
                        dict[item.Name] = d;
                    }
                }
                return dict;
            }
            foreach (var alias in aliases)
            {
                if (elem.TryGetProperty(alias, out var aliasProp) && aliasProp.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, double>();
                    foreach (var item in aliasProp.EnumerateObject())
                    {
                        if (item.Value.ValueKind == JsonValueKind.Number && item.Value.TryGetDouble(out double d))
                        {
                            dict[item.Name] = d;
                        }
                    }
                    return dict;
                }
            }
        }
        return null;
    }

    #endregion

    #region Tool Registrations

    private void RegisterAllTools()
    {
        // Tool 1: get_scene_info
        RegisterTool(new McpToolDefinition
        {
            Name = "get_scene_info",
            Description = "Returns active document units, tolerances, object count, and parametric metadata of all jewelry objects in the scene.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    filter = new
                    {
                        type = "string",
                        description = "Optional text filter matching object name or feature type."
                    }
                },
                required = Array.Empty<string>()
            },
            Handler = args => _mcpTools.GetSceneInfoAsync(GetString(args, "filter"))
        });

        // Tool 2: set_ring_rail
        RegisterTool(new McpToolDefinition
        {
            Name = "set_ring_rail",
            Description = "Creates or updates a parametric ring rail curve using international sizing standards (US, UK, EU, JP) and procedural profiles (CanonicalCircle, EuroShank, CushionSquircle, BypassOverlap).",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sizeUs = new { type = "number", description = "Ring size on the US standard scale (e.g. 7.0)." },
                    standard = new { type = "string", @enum = new[] { "US", "UK", "EU", "JP" }, description = "International ring sizing standard." },
                    profileType = new { type = "string", @enum = new[] { "CanonicalCircle", "EuroShank", "CushionSquircle", "BypassOverlap" }, description = "Rail geometry profile." },
                    widthMm = new { type = "number", description = "Nominal band width in millimeters." },
                    thicknessMm = new { type = "number", description = "Nominal band thickness in millimeters." },
                    cornerRadius = new { type = "number", description = "Corner radius in millimeters for EuroShank profile." },
                    superellipseExponent = new { type = "number", description = "Superellipse squircle exponent for CushionSquircle profile." },
                    bypassOverlapDeg = new { type = "number", description = "Overlap angle in degrees for BypassOverlap profile." },
                    bypassAxialOffsetMm = new { type = "number", description = "Axial Z offset in millimeters for BypassOverlap profile." },
                    bypassRadialOffsetMm = new { type = "number", description = "Radial offset in millimeters for BypassOverlap profile." }
                },
                required = new[] { "sizeUs" }
            },
            Handler = args => _mcpTools.SetRingRailAsync(
                GetDouble(args, "sizeUs", 7.0),
                GetString(args, "standard", "US")!,
                GetString(args, "profileType", "CanonicalCircle")!,
                GetDouble(args, "widthMm", 2.0),
                GetDouble(args, "thicknessMm", 1.6),
                GetDouble(args, "cornerRadius", 1.5),
                GetDouble(args, "superellipseExponent", 3.0),
                GetDouble(args, "bypassOverlapDeg", 45.0),
                GetDouble(args, "bypassAxialOffsetMm", 2.0),
                GetDouble(args, "bypassRadialOffsetMm", 1.5))
        });

        // Tool 3: set_shank
        RegisterTool(new McpToolDefinition
        {
            Name = "set_shank",
            Description = "Sweeps a parametric shank solid along a rail curve with smoothstep tapering and optional under-gallery azurador hollow chamber.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    targetRailId = new { type = "string", description = "Target ring rail object GUID." },
                    railId = new { type = "string", description = "Target ring rail object GUID." },
                    profileType = new { type = "string", @enum = new[] { "Flat", "DShape", "ComfortFit", "KnifeEdge", "Cathedral" }, description = "Cross-section profile type." },
                    bottomWidthMm = new { type = "number", description = "Base width in millimeters at palm." },
                    bottomThicknessMm = new { type = "number", description = "Base thickness in millimeters at palm." },
                    topWidthMm = new { type = "number", description = "Shoulder width in millimeters near head." },
                    topThicknessMm = new { type = "number", description = "Shoulder thickness in millimeters near head." },
                    hasUnderGallery = new { type = "boolean", description = "Whether to hollow out an under-gallery azurador chamber." },
                    underGalleryWallMm = new { type = "number", description = "Minimum metal wall thickness around chamber (>= 0.80 mm)." },
                    underGalleryClearanceMm = new { type = "number", description = "Floor clearance above inner finger rail (>= 0.80 mm)." },
                    underGallerySpanDeg = new { type = "number", description = "Angular span in degrees for under-gallery." },
                    crownRatio = new { type = "number", description = "Comfort-fit crown dome ratio in [0, 0.5)." },
                    knifeEdgeApexRadiusMm = new { type = "number", description = "Apex fillet radius for knife-edge profile." },
                    cathedralArchHeightMm = new { type = "number", description = "Shoulder elevation for cathedral arch profile." }
                },
                required = Array.Empty<string>()
            },
            Handler = args => _mcpTools.SetShankAsync(
                GetString(args, "targetRailId", null, "railId"),
                GetString(args, "profileType", "ComfortFit")!,
                GetDouble(args, "bottomWidthMm", 2.2),
                GetDouble(args, "bottomThicknessMm", 1.6),
                GetDouble(args, "topWidthMm", 2.8),
                GetDouble(args, "topThicknessMm", 2.0),
                GetBool(args, "hasUnderGallery", false),
                GetDouble(args, "underGalleryWallMm", 0.85),
                GetDouble(args, "underGalleryClearanceMm", 0.85),
                GetDouble(args, "underGallerySpanDeg", 60.0),
                GetDouble(args, "crownRatio", 0.25),
                GetDouble(args, "knifeEdgeApexRadiusMm", 0.15),
                GetDouble(args, "cathedralArchHeightMm", 0.0))
        });

        // Tool 4: set_gem_row
        RegisterTool(new McpToolDefinition
        {
            Name = "set_gem_row",
            Description = "Distributes a row of gems along a 3D curve using arc-length spacing and Gram-Schmidt orthonormal frames.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    targetCurveId = new { type = "string", description = "Target curve GUID." },
                    gemDiameterMm = new { type = "number", description = "Stone diameter in millimeters." },
                    spacingMm = new { type = "number", description = "Inter-stone spacing in millimeters." },
                    cutType = new { type = "string", @enum = new[] { "RoundBrilliant", "Princess", "Oval", "Pear", "Emerald", "Marquise", "Cushion", "Asscher", "Radiant", "Heart", "Trilliant", "Baguette", "TaperedBaguette" } },
                    startOffsetMm = new { type = "number", description = "Offset from curve start in millimeters." },
                    endOffsetMm = new { type = "number", description = "Offset from curve end in millimeters." }
                },
                required = new[] { "gemDiameterMm" }
            },
            Handler = args => _mcpTools.SetGemRowAsync(
                GetString(args, "targetCurveId"),
                GetDouble(args, "gemDiameterMm", 2.0),
                GetDouble(args, "spacingMm", 0.25),
                GetString(args, "cutType", "RoundBrilliant")!,
                GetDouble(args, "startOffsetMm", 0.0),
                GetDouble(args, "endOffsetMm", 0.0))
        });

        // Tool 5: set_pave_surface
        RegisterTool(new McpToolDefinition
        {
            Name = "set_pave_surface",
            Description = "Computes dense stone packing and retention micro-beads across a 3D surface with Lloyd's relaxation.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    targetSurfaceId = new { type = "string", description = "Target surface GUID." },
                    stoneDiameterMm = new { type = "number", description = "Stone diameter in millimeters." },
                    gemDiameterMm = new { type = "number", description = "Stone diameter in millimeters (alias)." },
                    stoneSpacingMm = new { type = "number", description = "Inter-stone spacing in millimeters." },
                    spacingMm = new { type = "number", description = "Inter-stone spacing in millimeters (alias)." },
                    patternType = new { type = "string", @enum = new[] { "Hexagonal", "OffsetLinear", "Grid", "Fishtail" } },
                    pattern = new { type = "string", @enum = new[] { "Hexagonal", "OffsetLinear", "Grid", "Fishtail" } },
                    borderOffsetMm = new { type = "number", description = "Margin offset from surface boundary in millimeters." },
                    beadDiameterMm = new { type = "number", description = "Retention bead sphere diameter in millimeters." },
                    beadHeightMm = new { type = "number", description = "Elevation of retention beads in millimeters." },
                    prongType = new { type = "string", @enum = new[] { "Bead", "BrightCut", "FishtailV", "Castle" } }
                },
                required = new[] { "stoneDiameterMm" }
            },
            Handler = args => _mcpTools.SetPaveSurfaceAsync(
                GetString(args, "targetSurfaceId"),
                GetDouble(args, "stoneDiameterMm", 1.3, "gemDiameterMm"),
                GetDouble(args, "stoneSpacingMm", 0.15, "spacingMm"),
                GetString(args, "patternType", "Hexagonal", "pattern")!,
                GetDouble(args, "borderOffsetMm", 0.2),
                GetDouble(args, "beadDiameterMm", 0.45),
                GetDouble(args, "beadHeightMm", 0.35),
                GetString(args, "prongType", "Bead")!)
        });

        // Tool 6: set_prong_head
        RegisterTool(new McpToolDefinition
        {
            Name = "set_prong_head",
            Description = "Creates a procedural prong setting head with configurable prong count (4, 6, 8), styles (Claw, Dome, V-Prong), and base/gallery wires.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    gemId = new { type = "string", description = "Target center gemstone GUID." },
                    cutType = new { type = "string", @enum = new[] { "RoundBrilliant", "Princess", "Oval", "Pear", "Emerald", "Marquise", "Cushion", "Asscher", "Radiant", "Heart", "Trilliant", "Baguette", "TaperedBaguette" } },
                    gemDiameterMm = new { type = "number", description = "Center gem diameter in millimeters." },
                    prongCount = new { type = "integer", @enum = new[] { 4, 6, 8 } },
                    prongStyle = new { type = "string", @enum = new[] { "RoundDome", "Flat", "Claw", "VProng" } },
                    prongDiameterMm = new { type = "number", description = "Wire diameter of prongs in millimeters." },
                    prongHeightMm = new { type = "number", description = "Total vertical height of prongs in millimeters." },
                    girdleOverhangMm = new { type = "number", description = "Overhang of prongs over gemstone girdle in millimeters." },
                    rakeAngleDeg = new { type = "number", description = "Inward rake angle in degrees." },
                    includeBaseWire = new { type = "boolean", description = "Whether to include structural bottom base wire ring." },
                    includeGalleryWire = new { type = "boolean", description = "Whether to include mid-level gallery wire ring." }
                },
                required = new[] { "prongCount" }
            },
            Handler = args => _mcpTools.SetProngHeadAsync(
                GetString(args, "gemId"),
                GetString(args, "cutType", "RoundBrilliant")!,
                GetDouble(args, "gemDiameterMm", 6.5),
                GetInt(args, "prongCount", 4),
                GetString(args, "prongStyle", "Claw")!,
                GetDouble(args, "prongDiameterMm", 0.85),
                GetDouble(args, "prongHeightMm", 4.5),
                GetDouble(args, "girdleOverhangMm", 0.25),
                GetDouble(args, "rakeAngleDeg", 10.0),
                GetBool(args, "includeBaseWire", true),
                GetBool(args, "includeGalleryWire", true))
        });

        // Tool 7: set_bezel
        RegisterTool(new McpToolDefinition
        {
            Name = "set_bezel",
            Description = "Creates a procedural full or semi-bezel setting with bearing ledge, casting draft angle, and light hole.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    gemId = new { type = "string", description = "Target center gemstone GUID." },
                    cutType = new { type = "string", @enum = new[] { "RoundBrilliant", "Princess", "Oval", "Pear", "Emerald", "Marquise", "Cushion", "Asscher", "Radiant", "Heart", "Trilliant", "Baguette", "TaperedBaguette" } },
                    gemDiameterMm = new { type = "number", description = "Center gem diameter in millimeters." },
                    wallThicknessMm = new { type = "number", description = "Metal wall thickness in millimeters (>= 0.40 mm)." },
                    heightAboveGirdleMm = new { type = "number", description = "Height of bezel rim extending above girdle in millimeters." },
                    isSemiBezel = new { type = "boolean", description = "Whether the setting is a semi-bezel with opposing cutout arches." },
                    semiBezelOpeningAngleDeg = new { type = "number", description = "Angular opening of semi-bezel cutouts in degrees." },
                    draftAngleDeg = new { type = "number", description = "Outward draft angle in degrees for casting release." },
                    seatDepthPercent = new { type = "number", description = "Depth of bearing seat ledge as percentage of girdle." },
                    lightHoleDiameterPercent = new { type = "number", description = "Diameter of culet light hole as percentage of gem diameter." }
                },
                required = Array.Empty<string>()
            },
            Handler = args => _mcpTools.SetBezelAsync(
                GetString(args, "gemId"),
                GetString(args, "cutType", "RoundBrilliant")!,
                GetDouble(args, "gemDiameterMm", 6.5),
                GetDouble(args, "wallThicknessMm", 0.80),
                GetDouble(args, "heightAboveGirdleMm", 0.60),
                GetBool(args, "isSemiBezel", false),
                GetDouble(args, "semiBezelOpeningAngleDeg", 60.0),
                GetDouble(args, "draftAngleDeg", 3.0),
                GetDouble(args, "seatDepthPercent", 40.0),
                GetDouble(args, "lightHoleDiameterPercent", 50.0))
        });

        // Tool 8: set_halo
        RegisterTool(new McpToolDefinition
        {
            Name = "set_halo",
            Description = "Computes offset perimeter halo melee distribution, platform under-rail tray, and micro-prongs.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    gemId = new { type = "string", description = "Center gemstone GUID." },
                    centerCutType = new { type = "string", @enum = new[] { "RoundBrilliant", "Princess", "Oval", "Pear", "Emerald", "Marquise", "Cushion", "Asscher", "Radiant", "Heart", "Trilliant", "Baguette", "TaperedBaguette" } },
                    centerGemDiameterMm = new { type = "number", description = "Center gem diameter in millimeters." },
                    meleeDiameterMm = new { type = "number", description = "Diameter of surrounding melee stones in millimeters." },
                    spacingMm = new { type = "number", description = "Spacing between adjacent melee stones in millimeters." },
                    guardDistanceMm = new { type = "number", description = "Guard distance between center gem and halo stones in millimeters." },
                    dropHeightMm = new { type = "number", description = "Vertical drop of halo plane relative to center gem in millimeters." },
                    trayDepthMm = new { type = "number", description = "Depth of under-rail platform tray in millimeters." },
                    trayWallThicknessMm = new { type = "number", description = "Wall thickness of platform tray in millimeters." }
                },
                required = new[] { "meleeDiameterMm" }
            },
            Handler = args => _mcpTools.SetHaloAsync(
                GetString(args, "gemId"),
                GetString(args, "centerCutType", "RoundBrilliant")!,
                GetDouble(args, "centerGemDiameterMm", 6.5),
                GetDouble(args, "meleeDiameterMm", 1.3),
                GetDouble(args, "spacingMm", 0.15),
                GetDouble(args, "guardDistanceMm", 0.25),
                GetDouble(args, "dropHeightMm", -0.30),
                GetDouble(args, "trayDepthMm", 1.2),
                GetDouble(args, "trayWallThicknessMm", 0.50))
        });

        // Tool 9: set_cutter
        RegisterTool(new McpToolDefinition
        {
            Name = "set_cutter",
            Description = "Generates a solid stone cutter with seat angle (70-deg, 90-deg, flat shelf, conical), girdle clearance, and light hole punch.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    gemId = new { type = "string", description = "Associated gemstone GUID." },
                    cutType = new { type = "string", @enum = new[] { "RoundBrilliant", "Princess", "Oval", "Pear", "Emerald", "Marquise", "Cushion", "Asscher", "Radiant", "Heart", "Trilliant", "Baguette", "TaperedBaguette" } },
                    gemDiameterMm = new { type = "number", description = "Gemstone diameter in millimeters." },
                    girdleClearanceMm = new { type = "number", description = "Girdle tolerance clearance band in millimeters." },
                    girdleClearancePercent = new { type = "number", description = "Girdle tolerance clearance percentage or band." },
                    seatAngle = new { type = "string", @enum = new[] { "Degree70", "Degree90", "FlatShelf", "Conical" } },
                    seatAngleDegrees = new { type = "string", @enum = new[] { "Degree70", "Degree90", "FlatShelf", "Conical" }, description = "Seat angle degrees." },
                    crownHeightExtensionMm = new { type = "number", description = "Upper crown piercing extension in millimeters." },
                    lightHoleDepthExtensionMm = new { type = "number", description = "Lower light hole punch extension in millimeters." },
                    lightHoleDiameterRatio = new { type = "number", description = "Diameter ratio of light hole punch relative to gem." },
                    taperShaft = new { type = "boolean", description = "Whether to taper the lower light hole punch shaft." }
                },
                required = Array.Empty<string>()
            },
            Handler = args =>
            {
                string seatVal = GetString(args, "seatAngle", "Degree70", "seatAngleDegrees")!;
                if (seatVal == "70" || seatVal == "70.0" || seatVal == "70deg") seatVal = "Degree70";
                if (seatVal == "90" || seatVal == "90.0" || seatVal == "90deg") seatVal = "Degree90";
                return _mcpTools.SetCutterAsync(
                    GetString(args, "gemId"),
                    GetString(args, "cutType", "RoundBrilliant")!,
                    GetDouble(args, "gemDiameterMm", 6.5),
                    GetDouble(args, "girdleClearanceMm", 0.10, "girdleClearancePercent"),
                    seatVal,
                    GetDouble(args, "crownHeightExtensionMm", 1.5),
                    GetDouble(args, "lightHoleDepthExtensionMm", 2.0),
                    GetDouble(args, "lightHoleDiameterRatio", 0.50),
                    GetBool(args, "taperShaft", true));
            }
        });

        // Tool 10: apply_boolean_cutters
        RegisterTool(new McpToolDefinition
        {
            Name = "apply_boolean_cutters",
            Description = "Subtracts cutter solids from a target metal body using adaptive multi-pass boolean difference.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    targetMetalId = new { type = "string", description = "GUID of the solid metal geometry to carve." },
                    metalId = new { type = "string", description = "GUID of the solid metal geometry to carve (alias)." },
                    cutterIds = new { type = "array", items = new { type = "string" }, description = "List of cutter object GUIDs. Auto-gathers cutters if omitted." },
                    baseToleranceMm = new { type = "number", description = "Initial boolean difference tolerance in millimeters (default 0.001 mm)." }
                },
                required = Array.Empty<string>()
            },
            Handler = args => _mcpTools.ApplyBooleanCuttersAsync(
                GetString(args, "metalId", null, "targetMetalId"),
                GetStringList(args, "cutterIds"),
                GetDouble(args, "baseToleranceMm", 0.001))
        });

        // Tool 11: calculate_bom_and_weights
        RegisterTool(new McpToolDefinition
        {
            Name = "calculate_bom_and_weights",
            Description = "Calculates solid metal volume, physical cast weights across precious alloys (14K, 18K, Pt, Ag, Wax), gem counts, and carat breakdown.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    projectTitle = new { type = "string", description = "Title for the Bill of Materials report." },
                    alloy = new { type = "string", @enum = new[] { "Gold24K", "Gold18KYellow", "Gold18KWhite", "Gold18KRose", "Gold14KYellow", "Gold14KWhite", "Platinum950", "Silver925", "CastingWax" }, description = "Precious metal alloy." },
                    metalAlloy = new { type = "string", @enum = new[] { "Gold24K", "Gold18KYellow", "Gold18KWhite", "Gold18KRose", "Gold14KYellow", "Gold14KWhite", "Platinum950", "Silver925", "CastingWax" }, description = "Precious metal alloy (alias)." },
                    spotPricePerGram = new { type = "object", additionalProperties = new { type = "number" }, description = "Optional dictionary or number of spot prices per gram." },
                    spotPricesPerGram = new { type = "object", additionalProperties = new { type = "number" }, description = "Optional dictionary of spot prices per gram keyed by alloy name." }
                },
                required = Array.Empty<string>()
            },
            Handler = args =>
            {
                var spotDict = GetDoubleDictionary(args, "spotPricesPerGram", "spotPricePerGram");
                if (spotDict == null)
                {
                    double singleSpot = GetDouble(args, "spotPricePerGram", 0.0, "spotPricesPerGram");
                    if (singleSpot > 0.0)
                    {
                        string alloyKey = GetString(args, "alloy", "Gold14KYellow", "metalAlloy")!;
                        spotDict = new Dictionary<string, double> { [alloyKey] = singleSpot };
                    }
                }
                return _mcpTools.CalculateBomAndWeightsAsync(
                    GetString(args, "projectTitle", "RadianceStudio Jewelry Design")!,
                    GetString(args, "alloy", "Gold14KYellow", "metalAlloy")!,
                    spotDict);
            }
        });

        // Tool 12: check_wall_thickness
        RegisterTool(new McpToolDefinition
        {
            Name = "check_wall_thickness",
            Description = "Inspects structural wall thickness using inverted normal ray-casting against 0.80 mm general wall and 0.60 mm prong neck invariants.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    targetObjectId = new { type = "string", description = "GUID of target object to inspect. Inspects active metal solids if omitted." },
                    objectId = new { type = "string", description = "GUID of target object to inspect. Inspects active metal solids if omitted (alias)." },
                    minGeneralWallMm = new { type = "number", description = "Minimum allowable general wall thickness in millimeters (default 0.80 mm)." },
                    minThresholdMm = new { type = "number", description = "Minimum allowable general wall thickness in millimeters (default 0.80 mm) (alias)." },
                    minProngNeckMm = new { type = "number", description = "Minimum allowable prong neck thickness in millimeters (default 0.60 mm)." },
                    onlyViolations = new { type = "boolean", description = "Whether to filter output to only violating locations." }
                },
                required = Array.Empty<string>()
            },
            Handler = args => _mcpTools.CheckWallThicknessAsync(
                GetString(args, "targetObjectId", null, "objectId"),
                GetDouble(args, "minGeneralWallMm", 0.80, "minThresholdMm"),
                GetDouble(args, "minProngNeckMm", 0.60),
                GetBool(args, "onlyViolations", true))
        });

        // Tool 13: check_and_export
        RegisterTool(new McpToolDefinition
        {
            Name = "check_and_export",
            Description = "Validates closed 2-manifold geometry and exports watertight binary STL for casting and 3D printing.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFilePath = new { type = "string", description = "Destination file path for binary STL export." },
                    targetVoxelSizeMm = new { type = "number", description = "ShrinkWrap target edge length in millimeters (default 0.05 mm)." },
                    validateManifold = new { type = "boolean", description = "Whether to enforce strict 2-manifold watertightness." }
                },
                required = new[] { "outputFilePath" }
            },
            Handler = args => _mcpTools.CheckAndExportAsync(
                GetString(args, "outputFilePath", "radiance_export.stl")!,
                GetDouble(args, "targetVoxelSizeMm", 0.05),
                GetBool(args, "validateManifold", true))
        });

        // Tool 14: set_parametric_mode
        RegisterTool(new McpToolDefinition
        {
            Name = "set_parametric_mode",
            Description = "Switches timeline evaluation between StudioClassic (flat object-centric) and FeatureTree (hierarchical history tree).",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    mode = new { type = "string", @enum = new[] { "StudioClassic", "FeatureTree" }, description = "Target parametric mode." }
                },
                required = new[] { "mode" }
            },
            Handler = args => _mcpTools.SetParametricModeAsync(GetString(args, "mode", "StudioClassic")!)
        });

        // Tool 15: rollback_timeline
        RegisterTool(new McpToolDefinition
        {
            Name = "rollback_timeline",
            Description = "Rolls back the FeatureTree timeline to a specific order index, freezing downstream operations.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    rollbackIndex = new { type = "integer", description = "Order index cutoff for rollback, or -1 for end of timeline." }
                },
                required = new[] { "rollbackIndex" }
            },
            Handler = args => _mcpTools.RollbackTimelineAsync(GetInt(args, "rollbackIndex", -1))
        });

        // Tool 16: suppress_feature
        RegisterTool(new McpToolDefinition
        {
            Name = "suppress_feature",
            Description = "Suppresses or resumes a specific feature node in the parametric timeline, optionally cascading to child operations.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    featureId = new { type = "string", description = "GUID of the feature node to suppress or resume." },
                    isSuppressed = new { type = "boolean", description = "True to suppress feature, false to resume." },
                    cascadeToChildren = new { type = "boolean", description = "Whether to cascade suppression down to dependent children." }
                },
                required = new[] { "featureId", "isSuppressed" }
            },
            Handler = args => _mcpTools.SuppressFeatureAsync(
                GetString(args, "featureId", Guid.Empty.ToString())!,
                GetBool(args, "isSuppressed", false),
                GetBool(args, "cascadeToChildren", true))
        });
    }

    #endregion
}
