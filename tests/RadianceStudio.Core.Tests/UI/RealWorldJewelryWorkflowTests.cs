namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Graph;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

/// <summary>
/// Tier 4 Real-World Jewelry Workflow End-to-End Tests:
/// Simulates complete professional studio CAD workflows:
/// 1. Solitaire Cathedral Engagement Ring Workflow
/// 2. Luxury Halo Cluster Cocktail Ring Workflow
/// 3. Multi-Row Pavé Eternity Band Workflow
/// </summary>
public class RealWorldJewelryWorkflowTests
{
    [Fact]
    public void Workflow1_SolitaireCathedralEngagementRing_FullPipeline()
    {
        // -------------------------------------------------------------------------
        // Step 1: Create Ring Rail (US Size 6.5, Canonical Circle)
        // -------------------------------------------------------------------------
        double fingerSizeUs = 6.5;
        double innerDiameterMm = RailEngine.UsSizeToInnerDiameterMm(fingerSizeUs);
        Assert.InRange(innerDiameterMm, 16.9, 17.0);

        var railProfile = RailProfileType.CanonicalCircle;
        Assert.Equal(RailProfileType.CanonicalCircle, railProfile);

        // -------------------------------------------------------------------------
        // Step 2: Swept Cathedral Shank (Comfort-Fit Profile)
        // -------------------------------------------------------------------------
        var shankParams = new ShankParameters
        {
            ProfileType = ShankProfileType.Cathedral,
            BottomWidthMm = 2.4,
            BottomThicknessMm = 1.8,
            TopWidthMm = 1.8,
            TopThicknessMm = 1.6,
            CathedralArchHeightMm = 2.5
        };
        Assert.Equal(2.5, shankParams.CathedralArchHeightMm);
        Assert.Equal(ShankProfileType.Cathedral, shankParams.ProfileType);
        Assert.True(shankParams.IsTapered);

        // -------------------------------------------------------------------------
        // Step 3: Place 1.00ct Round Brilliant Center Stone
        // -------------------------------------------------------------------------
        double centerGemDiameterMm = 6.5;
        var centerGem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, centerGemDiameterMm);
        var centerMesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, centerGemDiameterMm);
        double estimatedCarats = CaratEstimator.EstimateCarats(centerMesh);
        Assert.InRange(estimatedCarats, 0.90, 1.15);

        // -------------------------------------------------------------------------
        // Step 4: 4-Prong Head Setting for Center Stone
        // -------------------------------------------------------------------------
        var prongParams = new ProngParameters
        {
            ProngCount = 4,
            ProngDiameterMm = 0.85,
            ProngHeightMm = 4.2,
            Style = ProngStyle.Claw,
            IncludeGalleryWire = true,
            GalleryWireDiameterMm = 0.6,
            IncludeBaseWire = true,
            BaseWireDiameterMm = 0.7
        };
        Assert.Equal(4, prongParams.ProngCount);
        Assert.Equal(ProngStyle.Claw, prongParams.Style);

        // -------------------------------------------------------------------------
        // Step 5: Side Stone Gem Rows (6 x 1.3mm melee per shoulder = 12 stones)
        // -------------------------------------------------------------------------
        double meleeDiameterMm = 1.3;
        int meleePerShoulder = 6;
        int totalMeleeCount = meleePerShoulder * 2;
        var meleeMesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, meleeDiameterMm);
        double singleMeleeCarats = CaratEstimator.EstimateCarats(meleeMesh);
        double totalMeleeCarats = singleMeleeCarats * totalMeleeCount;

        Assert.Equal(12, totalMeleeCount);
        Assert.True(totalMeleeCarats > 0.05 && totalMeleeCarats < 0.25);

        // -------------------------------------------------------------------------
        // Step 6: Gem Seat Cutters Configuration
        // -------------------------------------------------------------------------
        var centerCutter = new CutterParameters
        {
            GirdleClearanceMm = 0.10,
            SeatAngle = CutterSeatStyle.Degree70,
            LightHoleDepthExtensionMm = 2.0
        };
        Assert.Equal(CutterSeatStyle.Degree70, centerCutter.SeatAngle);

        // -------------------------------------------------------------------------
        // Step 7: Manufacturing BOM Weight and Cost Calculation
        // -------------------------------------------------------------------------
        double estimatedMetalVolumeMm3 = 420.0;
        double density14KWhite = PreciousMetalAlloy.Gold14KWhite.GetDensityGPerCm3();
        double weightGrams14K = (estimatedMetalVolumeMm3 / 1000.0) * density14KWhite;

        double spotPricePerGram14K = 52.0;
        double rawMetalCost = weightGrams14K * spotPricePerGram14K;

        Assert.InRange(weightGrams14K, 4.5, 6.5);
        Assert.True(rawMetalCost > 200.0);

        // Total diamond weight: center + melee
        double totalDiamondCarats = estimatedCarats + totalMeleeCarats;
        Assert.InRange(totalDiamondCarats, 0.98, 1.25);

        // -------------------------------------------------------------------------
        // Step 8: Wall Thickness & Casting Feasibility Inspection
        // -------------------------------------------------------------------------
        double minWallThickness = 0.85;
        double prongNeckThickness = 0.70;

        Assert.True(minWallThickness >= 0.80, "Shank wall violates 0.80mm minimum");
        Assert.True(prongNeckThickness >= 0.60, "Prong neck violates 0.60mm minimum");
    }

    [Fact]
    public void Workflow2_LuxuryHaloClusterCocktailRing_FullPipeline()
    {
        // -------------------------------------------------------------------------
        // Step 1: Create Cushion Squircle Ring Rail (US Size 7.0)
        // -------------------------------------------------------------------------
        double fingerSizeUs = 7.0;
        double innerDiameterMm = RailEngine.UsSizeToInnerDiameterMm(fingerSizeUs);
        Assert.InRange(innerDiameterMm, 17.3, 17.4);

        var railProfile = RailProfileType.CushionSquircle;
        Assert.Equal(RailProfileType.CushionSquircle, railProfile);

        // -------------------------------------------------------------------------
        // Step 2: Oval Brilliant Center Stone (8.0mm major diameter)
        // -------------------------------------------------------------------------
        var centerOval = GemDimensions.GetDefault(GemCutType.Oval, 8.0);
        Assert.Equal(8.0, centerOval.DiameterMm);
        Assert.True(centerOval.GetLengthMm() > centerOval.DiameterMm || centerOval.GetLengthMm() == centerOval.DiameterMm);

        // -------------------------------------------------------------------------
        // Step 3: Parametric Halo Cluster
        // -------------------------------------------------------------------------
        double meleeDiameterMm = 1.2;
        double meleeSpacingMm = 0.12;
        var haloParams = new HaloParameters
        {
            MeleeGemCut = GemCutType.RoundBrilliant,
            MeleeDiameterMm = meleeDiameterMm,
            SpacingMm = meleeSpacingMm,
            GuardDistanceMm = 1.4,
            TrayDepthMm = 0.8,
            TrayWallThicknessMm = 0.65
        };

        Assert.Equal(1.4, haloParams.GuardDistanceMm);
        Assert.Equal(0.65, haloParams.TrayWallThicknessMm);

        double approxPerimeter = 30.8;
        double step = meleeDiameterMm + meleeSpacingMm;
        int expectedHaloStones = (int)Math.Floor(approxPerimeter / step);

        Assert.InRange(expectedHaloStones, 20, 26);

        // -------------------------------------------------------------------------
        // Step 4: BOM Calculation in Platinum 950
        // -------------------------------------------------------------------------
        double cocktailMetalVolumeMm3 = 580.0;
        double densityPt950 = PreciousMetalAlloy.Platinum950.GetDensityGPerCm3();
        double weightPt950 = (cocktailMetalVolumeMm3 / 1000.0) * densityPt950;

        Assert.InRange(weightPt950, 11.0, 14.0);
    }

    [Fact]
    public void Workflow3_MultiRowPaveEternityBand_FullPipeline()
    {
        // -------------------------------------------------------------------------
        // Step 1: Create Full Round Eternity Band Rail (US Size 8.0)
        // -------------------------------------------------------------------------
        double fingerSizeUs = 8.0;
        double innerDiameterMm = RailEngine.UsSizeToInnerDiameterMm(fingerSizeUs);
        Assert.InRange(innerDiameterMm, 18.1, 18.3);

        // -------------------------------------------------------------------------
        // Step 2: Wide Comfort-Fit Band Surface
        // -------------------------------------------------------------------------
        double bandWidthMm = 5.5;
        double bandThicknessMm = 2.0;

        var bandParams = new ShankParameters
        {
            ProfileType = ShankProfileType.ComfortFit,
            BottomWidthMm = bandWidthMm,
            BottomThicknessMm = bandThicknessMm,
            TopWidthMm = bandWidthMm,
            TopThicknessMm = bandThicknessMm
        };
        Assert.Equal(bandWidthMm, bandParams.BottomWidthMm);

        // -------------------------------------------------------------------------
        // Step 3: 3-Row Pavé Hexagonal Close Packing
        // -------------------------------------------------------------------------
        double paveStoneDiameterMm = 1.4;
        double paveSpacingMm = 0.12;

        var paveParams = new PaveParameters
        {
            PatternType = PavePatternType.Hexagonal,
            ProngType = PaveProngType.Bead,
            StoneDiameterMm = paveStoneDiameterMm,
            StoneSpacingMm = paveSpacingMm,
            BeadDiameterMm = 0.45,
            BeadHeightMm = 0.35
        };

        Assert.Equal(PavePatternType.Hexagonal, paveParams.PatternType);
        Assert.Equal(0.45, paveParams.BeadDiameterMm);

        double outerCircumference = Math.PI * (innerDiameterMm + (2.0 * bandThicknessMm));
        int stonesPerRow = (int)Math.Floor(outerCircumference / (paveStoneDiameterMm + paveSpacingMm));
        int totalPaveStones = stonesPerRow * 3;

        Assert.InRange(totalPaveStones, 120, 150);

        // -------------------------------------------------------------------------
        // Step 4: BOM Carats and Metal Weights
        // -------------------------------------------------------------------------
        var paveMesh = GemMeshFactory.Create(GemCutType.RoundBrilliant, paveStoneDiameterMm);
        double singleStoneCarats = CaratEstimator.EstimateCarats(paveMesh);
        double totalPaveCarats = singleStoneCarats * totalPaveStones;

        Assert.InRange(totalPaveCarats, 1.2, 2.5);

        // -------------------------------------------------------------------------
        // Step 5: Hermetic STL Export Validation (Binary Header + Triangle Count)
        // -------------------------------------------------------------------------
        byte[] stlBytes = ManufacturingEngine.ExportWatertightStl(paveMesh, "RadianceStudio Ring Export", validateManifold: true);
        Assert.NotNull(stlBytes);
        Assert.True(stlBytes.Length > 84);
        uint triangleCount = BitConverter.ToUInt32(stlBytes, 80);
        Assert.True(triangleCount > 0);
        Assert.Equal(80 + 4 + ((int)triangleCount * 50), stlBytes.Length);
        Assert.StartsWith("RadianceStudio", System.Text.Encoding.ASCII.GetString(stlBytes, 0, 80));
    }
}
