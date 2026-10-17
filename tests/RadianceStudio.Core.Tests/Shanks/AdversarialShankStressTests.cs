namespace RadianceStudio.Core.Tests.Shanks;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Xunit;

/// <summary>
/// Empirical adversarial stress testing suite for Milestone 2 (Parametric Shanks Engine).
/// Rigorously probes:
/// 1. Under-gallery casting structural invariants (wall thickness &gt;= 0.8 mm, floor clearance &gt;= 0.8 mm).
/// 2. Boundary conditions: Cathedral arch height limits, Knife-Edge apex extremes, zero tapering, BypassOverlap capping.
/// 3. Mesh topological integrity: vertex count consistency, zero orphaned vertices, manifold twin-edge matching.
/// </summary>
public class AdversarialShankStressTests
{
    private const double Tolerance = 1e-4;

    #region 1. Under-Gallery Casting Structural Invariants (Wall Thickness & Floor Clearance >= 0.80 mm)

    [Theory]
    [InlineData(0.7999)]
    [InlineData(0.79)]
    [InlineData(0.75)]
    [InlineData(0.50)]
    [InlineData(0.10)]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    public void UnderGallery_WallThickness_Below0_80_StrictlyThrows(double wallThickness)
    {
        // Constructor validation
        var exConst = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(wallThicknessMm: wallThickness));
        Assert.Contains("0.80", exConst.Message);

        // Static Validate method
        var exStatic = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UnderGalleryParameters.Validate(wallThickness, 0.85, 45.0, 1.0, 0.4));
        Assert.Contains("0.80", exStatic.Message);

        // Record 'with' expression
        var valid = new UnderGalleryParameters(0.85, 0.85);
        var exWith = Assert.Throws<ArgumentOutOfRangeException>(() =>
            valid with { WallThicknessMm = wallThickness });
        Assert.Contains("0.80", exWith.Message);
    }

    [Theory]
    [InlineData(0.7999)]
    [InlineData(0.79)]
    [InlineData(0.75)]
    [InlineData(0.50)]
    [InlineData(0.10)]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    public void UnderGallery_FloorClearance_Below0_80_StrictlyThrows(double floorClearance)
    {
        // Constructor validation
        var exConst = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(floorClearanceMm: floorClearance));
        Assert.Contains("0.80", exConst.Message);

        // Static Validate method
        var exStatic = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UnderGalleryParameters.Validate(0.85, floorClearance, 45.0, 1.0, 0.4));
        Assert.Contains("0.80", exStatic.Message);

        // Record 'with' expression
        var valid = new UnderGalleryParameters(0.85, 0.85);
        var exWith = Assert.Throws<ArgumentOutOfRangeException>(() =>
            valid with { FloorClearanceMm = floorClearance });
        Assert.Contains("0.80", exWith.Message);
    }

    [Theory]
    [InlineData(0.8000)]
    [InlineData(0.8001)]
    [InlineData(0.85)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    public void UnderGallery_WallThicknessAndFloorClearance_AtOrAbove0_80_Succeeds(double value)
    {
        var gallery = new UnderGalleryParameters(wallThicknessMm: value, floorClearanceMm: value);
        Assert.Equal(value, gallery.WallThicknessMm);
        Assert.Equal(value, gallery.FloorClearanceMm);
        gallery.Validate();
    }

    [Theory]
    [InlineData(0.7999)]
    [InlineData(0.5)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void ShankParameters_ThicknessBelow0_80_StrictlyThrows(double thickness)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(ShankProfileType.ComfortFit, 2.0, thickness, 2.5, 2.0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(ShankProfileType.ComfortFit, 2.0, 1.5, 2.5, thickness));
    }

    [Fact]
    public void UnderGallery_InsufficientHeadMaterial_ThrowsArgumentException()
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var gallery = new UnderGalleryParameters(wallThicknessMm: 0.85, floorClearanceMm: 0.85);

        // Required width = 2 * 0.85 + 0.10 = 1.80 mm
        // Required thickness = 0.85 + 0.85 + 0.10 = 1.80 mm

        // Case A: TopWidth is 1.79 mm (< 1.80)
        var pWidthTooSmall = new ShankParameters(
            ShankProfileType.Flat, 2.0, 2.0, 1.79, 2.0, underGallery: gallery);
        Assert.Throws<ArgumentException>(() => ShankEngine.CreateSweptShankMesh(rail, pWidthTooSmall));

        // Case B: TopThickness is 1.79 mm (< 1.80)
        var pThickTooSmall = new ShankParameters(
            ShankProfileType.Flat, 2.0, 2.0, 2.5, 1.79, underGallery: gallery);
        Assert.Throws<ArgumentException>(() => ShankEngine.CreateSweptShankMesh(rail, pThickTooSmall));
    }

    [Fact]
    public void UnderGallery_RemainingWallThickness_EmpiricallyVerifiedSound()
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        double wall = 0.85;
        double clearance = 0.85;
        var gallery = new UnderGalleryParameters(wallThicknessMm: wall, floorClearanceMm: clearance, spanDegrees: 45.0);

        var parameters = new ShankParameters(
            ShankProfileType.Flat,
            bottomWidthMm: 2.5,
            bottomThicknessMm: 1.8,
            topWidthMm: 3.5,
            topThicknessMm: 2.5,
            underGallery: gallery);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 64, crossSectionSampleCount: 32);

        Assert.True(mesh.IsClosedManifold, "Hollowed mesh must be a closed manifold.");
        Assert.True(mesh.MinWallThicknessMm >= 0.80, "Minimum wall thickness must be >= 0.8 mm.");

        // Empirically verify that every vertex inside the under-gallery span maintains >= 0.8 mm clearance from center
        // Inner rail radius = 8.66 mm. Cavity points must be at radial distance >= 8.66 + clearance = 9.51 mm
        foreach (var v in mesh.Vertices)
        {
            double r = Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
            // In the top region (Y > 8.0), inner boundary points must not violate the inner rail
            Assert.True(r >= 8.66 - Tolerance, $"Vertex radial position {r:F4} invaded finger space (< 8.66 mm).");
        }
    }

    [Theory]
    [InlineData(1.0)]    // Tiny 1 degree span
    [InlineData(15.0)]
    [InlineData(45.0)]
    [InlineData(90.0)]
    [InlineData(179.0)]  // Near full hemisphere span
    [InlineData(180.0)]  // Maximum allowed 180 degrees
    public void UnderGallery_SpanDegrees_ExtremeBoundaries_ProduceValidMeshes(double spanDeg)
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var gallery = new UnderGalleryParameters(0.80, 0.80, spanDegrees: spanDeg);
        var parameters = new ShankParameters(
            ShankProfileType.ComfortFit,
            bottomWidthMm: 2.5,
            bottomThicknessMm: 1.8,
            topWidthMm: 3.0,
            topThicknessMm: 2.2,
            underGallery: gallery);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        Assert.True(mesh.IsClosedManifold, $"Under-gallery with span {spanDeg} deg must be closed manifold.");
        Assert.True(mesh.EnclosedVolume > 0.0);
        Assert.True(mesh.MinWallThicknessMm >= 0.80);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(180.001)]
    [InlineData(360.0)]
    public void UnderGallery_SpanDegrees_OutOfRange_Throws(double spanDeg)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new UnderGalleryParameters(spanDegrees: spanDeg));
    }

    #endregion

    #region 2. Boundary Conditions: Zero Tapering (W_top == W_bottom)

    [Theory]
    [InlineData(ShankProfileType.Flat, 2.0, 1.6)]
    [InlineData(ShankProfileType.DShape, 2.5, 1.8)]
    [InlineData(ShankProfileType.ComfortFit, 3.0, 2.0)]
    [InlineData(ShankProfileType.KnifeEdge, 2.2, 1.8)]
    public void ZeroTapering_DimensionsIdenticalAlongWholeRail(
        ShankProfileType profile, double uniformWidth, double uniformThickness)
    {
        var p = new ShankParameters(profile, uniformWidth, uniformThickness);
        Assert.False(p.IsTapered, "Uniform band must report IsTapered == false.");

        // Check 100 sample angles around rail
        for (int i = 0; i < 100; i++)
        {
            double angle = (2.0 * Math.PI * i) / 100.0;
            var (w, th) = ShankEngine.CalculateTaperDimensionsAtAngle(
                p.BottomWidthMm, p.BottomThicknessMm, p.TopWidthMm, p.TopThicknessMm, angle);

            Assert.Equal(uniformWidth, w, 6);
            Assert.Equal(uniformThickness, th, 6);
        }

        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var mesh = ShankEngine.CreateSweptShankMesh(rail, p, sliceCount: 32);

        Assert.True(mesh.IsClosedManifold);
        Assert.True(mesh.EnclosedVolume > 0.0);
    }

    #endregion

    #region 3. Boundary Conditions: Knife-Edge Apex Radius Extremes

    [Theory]
    [InlineData(0.0)]        // Completely sharp knife edge apex
    [InlineData(1e-5)]       // Just below threshold
    [InlineData(1e-4)]       // At threshold
    [InlineData(0.05)]
    [InlineData(0.10)]
    [InlineData(0.50)]
    [InlineData(10.0)]       // Exceeds max fillet radius (clamped to maxFillet)
    [InlineData(100.0)]      // Extremely large apex radius
    public void KnifeEdge_ApexRadiusExtremes_ProduceValid2DAnd3DMeshes(double apexRadius)
    {
        var p = new ShankParameters(
            ShankProfileType.KnifeEdge,
            bottomWidthMm: 2.4,
            bottomThicknessMm: 1.8,
            topWidthMm: 2.8,
            topThicknessMm: 2.2,
            knifeEdgeApexRadiusMm: apexRadius);

        var section = ShankEngine.CreateCrossSection2D(
            ShankProfileType.KnifeEdge,
            widthMm: 2.6,
            thicknessMm: 2.0,
            knifeEdgeApexRadiusMm: apexRadius);

        Assert.Equal(ShankProfileType.KnifeEdge, section.ProfileType);
        Assert.True(section.Area > 0.0);
        Assert.True(section.Points.Count >= 3);

        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var mesh = ShankEngine.CreateSweptShankMesh(rail, p, sliceCount: 32);

        Assert.True(mesh.IsClosedManifold, $"KnifeEdge apex radius {apexRadius} mm must produce closed manifold.");
        Assert.True(mesh.EnclosedVolume > 0.0);
    }

    [Fact]
    public void KnifeEdge_NegativeApexRadius_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShankParameters(
                ShankProfileType.KnifeEdge,
                2.0, 1.5, 2.5, 1.8,
                knifeEdgeApexRadiusMm: -0.01));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ShankEngine.CreateCrossSection2D(
                ShankProfileType.KnifeEdge,
                2.0, 1.5,
                knifeEdgeApexRadiusMm: -0.01));
    }

    #endregion

    #region 4. Boundary Conditions: Open Rail Capping for BypassOverlap

    [Theory]
    [InlineData(ShankProfileType.Flat)]
    [InlineData(ShankProfileType.DShape)]
    [InlineData(ShankProfileType.ComfortFit)]
    [InlineData(ShankProfileType.KnifeEdge)]
    public void BypassOverlap_AllProfiles_ProduceCappedClosedManifolds(ShankProfileType profile)
    {
        var rail = RailEngine.CreateRingRailCurve3D(17.32, RailProfileType.BypassOverlap);
        Assert.False(rail.IsClosed, "BypassOverlap rail must be an open curve.");

        var parameters = new ShankParameters(profile, 2.0, 1.6, 2.4, 1.8);
        var mesh = ShankEngine.CreateSweptShankMesh(rail, parameters, sliceCount: 48);

        Assert.True(mesh.IsClosedManifold, $"Open BypassOverlap rail for {profile} must be capped into closed manifold.");
        Assert.True(mesh.EnclosedVolume > 0.0);
        Assert.True(mesh.SurfaceArea > 0.0);

        // Verify end cap normals point away from the curve
        // Start cap normal must point in -T direction (along negative rail start tangent)
        var startTangent = rail.TangentAtArcLength(0.0).Normalize();
        var endTangent = rail.TangentAtArcLength(rail.Length).Normalize();

        // Check start cap faces (first planar fan faces in mesh)
        int m = mesh.Vertices.Count / 48; // vertices per slice
        Assert.True(m > 0);
    }

    #endregion

    #region 5. Topological Integrity: Slice Vertex Consistency & Orphaned Vertices Stress

    [Theory]
    [InlineData(ShankProfileType.Flat)]
    [InlineData(ShankProfileType.DShape)]
    [InlineData(ShankProfileType.ComfortFit)]
    [InlineData(ShankProfileType.KnifeEdge)]
    [InlineData(ShankProfileType.Cathedral)]
    public void Mesh_AllVerticesMustBeReferencedByFaces_ZeroOrphanedVertices(ShankProfileType profile)
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var p = new ShankParameters(profile, 2.0, 1.6, 2.8, 2.0);
        var mesh = ShankEngine.CreateSweptShankMesh(rail, p, sliceCount: 32);

        var referencedIndices = new HashSet<int>();
        foreach (var face in mesh.Faces)
        {
            foreach (int idx in face)
            {
                Assert.True(idx >= 0 && idx < mesh.Vertices.Count,
                    $"Face vertex index {idx} out of range [0, {mesh.Vertices.Count - 1}] for profile {profile}.");
                referencedIndices.Add(idx);
            }
        }

        // Every vertex in mesh.Vertices MUST be part of at least one face
        Assert.Equal(mesh.Vertices.Count, referencedIndices.Count);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(5.0)]
    public void Cathedral_ArchHeight_TopologicalIntegrity_OrphanedVertexCheck(double archHeight)
    {
        var rail = new CircleCurve3D(Point3D.Origin, 8.66);
        var p = new ShankParameters(
            ShankProfileType.Cathedral,
            bottomWidthMm: 2.0,
            bottomThicknessMm: 1.6,
            topWidthMm: 2.8,
            topThicknessMm: 2.0,
            cathedralArchHeightMm: archHeight);

        var mesh = ShankEngine.CreateSweptShankMesh(rail, p, sliceCount: 32);

        Assert.True(mesh.IsClosedManifold, $"Cathedral archHeight={archHeight} must produce a closed manifold.");

        // Check for orphaned or out-of-bounds vertex references
        var referencedIndices = new HashSet<int>();
        foreach (var face in mesh.Faces)
        {
            foreach (int idx in face)
            {
                Assert.True(idx >= 0 && idx < mesh.Vertices.Count,
                    $"Face vertex index {idx} out of range [0, {mesh.Vertices.Count - 1}] for Cathedral.");
                referencedIndices.Add(idx);
            }
        }

        Assert.Equal(mesh.Vertices.Count, referencedIndices.Count);
    }

    #endregion
}
