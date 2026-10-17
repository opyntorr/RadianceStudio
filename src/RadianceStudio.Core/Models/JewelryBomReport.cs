namespace RadianceStudio.Core.Models;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Comprehensive Bill of Materials (BOM), metallurgical casting estimate, and manufacturing diagnostic report.
/// </summary>
public record JewelryBomReport
{
    private readonly bool? _isProductionReady;

    /// <summary>
    /// Descriptive title or SKU identifier of the jewelry project.
    /// </summary>
    public string ProjectTitle { get; init; } = "Untitled Project";

    /// <summary>
    /// UTC timestamp of report generation.
    /// </summary>
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Computed metal weights and costs across all supported precious metal alloys and casting wax.
    /// </summary>
    public List<MetalWeightInfo> MetalWeights { get; init; } = new();

    /// <summary>
    /// Total number of gemstones specified in the design.
    /// </summary>
    public int TotalGemCount { get; init; }

    /// <summary>
    /// Cumulative carat weight across all gemstones in the design.
    /// </summary>
    public double TotalCaratWeight { get; init; }

    /// <summary>
    /// Breakdown of gemstones grouped by cut type, material, and dimensions.
    /// </summary>
    public List<GemBomLineItem> GemBreakdown { get; init; } = new();

    /// <summary>
    /// Diagnostic records for any wall thickness measurements that violate minimum manufacturing thresholds.
    /// </summary>
    public List<WallThicknessDiagnostic> WallThicknessViolations { get; init; } = new();

    /// <summary>
    /// Indicates whether the piece passes all manufacturing constraints and has zero wall thickness violations.
    /// </summary>
    public bool IsProductionReady
    {
        get => _isProductionReady ?? (WallThicknessViolations == null || WallThicknessViolations.Count == 0);
        init => _isProductionReady = value;
    }

    /// <summary>
    /// Generates a structured, human-readable Markdown report suitable for production work orders and studio quotes.
    /// </summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;

        sb.AppendLine($"# Bill of Materials (BOM) Report: {ProjectTitle}");
        sb.AppendLine();
        sb.AppendLine($"- **Generated (UTC):** {CreatedUtc:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Manufacturing Status:** {(IsProductionReady ? "✅ PRODUCTION READY (PASSED)" : "❌ REVISION REQUIRED (WALL THICKNESS VIOLATIONS)")}");
        sb.AppendLine($"- **Total Gem Count:** {TotalGemCount}");
        sb.AppendLine($"- **Total Carat Weight:** {TotalCaratWeight.ToString("F3", inv)} ct");
        sb.AppendLine();

        // 1. Metal Weight Table
        sb.AppendLine("## 1. Precious Metal & Casting Estimates");
        sb.AppendLine();
        sb.AppendLine("| Alloy | Density (g/cm³) | Volume (mm³) | Weight (g) | Weight (dwt) | Weight (oz t) | Est. Cost |");
        sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: | :---: |");
        foreach (var mw in MetalWeights)
        {
            string costStr = mw.EstimatedCost > 0.0 ? mw.EstimatedCost.ToString("C2", inv) : "—";
            sb.AppendLine(
                $"| {mw.Alloy,-14} | {mw.DensityGPerCm3.ToString("F2", inv),6} | {mw.VolumeMm3.ToString("F2", inv),8} | {mw.WeightGrams.ToString("F2", inv),7} | {mw.WeightDwt.ToString("F2", inv),7} | {mw.WeightTroyOunces.ToString("F3", inv),7} | {costStr,9} |");
        }
        sb.AppendLine();

        // 2. Gemstone Schedule
        sb.AppendLine("## 2. Gemstone Schedule");
        sb.AppendLine();
        if (GemBreakdown.Count == 0)
        {
            sb.AppendLine("_No gemstones specified in this design._");
        }
        else
        {
            sb.AppendLine("| Cut Type | Material | Count | Avg Diameter (mm) | Total Carats |");
            sb.AppendLine("| :--- | :--- | :---: | :---: | :---: |");
            foreach (var item in GemBreakdown)
            {
                sb.AppendLine(
                    $"| {item.CutType,-14} | {item.GemMaterial,-12} | {item.Count,5} | {item.AverageDiameterMm.ToString("F2", inv),8} | {item.TotalCarats.ToString("F3", inv),8} ct |");
            }
        }
        sb.AppendLine();

        // 3. Wall Thickness & Quality Diagnostics
        sb.AppendLine("## 3. Wall Thickness Quality Diagnostics");
        sb.AppendLine();
        if (WallThicknessViolations.Count == 0)
        {
            sb.AppendLine("✅ **Zero thickness violations detected.** All evaluated features satisfy or exceed structural CAM casting thresholds.");
        }
        else
        {
            sb.AppendLine($"⚠️ **{WallThicknessViolations.Count} violation(s) detected.** The following regions are below minimum casting thickness:");
            sb.AppendLine();
            sb.AppendLine("| Feature | Measured (mm) | Required (mm) | Deficit (mm) | Location (X, Y, Z) |");
            sb.AppendLine("| :--- | :---: | :---: | :---: | :--- |");
            foreach (var v in WallThicknessViolations)
            {
                double deficit = v.RequiredMinimumMm - v.MeasuredThicknessMm;
                sb.AppendLine(
                    $"| {v.FeatureDescription,-18} | {v.MeasuredThicknessMm.ToString("F3", inv),7} | {v.RequiredMinimumMm.ToString("F3", inv),7} | {deficit.ToString("F3", inv),7} | ({v.Location.X.ToString("F2", inv)}, {v.Location.Y.ToString("F2", inv)}, {v.Location.Z.ToString("F2", inv)}) |");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes the BOM report into structured JSON format.
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        return JsonSerializer.Serialize(this, options);
    }
}
