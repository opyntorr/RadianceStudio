namespace RadianceStudio.Core.Tests.Shanks;

using System;
using System.Diagnostics;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using global::Rhino.Geometry;
using Xunit;

/// <summary>
/// Comprehensive unit and integration tests for the Parametric Shanks Engine (Milestone 2).
/// Validates cross-section profiles (Flat, DShape, ComfortFit, KnifeEdge, Cathedral),
/// Hermite smoothstep tapering s(t) = 3t^2 - 2t^3 with zero boundary derivatives,
/// under-gallery azurador casting structural invariants (wall thickness &gt;= 0.8 mm, floor clearance &gt;= 0.8 mm),
/// watertight swept solid meshes for all rail profiles, and dual execution in Rhino and headless CLI.
/// </summary>
public class ShankEngineTests
{
    private const double Tolerance = 1e-4;

    #region 1. ShankProfileType Enum & Aliases

    [Fact]
    public void ShankProfileType_DefinesAllFiveCanonicalProfiles()
    {
        Assert.True(Enum.IsDefined(typeof(ShankProfileType), ShankProfileType.Flat));
        Assert.True(Enum.IsDefined(typeof(ShankProfileType), ShankProfileType.DShape));
        Assert.True(Enum.IsDefined(typeof(ShankProfileType), ShankProfileType.ComfortFit));
        Assert.True(Enum.IsDefined(typeof(ShankProfileType), ShankProfileType.KnifeEdge));
        Assert.True(Enum.IsDefined(typeof(ShankProfileType), ShankProfileType.Cathedral));
    }

    [Fact]
    public void ShankProfileType_AliasesMatchCanonicalValues()
    {
        Assert.Equal(ShankProfileType.DShape, ShankProfileType.HalfRound);
        Assert.Equal(ShankProfileType.ComfortFit, ShankProfileType.Comfort);
        Assert.Equal(ShankProfileType.KnifeEdge, ShankProfileType.Knife);
    }

    #endregion

    #region 2. ShankParameters Model & Validation

    [Fact]
    public void ShankParameters_DefaultConstructor_ProvidesValidJewelryDefaults()
    {
        var p = new ShankParameters();

        Assert.Equal(ShankProfileType.ComfortFit, p.ProfileType);
        Assert.Equal(2.0, p.BottomWidthMm);
        Assert.Equal(1.6, p.BottomThicknessMm);
        Assert.Equal(2.6, p.TopWidthMm);
        Assert.Equal(2.0, p.TopThicknessMm);
        Assert.Equal(0.25, p.CrownRatio);
        Assert.Equal(0.10, p.KnifeEdgeApexRadiusMm);
        Assert.Equal(2.0, p.CathedralArchHeightMm);
        Assert.Null(p.UnderGallery);
        Assert.True(p.IsTapered);
    }

    [Fact]
    public void ShankParameters_UniformConstructor_CreatesNonTaperedShank()
    {
        var p = new ShankParameters(ShankProfileType.Flat, 3.0, 1.8);

        Assert.Equal(ShankProfileType.Flat, p.ProfileType);
        Assert.Equal(3.0, p.BottomWidthMm);
        Assert.Equal(1.8, p.BottomThicknessMm);
        Assert.Equal(3.0, p.TopWidthMm);
        Assert.Equal(1.8, p.TopThicknessMm);
        Assert.False(p.IsTapered);
    }

    [Theory]
    [InlineData(0.0, 1.5, 2.5, 1.8)]
    [InlineData(-1.0, 1.5, 2.5, 1.8)]
    [InlineData(2.0, 1.5, 0.0, 1.8)]
    [InlineData(2.0, 1.5, -0.5, 1.8)]
    public void ShankParameters_NonPositiveWidth_ThrowsArgumentOutOfRangeException(
        double bW, double bT, double tW, double tT)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(ShankProfileType.ComfortFit, bW, bT, tW, tT));
    }

    [Theory]
    [InlineData(2.0, 0.79, 2.5, 1.8)]
    [InlineData(2.0, 0.0, 2.5, 1.8)]
    [InlineData(2.0, -0.5, 2.5, 1.8)]
    [InlineData(2.0, 1.5, 2.5, 0.79)]
    [InlineData(2.0, 1.5, 2.5, 0.0)]
    public void ShankParameters_ThicknessBelowMinimum_ThrowsArgumentOutOfRangeException(
        double bW, double bT, double tW, double tT)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(ShankProfileType.ComfortFit, bW, bT, tW, tT));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    public void ShankParameters_InvalidCrownRatio_ThrowsArgumentOutOfRangeException(double crownRatio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(
                ShankProfileType.ComfortFit,
                2.0, 1.5, 2.5, 1.8,
                crownRatio: crownRatio));
    }

    [Fact]
    public void ShankParameters_NegativeKnifeEdgeApexOrCathedralHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(
                ShankProfileType.KnifeEdge,
                2.0, 1.5, 2.5, 1.8,
                knifeEdgeApexRadiusMm: -0.05));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(
                ShankProfileType.Cathedral,
                2.0, 1.5, 2.5, 1.8,
                cathedralArchHeightMm: -1.0));
    }

    #endregion

    #region 3. UnderGalleryParameters & Structural Invariant Enforcement

    [Fact]
    public void UnderGalleryParameters_DefaultConstructor_InitializesCastingSafeDefaults()
    {
        var g = new UnderGalleryParameters();

        Assert.Equal(0.85, g.WallThicknessMm);
        Assert.Equal(0.85, g.FloorClearanceMm);
        Assert.Equal(UnderGalleryPattern.Honeycomb, g.Pattern);
        Assert.Equal(45.0, g.SpanDegrees);
        Assert.Equal(1.0, g.HoleDiameterMm);
        Assert.Equal(0.4, g.SpacingMm);
    }

    [Theory]
    [InlineData(0.80)]
    [InlineData(0.85)]
    [InlineData(1.00)]
    [InlineData(1.50)]
    public void UnderGalleryParameters_WallThicknessAtOrAboveMinimum_Succeeds(double wallMm)
    {
        var g = new UnderGalleryParameters(wallThicknessMm: wallMm);
        Assert.Equal(wallMm, g.WallThicknessMm);
    }

    [Theory]
    [InlineData(0.79)]
    [InlineData(0.75)]
    [InlineData(0.50)]
    [InlineData(0.0)]
    [InlineData(-0.8)]
    public void UnderGalleryParameters_WallThicknessBelowMinimum_ThrowsArgumentOutOfRangeException(double wallMm)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(wallThicknessMm: wallMm));

        Assert.Contains("0.80 mm", ex.Message);
    }

    [Theory]
    [InlineData(0.80)]
    [InlineData(0.85)]
    [InlineData(1.20)]
    public void UnderGalleryParameters_FloorClearanceAtOrAboveMinimum_Succeeds(double clearanceMm)
    {
        var g = new UnderGalleryParameters(floorClearanceMm: clearanceMm);
        Assert.Equal(clearanceMm, g.FloorClearanceMm);
    }

    [Theory]
    [InlineData(0.79)]
    [InlineData(0.50)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void UnderGalleryParameters_FloorClearanceBelowMinimum_ThrowsArgumentOutOfRangeException(double clearanceMm)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(floorClearanceMm: clearanceMm));

        Assert.Contains("0.80 mm", ex.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-15.0)]
    [InlineData(181.0)]
    public void UnderGalleryParameters_InvalidSpanDegrees_ThrowsArgumentOutOfRangeException(double spanDeg)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(spanDegrees: spanDeg));
    }

    [Fact]
    public void UnderGalleryParameters_InvalidHoleDiameterOrSpacing_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(holeDiameterMm: 0.0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(holeDiameterMm: -0.5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(spacingMm: -0.1));
    }

    [Fact]
    public void UnderGalleryParameters_SupportsAllPatternStyles()
    {
        foreach (UnderGalleryPattern pattern in Enum.GetValues(typeof(UnderGalleryPattern)))
        {
            var g = new UnderGalleryParameters(pattern: pattern);
            Assert.Equal(pattern, g.Pattern);
        }
    }

    #endregion

    #region 4. Hermite Smoothstep Tapering Formulation

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 1.0)]
    public void Smoothstep_KeyCheckpoints_MatchExpectedValues(double t, double expected)
    {
        double s = ShankEngine.Smoothstep(t);
        Assert.Equal(expected, s, 6);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(-10.0, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(5.0, 1.0)]
    public void Smoothstep_ClampsOutsideZeroOneInterval(double t, double expected)
    {
        double s = ShankEngine.Smoothstep(t);
        Assert.Equal(expected, s, 6);
    }

    [Fact]
    public void Smoothstep_BoundaryDerivatives_StrictlyZero()
    {
        // Enforce s'(0) = 0 and s'(1) = 0 to eliminate curvature jerks
        Assert.Equal(0.0, ShankEngine.SmoothstepDerivative(0.0), 6);
        Assert.Equal(0.0, ShankEngine.SmoothstepDerivative(1.0), 6);

        // Midpoint derivative s'(0.5) = 6(0.5)(0.5) = 1.5
        Assert.Equal(1.5, ShankEngine.SmoothstepDerivative(0.5), 6);
    }

    [Fact]
    public void Smoothstep_IsStrictlyMonotonicOnInterval()
    {
        double prev = ShankEngine.Smoothstep(0.0);
        for (int i = 1; i <= 100; i++)
        {
            double t = i / 100.0;
            double curr = ShankEngine.Smoothstep(t);
            Assert.True(curr >= prev, $"Smoothstep must be monotonically increasing at t={t}");
            prev = curr;
        }
    }

    [Theory]
    [InlineData(-Math.PI / 2.0, 0.0)] // Bottom / palm side (theta = -pi/2) -> t = 0
    [InlineData(1.5 * Math.PI, 0.0)]  // Bottom / palm side (theta = 3pi/2) -> t = 0
    [InlineData(Math.PI / 2.0, 1.0)]  // Top / head side (theta = pi/2) -> t = 1
    [InlineData(0.0, 0.5)]            // Right flank (theta = 0) -> t = 0.5
    [InlineData(Math.PI, 0.5)]        // Left flank (theta = pi) -> t = 0.5
    public void AngleToNormalizedParameter_MapsAnglesSymmetrically(double angle, double expectedT)
    {
        double t = ShankEngine.AngleToNormalizedParameter(angle);
        Assert.True(Math.Abs(t - expectedT) <= 1e-6, $"Angle {angle:F3} should produce t={expectedT}, got {t}");
    }

    [Fact]
    public void CalculateTaperDimensions_InterpolatesCorrectly()
    {
        double bW = 2.0, bT = 1.6, tW = 3.0, tT = 2.4;

        // At t = 0 (bottom)
        var (w0, t0) = ShankEngine.CalculateTaperDimensions(bW, bT, tW, tT, 0.0);
        Assert.Equal(bW, w0, 4);
        Assert.Equal(bT, t0, 4);

        // At t = 1 (top)
        var (w1, t1) = ShankEngine.CalculateTaperDimensions(bW, bT, tW, tT, 1.0);
        Assert.Equal(tW, w1, 4);
        Assert.Equal(tT, t1, 4);

        // At t = 0.5 (midpoint: s(0.5) = 0.5)
        var (wMid, tMid) = ShankEngine.CalculateTaperDimensions(bW, bT, tW, tT, 0.5);
        Assert.Equal(2.5, wMid, 4);
        Assert.Equal(2.0, tMid, 4);
    }

    [Fact]
    public void CalculateTaperDimensions_UniformInputs_OutputsUniformDimensions()
    {
        // Edge Case #10: Tapering with W_top == W_bottom produces uniform non-tapered band
        double width = 2.5;
        double thickness = 1.8;

        for (double t = 0.0; t <= 1.0; t += 0.1)
        {
            var (w, th) = ShankEngine.CalculateTaperDimensions(width, thickness, width, thickness, t);
            Assert.Equal(width, w, 4);
            Assert.Equal(thickness, th, 4);
        }
    }

    #endregion

    #region 5. 2D Cross-Section Profiles Geometry & Area

    [Fact]
    public void CreateCrossSection2D_Flat_ProducesExactRectangle()
    {
        double w = 3.0, t = 1.8;
        var section = ShankEngine.CreateCrossSection2D(ShankProfileType.Flat, w, t);

        Assert.Equal(ShankProfileType.Flat, section.ProfileType);
        Assert.Equal(4, section.Points.Count);

        // Area must equal W * T exactly
        double expectedArea = w * t;
        Assert.True(Math.Abs(section.Area - expectedArea) <= Tolerance);

        // Bounds must match [-W/2, W/2] and [0, T]
        Assert.Equal(-w / 2.0, section.Bounds.MinU, 4);
        Assert.Equal(w / 2.0, section.Bounds.MaxU, 4);
        Assert.Equal(0.0, section.Bounds.MinV, 4);
        Assert.Equal(t, section.Bounds.MaxV, 4);
    }

    [Fact]
    public void CreateCrossSection2D_DShape_ProducesFlatBaseAndEllipticalCrown()
    {
        double w = 2.8, t = 2.0;
        var section = ShankEngine.CreateCrossSection2D(ShankProfileType.DShape, w, t, sampleCount: 64);

        Assert.Equal(ShankProfileType.DShape, section.ProfileType);
        Assert.True(section.Points.Count >= 16);

        // Theoretical semi-ellipse area = (pi / 4) * W * T
        double theoreticalArea = (Math.PI / 4.0) * w * t;
        Assert.True(Math.Abs(section.Area - theoreticalArea) / theoreticalArea < 0.015,
            $"DShape area {section.Area:F3} should match theoretical semi-ellipse {theoreticalArea:F3}");

        // Bounds check: MinV ~ 0 (flat inner face), MaxV ~ T (apex)
        Assert.True(Math.Abs(section.Bounds.MinV) <= 1e-4);
        Assert.True(Math.Abs(section.Bounds.MaxV - t) <= 0.05);
        Assert.True(Math.Abs(section.Bounds.MinU - (-w / 2.0)) <= 1e-4);
        Assert.True(Math.Abs(section.Bounds.MaxU - (w / 2.0)) <= 1e-4);
    }

    [Fact]
    public void CreateCrossSection2D_ComfortFit_ProducesBulgingInnerCrown()
    {
        double w = 3.0, t = 2.0, crownRatio = 0.25;
        var section = ShankEngine.CreateCrossSection2D(
            ShankProfileType.ComfortFit,
            w,
            t,
            crownRatio: crownRatio,
            sampleCount: 64);

        Assert.Equal(ShankProfileType.ComfortFit, section.ProfileType);

        // Inner face must bulge inward (MinV < 0) by crownRatio * T = 0.50 mm
        double expectedInnerCrown = crownRatio * t;
        Assert.True(section.Bounds.MinV < -0.45,
            $"ComfortFit inner curve must bulge inward: MinV={section.Bounds.MinV:F3}");
        Assert.True(Math.Abs(section.Bounds.MinV - (-expectedInnerCrown)) <= 0.05);

        // Total vertical thickness from inner crown to outer crown ~ T * (1 + crownRatio)
        double totalSpan = section.Bounds.MaxV - section.Bounds.MinV;
        Assert.True(Math.Abs(totalSpan - (t + expectedInnerCrown)) <= 0.05);
    }

    [Fact]
    public void CreateCrossSection2D_KnifeEdge_ProducesTriangularPeakedRidge()
    {
        double w = 2.6, t = 2.2, apexR = 0.15;
        var section = ShankEngine.CreateCrossSection2D(
            ShankProfileType.KnifeEdge,
            w,
            t,
            knifeEdgeApexRadiusMm: apexR);

        Assert.Equal(ShankProfileType.KnifeEdge, section.ProfileType);

        // Peak reaches ~ T
        Assert.True(Math.Abs(section.Bounds.MaxV - t) <= 0.02);

        // Area of knife edge triangle is roughly (1/2) * W * T
        double triangleArea = 0.5 * w * t;
        Assert.True(section.Area < (w * t) * 0.75, "KnifeEdge area must be significantly smaller than full rectangle");
        Assert.True(section.Area > triangleArea * 0.90, "KnifeEdge area must be near triangle area");
    }

    [Fact]
    public void CreateCrossSection2D_Cathedral_ShoulderArchElevatesTop()
    {
        double w = 2.5, t = 1.8, archH = 1.5;
        var flatSection = ShankEngine.CreateCrossSection2D(ShankProfileType.Cathedral, w, t, cathedralArchHeightMm: 0.0);
        var archSection = ShankEngine.CreateCrossSection2D(ShankProfileType.Cathedral, w, t, cathedralArchHeightMm: archH);

        // Without arch, matches flat thickness
        Assert.Equal(t, flatSection.Bounds.MaxV, 4);

        // With arch, top elevates by archH
        Assert.True(Math.Abs(archSection.Bounds.MaxV - (t + archH)) <= 0.05);
        Assert.True(archSection.Area > flatSection.Area);
    }

    [Fact]
    public void CreateCrossSectionProfilePoints_TransformsCorrectlyTo3D()
    {
        var origin = new Point3D(10, 20, 30);
        var tangent = new Vector3D(0, 1, 0);
        var normal = new Vector3D(1, 0, 0);
        var binormal = new Vector3D(0, 0, 1);
        var frame = new OrthonormalFrame(tangent, normal, binormal);

        var pts = ShankEngine.CreateCrossSectionProfilePoints(
            ShankProfileType.Flat,
            2.0,
            1.5,
            origin,
            frame);

        Assert.Equal(4, pts.Count);
        // Point 0 was (-W/2, 0) in (u,v), should map to origin - binormal*1.0 = (10, 20, 29)
        Assert.Equal(10.0, pts[0].X, 4);
        Assert.Equal(20.0, pts[0].Y, 4);
        Assert.Equal(29.0, pts[0].Z, 4);
    }

    #endregion

    #region 6. Headless Swept Solid Mesh Generation & Manifold Validity

    [Fact]
    public void CreateSweptShankMesh_CanonicalCircleRail_ProducesWatertightClosedManifold()
    {
        double innerDiameter = 17.32; // US Size 7
        var rail = new CircleCurve3D(Point3D.Origin, innerDiameter / 2.0);

        var parameters = new ShankParameters(
            ShankProfileType.ComfortFit,
            bottomWidthMm: 2.0,
            bottomThicknessMm: 1.6,
            topWidthMm: 2.8,
            topThicknessMm: 2.0);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        Assert.NotNull(mesh);
        Assert.True(mesh.Vertices.Count > 0);
        Assert.True(mesh.Faces.Count > 0);

        // Watertight closed 2-manifold check: every directed edge shared by exactly 2 faces
        Assert.True(mesh.IsClosedManifold, "Swept shank mesh must be a watertight closed manifold.");

        // Positive volume check via divergence theorem
        Assert.True(mesh.EnclosedVolume > 50.0, $"Enclosed volume {mesh.EnclosedVolume:F1} mm^3 must be substantial.");
        Assert.True(mesh.EnclosedVolume < 1000.0, $"Enclosed volume {mesh.EnclosedVolume:F1} mm^3 must be physically realistic.");

        // Surface area check
        Assert.True(mesh.SurfaceArea > 100.0, $"Surface area {mesh.SurfaceArea:F1} mm^2 must be positive.");

        // Structural wall thickness invariant
        Assert.True(mesh.MinWallThicknessMm >= 0.80, $"Minimum wall thickness {mesh.MinWallThicknessMm:F2} mm must be >= 0.8 mm.");
    }

    [Theory]
    [InlineData(ShankProfileType.Flat)]
    [InlineData(ShankProfileType.DShape)]
    [InlineData(ShankProfileType.ComfortFit)]
    [InlineData(ShankProfileType.KnifeEdge)]
    [InlineData(ShankProfileType.Cathedral)]
    public void CreateSweptShankMesh_AllFiveProfiles_ProduceValidClosedManifolds(ShankProfileType profile)
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.5); // ~17 mm diameter
        var parameters = new ShankParameters(profile, 2.2, 1.6, 2.8, 2.0);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 32);

        Assert.True(mesh.IsClosedManifold, $"Profile {profile} must produce a closed manifold mesh.");
        Assert.True(mesh.EnclosedVolume > 0.0, $"Profile {profile} must have positive volume.");
    }

    [Fact]
    public void CreateSweptShankMesh_EuroShankRail_ProducesClosedManifold()
    {
        var rail = RailEngine.CreateRingRailCurve3D(17.32, RailProfileType.EuroShank);
        var parameters = new ShankParameters(ShankProfileType.ComfortFit, 2.0, 1.6, 2.6, 2.0);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        Assert.True(mesh.IsClosedManifold, "EuroShank swept mesh must be a closed manifold.");
        Assert.True(mesh.EnclosedVolume > 100.0);
    }

    [Fact]
    public void CreateSweptShankMesh_CushionSquircleRail_ProducesClosedManifold()
    {
        var rail = RailEngine.CreateRingRailCurve3D(17.32, RailProfileType.CushionSquircle);
        var parameters = new ShankParameters(ShankProfileType.DShape, 2.2, 1.6, 3.0, 2.2);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        Assert.True(mesh.IsClosedManifold, "Cushion squircle swept mesh must be a closed manifold.");
        Assert.True(mesh.EnclosedVolume > 100.0);
    }

    [Fact]
    public void CreateSweptShankMesh_BypassOverlapRail_ProducesClosedManifoldWithCappedEnds()
    {
        var rail = RailEngine.CreateRingRailCurve3D(17.32, RailProfileType.BypassOverlap);
        Assert.False(rail.IsClosed, "Bypass overlap rail is open.");

        var parameters = new ShankParameters(ShankProfileType.ComfortFit, 2.0, 1.6, 2.4, 1.8);
        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        // Open curve swept mesh must be closed with planar end caps
        Assert.True(mesh.IsClosedManifold, "Bypass overlap mesh must be capped into a closed manifold.");
        Assert.True(mesh.EnclosedVolume > 100.0);
    }

    #endregion

    #region 7. Under-Gallery Azurador Hollowing & Invariant In-Mesh

    [Fact]
    public void CreateSweptShankMesh_WithUnderGallery_ReducesVolumeAndPreservesMinWall()
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);

        // Solid shank without under-gallery
        var solidParams = new ShankParameters(
            ShankProfileType.ComfortFit,
            bottomWidthMm: 2.2,
            bottomThicknessMm: 1.6,
            topWidthMm: 3.5,
            topThicknessMm: 2.4);

        var solidMesh = ShankEngine.CreateSweptShankMesh(rail, solidParams, sliceCount: 48);

        // Hollowed shank with under-gallery
        var gallery = new UnderGalleryParameters(
            wallThicknessMm: 0.85,
            floorClearanceMm: 0.85,
            spanDegrees: 45.0);

        var hollowParams = solidParams with { UnderGallery = gallery };
        var hollowMesh = ShankEngine.CreateSweptShankMesh(rail, hollowParams, sliceCount: 48);

        // Hollowed shank must be a valid closed manifold
        Assert.True(hollowMesh.IsClosedManifold);

        // Under-gallery hollowing must strictly reduce solid metal volume
        Assert.True(hollowMesh.EnclosedVolume < solidMesh.EnclosedVolume,
            $"Hollow volume ({hollowMesh.EnclosedVolume:F2}) must be strictly less than solid volume ({solidMesh.EnclosedVolume:F2}).");

        // Verified minimum wall thickness in the mesh must respect casting threshold >= 0.8 mm
        Assert.True(hollowMesh.MinWallThicknessMm >= 0.80,
            $"Minimum wall thickness ({hollowMesh.MinWallThicknessMm:F2} mm) must be >= 0.8 mm.");
    }

    [Fact]
    public void CreateSweptShankMesh_WhenDimensionsTooThinForUnderGallery_ThrowsArgumentException()
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);

        // Top thickness is 1.2 mm, but gallery requires wall 0.85 + clearance 0.85 = 1.70 mm
        var gallery = new UnderGalleryParameters(wallThicknessMm: 0.85, floorClearanceMm: 0.85);
        var tooThinParams = new ShankParameters(
            ShankProfileType.ComfortFit,
            bottomWidthMm: 2.0,
            bottomThicknessMm: 1.0,
            topWidthMm: 2.2,
            topThicknessMm: 1.2,
            underGallery: gallery);

        var ex = Assert.Throws<ArgumentException>(() =>
            ShankEngine.CreateSweptShankMesh(rail, tooThinParams));

        Assert.Contains("insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 8. Dual Execution & Rhino Integration

    [Fact]
    public void IsRhinoNativeAvailable_PropertyExecutesWithoutException()
    {
        // Must evaluate safely in any environment (returns false in CLI without Rhino)
        bool isAvailable = ShankEngine.IsRhinoNativeAvailable;
        Assert.True(isAvailable || !isAvailable); // Valid boolean
    }

    [Fact]
    public void CreateCrossSectionProfile_InvalidParameters_ThrowsEvenInHeadless()
    {
        var plane = Plane.WorldXY;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShankEngine.CreateCrossSectionProfile(ShankProfileType.Flat, 0.0, 1.8, plane));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShankEngine.CreateCrossSectionProfile(ShankProfileType.Flat, 2.5, 0.5, plane));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShankEngine.CreateCrossSectionProfile((ShankProfileType)999, 2.5, 1.8, plane));
    }

    [Fact]
    public void CreateSweptShank_InvalidParameters_ThrowsEvenInHeadless()
    {
        // When rail is null or dimensions invalid
        Assert.Throws<ArgumentNullException>(() =>
            ShankEngine.CreateSweptShank(null!, new ShankParameters()));
    }

    [Fact]
    public void CreateCrossSectionProfile_WhenRhinoNativeAvailable_ReturnsValidCurveWithMetadata()
    {
        if (ShankEngine.IsRhinoNativeAvailable)
        {
            Curve? profile = ShankEngine.CreateCrossSectionProfile(
                ShankProfileType.ComfortFit,
                2.5,
                1.8,
                Plane.WorldXY);

            Assert.NotNull(profile);
            Assert.True(profile.IsValid);
            Assert.True(profile.IsClosed);

            var metadata = ParametricMetadata.Find(profile);
            Assert.NotNull(metadata);
            Assert.Equal(ShankProfileType.ComfortFit.ToString(), (string)metadata.Parameters["ProfileType"]!);
            Assert.Equal(2.5, (double)metadata.Parameters["WidthMm"]!);
            Assert.Equal(1.8, (double)metadata.Parameters["ThicknessMm"]!);
        }
    }

    [Fact]
    public void CreateSweptShank_WhenRhinoNativeAvailable_ReturnsValidSolidBrep()
    {
        if (ShankEngine.IsRhinoNativeAvailable)
        {
            Curve? rail = RailEngine.CreateRingRail(17.32, RailProfileType.CanonicalCircle);
            Assert.NotNull(rail);

            var parameters = new ShankParameters(
                ShankProfileType.ComfortFit,
                2.0, 1.6, 2.8, 2.0);

            Brep? shank = ShankEngine.CreateSweptShank(rail, parameters);
            Assert.NotNull(shank);
            Assert.True(shank.IsValid);
            Assert.True(shank.IsSolid, "Swept shank must produce a solid manifold Brep.");

            var metadata = ParametricMetadata.Find(shank);
            Assert.NotNull(metadata);
            Assert.True((bool)metadata.Parameters["IsSolid"]!);
        }
    }

    #endregion

    #region 9. Adversarial & Performance Benchmark

    [Theory]
    [InlineData(11.63, 1.0, 0.8, 1.5, 1.0)]   // US Size 0 boundary (delicate band)
    [InlineData(24.635, 8.0, 2.5, 12.0, 3.5)] // US Size 16 boundary (wide cigar band)
    public void CreateSweptShankMesh_ExtremeSizesAndWidths_GenerateValidManifolds(
        double innerDiameterMm,
        double bW, double bT, double tW, double tT)
    {
        var rail = new CircleCurve3D(Point3D.Origin, innerDiameterMm / 2.0);
        var parameters = new ShankParameters(
            ShankProfileType.ComfortFit,
            bW, bT, tW, tT);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 32);

        Assert.True(mesh.IsClosedManifold);
        Assert.True(mesh.EnclosedVolume > 0.0);
        Assert.True(mesh.MinWallThicknessMm >= 0.80);
    }

    [Fact]
    public void CreateSweptShankMesh_PerformanceBenchmark_ExecutesWithinViewportBudget()
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var parameters = new ShankParameters(
            ShankProfileType.ComfortFit,
            2.0, 1.6, 2.8, 2.0,
            underGallery: new UnderGalleryParameters(0.85, 0.85));

        // Warm up JIT
        _ = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 16);

        // Benchmark 64 slices
        var sw = Stopwatch.StartNew();
        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 64);
        sw.Stop();

        Assert.True(mesh.IsClosedManifold);
        Assert.True(sw.ElapsedMilliseconds < 50,
            $"Swept shank generation took {sw.ElapsedMilliseconds} ms, which exceeds the 50 ms viewport threshold.");
    }

    #endregion
}
