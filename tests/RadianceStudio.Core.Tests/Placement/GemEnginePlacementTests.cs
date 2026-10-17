namespace RadianceStudio.Core.Tests.Placement;

using System;
using System.Diagnostics;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

public class GemEnginePlacementTests
{
    private readonly IGemEngine _engine = new GemEngine();

    [Theory]
    [InlineData(10.0, 2.0, 0.5, 4)]
    [InlineData(9.5, 2.0, 0.5, 4)]
    [InlineData(2.0, 2.0, 0.5, 1)]
    [InlineData(1.9, 2.0, 0.5, 0)]
    [InlineData(0.0, 2.0, 0.5, 0)]
    [InlineData(7.5, 1.5, 0.5, 4)] // 7.5 = 1.5 + 3 * 2.0, exact division boundary
    public void CalculateStoneCount_ComputesAccurateCounts(
        double usableLength,
        double gemSize,
        double spacing,
        int expectedCount)
    {
        int count = _engine.CalculateStoneCount(usableLength, gemSize, spacing);
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void CalculateStoneCount_ThrowsOnInvalidParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.CalculateStoneCount(10.0, 0.0, 0.5));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _engine.CalculateStoneCount(10.0, -1.0, 0.5));

        Assert.Throws<ArgumentException>(() =>
            _engine.CalculateStoneCount(10.0, 1.0, -1.0)); // step = 0
    }

    [Fact]
    public void CalculateArcDistances_MaintainsExactSpacingTolerance()
    {
        double usableLength = 20.0;
        double gemSize = 2.0;
        double spacing = 0.5;
        double startOffset = 1.0;

        var distances = _engine.CalculateArcDistances(usableLength, gemSize, spacing, startOffset);

        Assert.NotEmpty(distances);
        Assert.Equal(startOffset + (gemSize * 0.5), distances[0], 6);

        double expectedStep = gemSize + spacing;
        for (int i = 1; i < distances.Count; i++)
        {
            double step = distances[i] - distances[i - 1];
            Assert.Equal(expectedStep, step, 6); // Tolerance < 0.001 mm
        }
    }

    [Fact]
    public void CalculatePlacements_OnLineCurve_ProducesValidPlacements()
    {
        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(50, 0, 0));
        var placements = _engine.CalculatePlacements(
            curve: line,
            surface: null,
            cutType: GemCutType.RoundBrilliant,
            gemSizeMm: 2.0,
            spacingMm: 0.5,
            startOffsetMm: 2.0,
            endOffsetMm: 2.0);

        Assert.NotEmpty(placements);

        foreach (var placement in placements)
        {
            Assert.Equal(GemCutType.RoundBrilliant, placement.CutType);
            Assert.Equal(2.0, placement.GemSizeMm);

            // Verify matrix position aligns with point on curve
            var mat = new Matrix4x4D(placement.Matrix4x4);
            var pt = mat.Transform(Point3D.Origin);
            Assert.Equal(placement.ArcPositionMm, pt.X, 4);
            Assert.Equal(0.0, pt.Y, 4);
            Assert.Equal(0.0, pt.Z, 4);

            // Orthonormality of rotation columns
            double sx = placement.GemSizeMm;
            double sy = placement.GemSizeMm;
            double sz = placement.GemSizeMm;

            var col0 = new Vector3D(mat.M00 / sx, mat.M10 / sx, mat.M20 / sx);
            var col1 = new Vector3D(mat.M01 / sy, mat.M11 / sy, mat.M21 / sy);
            var col2 = new Vector3D(mat.M02 / sz, mat.M12 / sz, mat.M22 / sz);

            Assert.True(Math.Abs(Vector3D.Dot(col0, col1)) < 1e-6);
            Assert.True(Math.Abs(Vector3D.Dot(col0, col2)) < 1e-6);
            Assert.True(Math.Abs(Vector3D.Dot(col1, col2)) < 1e-6);

            double detR = col0.Dot(col1.Cross(col2));
            Assert.Equal(1.0, detR, 5);
        }
    }

    [Fact]
    public void CalculatePlacements_OnCircleCurveWithSurface_PreservesSurfaceNormal()
    {
        var circle = new CircleCurve3D(Point3D.Origin, 10.0);
        var surface = new PlaneSurface3D(Vector3D.UnitZ);

        var placements = _engine.CalculatePlacements(
            curve: circle,
            surface: surface,
            cutType: GemCutType.Princess,
            gemSizeMm: 1.5,
            spacingMm: 0.3,
            startOffsetMm: 0.0,
            endOffsetMm: 0.0);

        Assert.NotEmpty(placements);

        foreach (var placement in placements)
        {
            var mat = new Matrix4x4D(placement.Matrix4x4);

            // Normal is column 2 divided by gemSize
            var normal = new Vector3D(mat.M02 / 1.5, mat.M12 / 1.5, mat.M22 / 1.5);
            Assert.Equal(0.0, normal.X, 6);
            Assert.Equal(0.0, normal.Y, 6);
            Assert.Equal(1.0, normal.Z, 6);
        }
    }

    [Fact]
    public void CalculatePlacements_WithFlipNormal_InvertsNormalAndPreservesDet()
    {
        var helix = new HelixCurve3D(Point3D.Origin, 15.0, 5.0, 3.0);

        var standard = _engine.CalculatePlacements(helix, null, GemCutType.Oval, 2.0, 0.4, flipNormal: false);
        var flipped = _engine.CalculatePlacements(helix, null, GemCutType.Oval, 2.0, 0.4, flipNormal: true);

        Assert.Equal(standard.Count, flipped.Count);
        Assert.NotEmpty(standard);

        for (int i = 0; i < standard.Count; i++)
        {
            var mStd = new Matrix4x4D(standard[i].Matrix4x4);
            var mFlip = new Matrix4x4D(flipped[i].Matrix4x4);

            var nStd = new Vector3D(mStd.M02, mStd.M12, mStd.M22);
            var nFlip = new Vector3D(mFlip.M02, mFlip.M12, mFlip.M22);

            Assert.Equal(-nStd.X, nFlip.X, 5);
            Assert.Equal(-nStd.Y, nFlip.Y, 5);
            Assert.Equal(-nStd.Z, nFlip.Z, 5);

            // Both must be right-handed with uniform scaling (gemSize = 2.0 on all 3 axes)
            double detStd = mStd.Determinant3x3() / (2.0 * 2.0 * 2.0);
            double detFlip = mFlip.Determinant3x3() / (2.0 * 2.0 * 2.0);

            Assert.Equal(1.0, detStd, 5);
            Assert.Equal(1.0, detFlip, 5);
        }
    }

    [Fact]
    public void CalculatePlacements_Benchmark_500Stones_ExecutesInUnder10Milliseconds()
    {
        // Helix with 25 turns, radius 20mm, total length > 3000mm to host > 500 stones
        var helix = new HelixCurve3D(Point3D.Origin, 20.0, 10.0, 25.0);
        double gemSize = 2.0;
        double spacing = 0.5;

        // JIT warm-up
        for (int i = 0; i < 5; i++)
        {
            _ = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
        }

        double minElapsedMs = double.MaxValue;
        IReadOnlyList<GemPlacement>? placements = null;
        for (int i = 0; i < 5; i++)
        {
            var sw = Stopwatch.StartNew();
            placements = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
            sw.Stop();
            if (sw.Elapsed.TotalMilliseconds < minElapsedMs)
            {
                minElapsedMs = sw.Elapsed.TotalMilliseconds;
            }
        }

        Assert.NotNull(placements);
        Assert.True(placements.Count >= 500, $"Expected at least 500 stones, got {placements.Count}");
        Assert.True(minElapsedMs < 10.0,
            $"Expected 500-stone placement calculation to execute in < 10 ms, but best took {minElapsedMs:F2} ms");
    }
}
