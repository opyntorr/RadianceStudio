namespace RadianceStudio.Core.Tests.Manufacturing;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

public class ManufacturingEngineTests
{
    #region Metal Weight & Alloy Densities Tests

    [Fact]
    public void PreciousMetalAlloy_Densities_MatchExactMetallurgicalStandards()
    {
        Assert.Equal(19.32, PreciousMetalAlloy.Gold24K.GetDensityGPerCm3());
        Assert.Equal(15.58, PreciousMetalAlloy.Gold18KYellow.GetDensityGPerCm3());
        Assert.Equal(15.90, PreciousMetalAlloy.Gold18KWhite.GetDensityGPerCm3());
        Assert.Equal(15.55, PreciousMetalAlloy.Gold18KRose.GetDensityGPerCm3());
        Assert.Equal(13.07, PreciousMetalAlloy.Gold14KYellow.GetDensityGPerCm3());
        Assert.Equal(12.80, PreciousMetalAlloy.Gold14KWhite.GetDensityGPerCm3());
        Assert.Equal(21.45, PreciousMetalAlloy.Platinum950.GetDensityGPerCm3());
        Assert.Equal(10.36, PreciousMetalAlloy.Silver925.GetDensityGPerCm3());
        Assert.Equal(0.96, PreciousMetalAlloy.CastingWax.GetDensityGPerCm3());
    }

    [Fact]
    public void WaxToMetalMultiplier_CalculatesExpectedRatios()
    {
        // 18K Yellow: 15.58 / 0.96 ≈ 16.2291666...
        double yellow18KFactor = ManufacturingEngine.GetWaxToMetalMultiplier(PreciousMetalAlloy.Gold18KYellow);
        Assert.True(Math.Abs(yellow18KFactor - (15.58 / 0.96)) < 1e-6);
        Assert.True(Math.Abs(yellow18KFactor - 16.229) < 0.01);

        // Platinum950: 21.45 / 0.96 ≈ 22.34375
        double platFactor = ManufacturingEngine.GetWaxToMetalMultiplier(PreciousMetalAlloy.Platinum950);
        Assert.Equal(21.45 / 0.96, platFactor, 6);

        // Wax to Wax: exactly 1.0
        double waxFactor = ManufacturingEngine.GetWaxToMetalMultiplier(PreciousMetalAlloy.CastingWax);
        Assert.Equal(1.0, waxFactor, 6);
    }

    [Fact]
    public void CastingResolutionProfiles_MapToPrescribedVoxelSizes()
    {
        Assert.Equal(0.03, CastingResolutionProfile.UltraPrecision.GetVoxelSizeMm());
        Assert.Equal(0.05, CastingResolutionProfile.StandardCasting.GetVoxelSizeMm());
        Assert.Equal(0.08, CastingResolutionProfile.RapidPrototyping.GetVoxelSizeMm());
    }

    [Fact]
    public void CalculateMetalWeights_OneCubicCentimeter_ComputesExactConversions()
    {
        // 1 cm³ = 1000 mm³
        double volumeMm3 = 1000.0;
        var spotPrices = new Dictionary<PreciousMetalAlloy, double>
        {
            [PreciousMetalAlloy.Gold18KYellow] = 65.50
        };

        var weights = ManufacturingEngine.CalculateMetalWeights(volumeMm3, spotPrices);

        Assert.Equal(9, weights.Count);

        var gold18K = weights.Single(w => w.Alloy == PreciousMetalAlloy.Gold18KYellow);
        Assert.Equal(15.58, gold18K.DensityGPerCm3);
        Assert.Equal(1000.0, gold18K.VolumeMm3);
        Assert.Equal(15.58, gold18K.WeightGrams, 4);

        // Verify pennyweight (dwt): grams / 1.55517384
        double expectedDwt = 15.58 / 1.55517384;
        Assert.Equal(expectedDwt, gold18K.WeightDwt, 4);

        // Verify troy ounce (oz t): grams / 31.1034768
        double expectedTroyOz = 15.58 / 31.1034768;
        Assert.Equal(expectedTroyOz, gold18K.WeightTroyOunces, 4);

        // Verify cost calculation: 15.58 * 65.50 = 1020.49
        Assert.Equal(15.58 * 65.50, gold18K.EstimatedCost, 2);
    }

    [Fact]
    public void CalculateMetalWeights_FromWatertightCubeMesh_IntegratesDivergenceTheorem()
    {
        // Watertight cube 10mm x 10mm x 10mm = 1000 mm³
        var cube = CreateSolidCube(10.0);
        var weights = ManufacturingEngine.CalculateMetalWeights(cube);

        var silver = weights.Single(w => w.Alloy == PreciousMetalAlloy.Silver925);
        Assert.Equal(1000.0, silver.VolumeMm3, 2);
        Assert.Equal(10.36, silver.WeightGrams, 2);
    }

    #endregion

    #region Wall Thickness Inspection Tests

    [Fact]
    public void InspectWallThickness_HollowTube_AccuratelyDetectsViolations_WhenThicknessBelowThreshold()
    {
        // Hollow cylinder with outer radius 5.0mm, inner radius 4.3mm -> wall thickness = 0.70mm
        var tube = CreateHollowTube(radiusOuter: 5.0, radiusInner: 4.3, height: 10.0, segments: 16);

        // Threshold = 0.80 mm -> 0.70 mm is a violation
        var diagnosticsBelow = ManufacturingEngine.InspectWallThickness(tube, minGeneralWallMm: 0.80);

        Assert.NotEmpty(diagnosticsBelow);

        // Cylindrical outer wall vertices should measure approx 0.70 mm thickness
        var cylindricalDiagnostics = diagnosticsBelow
            .Where(d => Math.Abs(d.Location.Z - 5.0) < 4.0)
            .ToList();

        Assert.NotEmpty(cylindricalDiagnostics);
        foreach (var diag in cylindricalDiagnostics)
        {
            Assert.True(Math.Abs(diag.MeasuredThicknessMm - 0.70) < 0.05,
                $"Expected wall thickness ~0.70mm, but measured {diag.MeasuredThicknessMm:F4}");
            Assert.True(diag.IsViolation);
        }

        // Violations convenience helper should return violations
        var violations = ManufacturingEngine.GetWallThicknessViolations(tube, minGeneralWallMm: 0.80);
        Assert.NotEmpty(violations);
        Assert.All(violations, v => Assert.True(v.IsViolation));
    }

    [Fact]
    public void InspectWallThickness_HollowTube_ReportsZeroViolations_WhenThicknessMeetsThreshold()
    {
        // Hollow cylinder with outer radius 5.0mm, inner radius 4.3mm -> wall thickness = 0.70mm
        var tube = CreateHollowTube(radiusOuter: 5.0, radiusInner: 4.3, height: 10.0, segments: 16);

        // Threshold = 0.60 mm -> 0.70 mm is compliant (NOT a violation)
        var violations = ManufacturingEngine.GetWallThicknessViolations(tube, minGeneralWallMm: 0.60);

        Assert.Empty(violations);
    }

    [Fact]
    public void InspectWallThickness_SolidCube_MeasuresFullCubeThickness()
    {
        // Solid cube 5.0mm wide (-2.5 to +2.5)
        var cube = CreateSolidCube(5.0);

        var diagnostics = ManufacturingEngine.InspectWallThickness(cube, minGeneralWallMm: 0.80);

        Assert.NotEmpty(diagnostics);
        // Distance through a 5.0mm solid cube along normal is 5.0mm (>= 0.80mm required)
        Assert.All(diagnostics, d =>
        {
            Assert.False(d.IsViolation);
            Assert.True(d.MeasuredThicknessMm >= 4.9);
        });
    }

    [Fact]
    public void InspectWallThickness_CustomFeatureClassifier_AssignsFeatureAndAppliesSpecializedThreshold()
    {
        var cube = CreateSolidCube(0.70); // 0.70mm thick cube

        // minGeneralWallMm = 0.80 (0.70 violates general), minProngNeckMm = 0.60 (0.70 passes prong neck)
        var diags = ManufacturingEngine.InspectWallThickness(
            cube,
            minGeneralWallMm: 0.80,
            minProngNeckMm: 0.60,
            featureClassifier: pt => "Prong Neck");

        Assert.NotEmpty(diags);
        Assert.All(diags, d =>
        {
            Assert.Equal("Prong Neck", d.FeatureDescription);
            Assert.Equal(0.60, d.RequiredMinimumMm);
            Assert.False(d.IsViolation, $"0.70mm should satisfy 0.60mm prong neck requirement, but got {d.MeasuredThicknessMm}");
        });
    }

    #endregion

    #region Bill of Materials (BOM) Tests

    [Fact]
    public void GenerateBomReport_AggregatesTotalsAndSetsProductionReadyFlag()
    {
        var gems = new List<(GemDimensions gem, CaratEstimator.GemMaterial material)>
        {
            (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 6.5), CaratEstimator.GemMaterial.Diamond),
            (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 1.5), CaratEstimator.GemMaterial.Diamond),
            (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 1.5), CaratEstimator.GemMaterial.Diamond),
            (GemDimensions.GetDefault(GemCutType.Emerald, 4.0), CaratEstimator.GemMaterial.Sapphire)
        };

        var spotPrices = new Dictionary<PreciousMetalAlloy, double>
        {
            [PreciousMetalAlloy.Gold18KWhite] = 72.00
        };

        // Case 1: BOM with zero violations -> Production Ready
        var reportReady = ManufacturingEngine.GenerateBomReport(
            projectTitle: "Classic Halo Ring",
            metalVolumeMm3: 350.0,
            gems: gems,
            diagnostics: null,
            spotPricesPerGram: spotPrices);

        Assert.Equal("Classic Halo Ring", reportReady.ProjectTitle);
        Assert.Equal(4, reportReady.TotalGemCount);
        Assert.True(reportReady.TotalCaratWeight > 1.0);
        Assert.Equal(3, reportReady.GemBreakdown.Count); // 6.5mm Dia, 1.5mm Dia (qty 2), 4.0mm Sapp
        Assert.True(reportReady.IsProductionReady);
        Assert.Empty(reportReady.WallThicknessViolations);

        var gold18KWhite = reportReady.MetalWeights.Single(m => m.Alloy == PreciousMetalAlloy.Gold18KWhite);
        Assert.True(gold18KWhite.WeightGrams > 0.0);
        Assert.True(gold18KWhite.EstimatedCost > 0.0);

        // Case 2: BOM with thickness violations -> NOT Production Ready
        var violations = new List<WallThicknessDiagnostic>
        {
            new WallThicknessDiagnostic(new Point3D(0, 0, 5), Vector3D.UnitZ, 0.45, 0.80, "Prong Neck")
        };

        var reportViolations = ManufacturingEngine.GenerateBomReport(
            projectTitle: "Classic Halo Ring (Thin)",
            metalVolumeMm3: 350.0,
            gems: gems,
            diagnostics: violations,
            spotPricesPerGram: spotPrices);

        Assert.False(reportViolations.IsProductionReady);
        Assert.Single(reportViolations.WallThicknessViolations);
    }

    [Fact]
    public void JewelryBomReport_ToMarkdown_GeneratesValidStructuredMarkdown()
    {
        var gems = new List<(GemDimensions gem, CaratEstimator.GemMaterial material)>
        {
            (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 6.5), CaratEstimator.GemMaterial.Diamond)
        };

        var report = ManufacturingEngine.GenerateBomReport("BOM Markdown Test", 200.0, gems);
        string md = report.ToMarkdown();

        Assert.Contains("# Bill of Materials (BOM) Report: BOM Markdown Test", md);
        Assert.Contains("## 1. Precious Metal & Casting Estimates", md);
        Assert.Contains("## 2. Gemstone Schedule", md);
        Assert.Contains("## 3. Wall Thickness Quality Diagnostics", md);
        Assert.Contains("Gold18KYellow", md);
        Assert.Contains("RoundBrilliant", md);
        Assert.Contains("PRODUCTION READY", md);
    }

    [Fact]
    public void JewelryBomReport_ToJson_SerializesAndDeserializesCorrectly()
    {
        var gems = new List<(GemDimensions gem, CaratEstimator.GemMaterial material)>
        {
            (GemDimensions.GetDefault(GemCutType.RoundBrilliant, 5.0), CaratEstimator.GemMaterial.Diamond)
        };

        var report = ManufacturingEngine.GenerateBomReport("JSON Test Project", 150.0, gems);
        string json = report.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"ProjectTitle\": \"JSON Test Project\"", json);
        Assert.Contains("\"Gold18KYellow\"", json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("JSON Test Project", root.GetProperty("ProjectTitle").GetString());
        Assert.True(root.GetProperty("IsProductionReady").GetBoolean());
    }

    #endregion

    #region Watertight Binary STL Exporter Tests

    [Fact]
    public void ExportWatertightStl_SolidCube_GeneratesExactBinaryStlLayout()
    {
        var cube = CreateSolidCube(2.0); // 12 triangles (6 quad faces triangulated)
        byte[] stlBytes = ManufacturingEngine.ExportWatertightStl(cube, "RadianceStudio Test Export");

        // 80 bytes header + 4 bytes triangle count + (12 * 50 bytes) = 80 + 4 + 600 = 684 bytes
        const int expectedTriangleCount = 12;
        int expectedLength = 80 + 4 + (expectedTriangleCount * 50);

        Assert.Equal(expectedLength, stlBytes.Length);

        // Verify header
        string headerStr = System.Text.Encoding.ASCII.GetString(stlBytes, 0, 80);
        Assert.StartsWith("RadianceStudio Test Export", headerStr);

        // Verify triangle count (little endian uint32 at offset 80)
        uint triCount = BitConverter.ToUInt32(stlBytes, 80);
        Assert.Equal((uint)expectedTriangleCount, triCount);

        // Verify first facet normal and vertices
        float nx = BitConverter.ToSingle(stlBytes, 84);
        float ny = BitConverter.ToSingle(stlBytes, 88);
        float nz = BitConverter.ToSingle(stlBytes, 92);
        Assert.False(float.IsNaN(nx) || float.IsNaN(ny) || float.IsNaN(nz));

        // Verify attribute byte count for first facet (ushort at offset 84 + 48 = 132)
        ushort attrBytes = BitConverter.ToUInt16(stlBytes, 132);
        Assert.Equal(0, attrBytes);
    }

    [Fact]
    public void ExportWatertightStl_OpenMesh_ThrowsInvalidOperationException_WhenValidationEnabled()
    {
        // An open mesh with 1 quad face (2 triangles) is not a closed watertight manifold
        var openMesh = new GemMesh
        {
            Vertices = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(1, 0, 0),
                new Point3D(1, 1, 0),
                new Point3D(0, 1, 0)
            },
            Faces = new[] { new[] { 0, 1, 2, 3 } }
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ManufacturingEngine.ExportWatertightStl(openMesh, validateManifold: true));

        Assert.Contains("manifold", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateMeshManifoldness_DetectsOpenEdges()
    {
        var openMesh = new GemMesh
        {
            Vertices = new[]
            {
                new Point3D(0, 0, 0),
                new Point3D(1, 0, 0),
                new Point3D(1, 1, 0),
                new Point3D(0, 1, 0)
            },
            Faces = new[] { new[] { 0, 1, 2, 3 } }
        };

        bool isValid = ManufacturingEngine.ValidateMeshManifoldness(openMesh, out var errors);
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("open boundary edge", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Geometry Generation Helpers for Testing

    private static GemMesh CreateSolidCube(double size)
    {
        double h = size * 0.5;
        var verts = new[]
        {
            new Point3D(-h, -h, -h), // 0
            new Point3D( h, -h, -h), // 1
            new Point3D( h,  h, -h), // 2
            new Point3D(-h,  h, -h), // 3
            new Point3D(-h, -h,  h), // 4
            new Point3D( h, -h,  h), // 5
            new Point3D( h,  h,  h), // 6
            new Point3D(-h,  h,  h)  // 7
        };

        // 6 quad faces with outward-facing counter-clockwise winding
        var faces = new[]
        {
            new[] { 0, 3, 2, 1 }, // Bottom (-Z)
            new[] { 4, 5, 6, 7 }, // Top (+Z)
            new[] { 0, 1, 5, 4 }, // Front (-Y)
            new[] { 2, 3, 7, 6 }, // Back (+Y)
            new[] { 0, 4, 7, 3 }, // Left (-X)
            new[] { 1, 2, 6, 5 }  // Right (+X)
        };

        return new GemMesh
        {
            Vertices = verts,
            Faces = faces
        };
    }

    private static GemMesh CreateHollowTube(double radiusOuter, double radiusInner, double height, int segments, int heightSlices = 4)
    {
        var verts = new List<Point3D>();
        var faces = new List<int[]>();

        // Outer cylinder vertices: heightSlices + 1 rings of 'segments' vertices each
        for (int s = 0; s <= heightSlices; s++)
        {
            double z = (height * s) / heightSlices;
            for (int i = 0; i < segments; i++)
            {
                double theta = (2.0 * Math.PI * i) / segments;
                verts.Add(new Point3D(radiusOuter * Math.Cos(theta), radiusOuter * Math.Sin(theta), z));
            }
        }

        // Inner cylinder vertices: heightSlices + 1 rings of 'segments' vertices each
        int innerStart = verts.Count;
        for (int s = 0; s <= heightSlices; s++)
        {
            double z = (height * s) / heightSlices;
            for (int i = 0; i < segments; i++)
            {
                double theta = (2.0 * Math.PI * i) / segments;
                verts.Add(new Point3D(radiusInner * Math.Cos(theta), radiusInner * Math.Sin(theta), z));
            }
        }

        // Outer cylindrical quads
        for (int s = 0; s < heightSlices; s++)
        {
            int ring0 = s * segments;
            int ring1 = (s + 1) * segments;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new[] { ring0 + i, ring0 + next, ring1 + next, ring1 + i });
            }
        }

        // Inner cylindrical quads (facing inward toward center axis)
        for (int s = 0; s < heightSlices; s++)
        {
            int ring0 = innerStart + (s * segments);
            int ring1 = innerStart + ((s + 1) * segments);
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new[] { ring0 + next, ring0 + i, ring1 + i, ring1 + next });
            }
        }

        // Bottom annular rim at s = 0 (z = 0, facing -Z)
        int outerBottom = 0;
        int innerBottom = innerStart;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            faces.Add(new[] { outerBottom + next, outerBottom + i, innerBottom + i, innerBottom + next });
        }

        // Top annular rim at s = heightSlices (z = height, facing +Z)
        int outerTop = heightSlices * segments;
        int innerTop = innerStart + (heightSlices * segments);
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            faces.Add(new[] { outerTop + i, outerTop + next, innerTop + next, innerTop + i });
        }

        return new GemMesh
        {
            Vertices = verts,
            Faces = faces
        };
    }

    #endregion
}
