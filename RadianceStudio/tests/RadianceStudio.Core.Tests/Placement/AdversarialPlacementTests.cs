namespace RadianceStudio.Core.Tests.Placement;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

/// <summary>
/// Adversarial stress test suite probing curve placement, orthonormal frames,
/// edge cases (zero/negative length, pavé negative spacing, collinear cross products),
/// high-curvature twisting 3D space curves, and 500-stone performance benchmarks.
/// </summary>
public class AdversarialPlacementTests
{
    private readonly IGemEngine _engine = new GemEngine();

    #region 1. Zero and Negative Usable Length Stress Tests

    [Theory]
    [InlineData(0.0, 2.0, 0.5)]
    [InlineData(-0.001, 2.0, 0.5)]
    [InlineData(-100.0, 2.0, 0.5)]
    [InlineData(-1e-12, 1.0, 0.2)]
    public void CalculateStoneCount_ZeroOrNegativeUsableLength_ReturnsZero(
        double usableLength, double gemSize, double spacing)
    {
        int count = _engine.CalculateStoneCount(usableLength, gemSize, spacing);
        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(-100.0)]
    public void CalculateArcDistances_ZeroOrNegativeUsableLength_ReturnsEmpty(double usableLength)
    {
        var distances = _engine.CalculateArcDistances(usableLength, 2.0, 0.5, 0.0);
        Assert.Empty(distances);
    }

    [Fact]
    public void CalculatePlacements_OffsetsExceedCurveLength_ReturnsEmpty()
    {
        // 10 mm curve, but startOffset (6mm) + endOffset (6mm) = 12mm -> Usable length = -2mm
        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var placements = _engine.CalculatePlacements(
            line, null, GemCutType.RoundBrilliant, 2.0, 0.5,
            startOffsetMm: 6.0, endOffsetMm: 6.0);

        Assert.Empty(placements);
    }

    [Fact]
    public void CalculatePlacements_UsableLengthLessThanGemSize_ReturnsEmpty()
    {
        // 5 mm curve, gemSize = 6 mm -> does not fit
        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(5, 0, 0));
        var placements = _engine.CalculatePlacements(
            line, null, GemCutType.RoundBrilliant, 6.0, 0.5);

        Assert.Empty(placements);
    }

    [Fact]
    public void CalculatePlacements_UsableLengthExactlyEqualsGemSize_PlacesSingleGem()
    {
        // Usable length = 2.0 mm, gemSize = 2.0 mm -> exactly 1 gem fits
        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(2.0, 0, 0));
        var placements = _engine.CalculatePlacements(
            line, null, GemCutType.RoundBrilliant, 2.0, 0.5);

        Assert.Single(placements);
        Assert.Equal(1.0, placements[0].ArcPositionMm, 6); // center at d/2 = 1.0 mm
    }

    #endregion

    #region 2. Negative Spacing (Pavé Overlap Setting) Stress Tests

    [Theory]
    [InlineData(2.6, 1.0, -0.2, 3)] // step = 0.8, first = 0.5, pos: 0.5, 1.3, 2.1 (last stone covers up to 2.6)
    [InlineData(10.0, 2.0, -0.4, 6)] // step = 1.6, ratio = (10 - 2)/1.6 = 5.0 -> N = 6
    [InlineData(5.0, 1.5, -0.5, 4)]  // step = 1.0, ratio = (5 - 1.5)/1.0 = 3.5 -> N = 4
    [InlineData(0.8, 0.8, -0.5, 1)]  // min spec gemSize 0.8mm with -0.5mm spacing -> step = 0.3, fits 1 stone
    public void CalculateStoneCount_NegativeSpacing_CalculatesCorrectPaveCount(
        double usableLength, double gemSize, double negativeSpacing, int expectedCount)
    {
        int count = _engine.CalculateStoneCount(usableLength, gemSize, negativeSpacing);
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void CalculateArcDistances_NegativeSpacing_MaintainsExactOverlapStep()
    {
        double gemSize = 1.0;
        double negativeSpacing = -0.2; // step = 0.8
        double usableLength = 2.6;
        double startOffset = 0.0;

        var distances = _engine.CalculateArcDistances(usableLength, gemSize, negativeSpacing, startOffset);

        Assert.Equal(3, distances.Count);
        Assert.Equal(0.5, distances[0], 6);
        Assert.Equal(1.3, distances[1], 6);
        Assert.Equal(2.1, distances[2], 6);

        // Verify inter-gem step matches GemSize + Spacing (< 0.001 mm tolerance)
        for (int i = 1; i < distances.Count; i++)
        {
            double step = distances[i] - distances[i - 1];
            Assert.Equal(gemSize + negativeSpacing, step, 6);
        }
    }

    [Fact]
    public void CalculatePlacements_NegativeSpacing_ExecutesCorrectlyOn3DCurve()
    {
        var helix = new HelixCurve3D(Point3D.Origin, 10.0, 4.0, 2.0);
        double gemSize = 1.2;
        double negativeSpacing = -0.3; // step = 0.9 mm

        var placements = _engine.CalculatePlacements(
            helix, null, GemCutType.RoundBrilliant, gemSize, negativeSpacing);

        Assert.NotEmpty(placements);
        for (int i = 1; i < placements.Count; i++)
        {
            double deltaS = placements[i].ArcPositionMm - placements[i - 1].ArcPositionMm;
            Assert.Equal(gemSize + negativeSpacing, deltaS, 6);
        }
    }

    [Theory]
    [InlineData(1.0, -1.0)] // step = 0.0 -> degenerate step
    [InlineData(1.0, -1.5)] // step = -0.5 < 0 -> negative step
    [InlineData(0.5, -0.5)] // step = 0.0
    public void CalculateStoneCount_SpacingLessThanOrEqualToNegativeGemSize_ThrowsArgumentException(
        double gemSize, double invalidSpacing)
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.CalculateStoneCount(10.0, gemSize, invalidSpacing));
    }

    #endregion

    #region 3. Collinear Tangent & Normal Vectors (Degenerate Cross Product)

    [Theory]
    [InlineData(0, 0, 1, 0, 0, 1)]     // Parallel along +Z
    [InlineData(0, 0, 1, 0, 0, -1)]    // Anti-parallel along Z
    [InlineData(1, 0, 0, 1, 0, 0)]     // Parallel along +X
    [InlineData(-1, 0, 0, 1, 0, 0)]    // Anti-parallel along X
    [InlineData(0, 1, 0, 0, 1, 0)]     // Parallel along +Y
    [InlineData(0, -1, 0, 0, 1, 0)]    // Anti-parallel along Y
    [InlineData(0.57735, 0.57735, 0.57735, 0.57735, 0.57735, 0.57735)] // Parallel diagonal
    [InlineData(-0.57735, -0.57735, -0.57735, 0.57735, 0.57735, 0.57735)] // Anti-parallel diagonal
    public void OrthonormalFrame_CollinearTangentAndNormal_ResolvesDeterministically(
        double tx, double ty, double tz,
        double nx, double ny, double nz)
    {
        var tangent = new Vector3D(tx, ty, tz);
        var normal = new Vector3D(nx, ny, nz);

        var frame = OrthonormalFrame.Create(tangent, normal);

        Assert.True(frame.IsOrthonormal(1e-6));
        Assert.Equal(1.0, frame.Determinant, 6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Normal)) < 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Binormal)) < 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Normal, frame.Binormal)) < 1e-6);
    }

    [Fact]
    public void OrthonormalFrame_CollinearWithFlipNormal_PreservesRightHandedDeterminant()
    {
        var tangent = new Vector3D(0, 0, 1);
        var normal = new Vector3D(0, 0, 1);

        var normalFrame = OrthonormalFrame.Create(tangent, normal, flipNormal: false);
        var flippedFrame = OrthonormalFrame.Create(tangent, normal, flipNormal: true);

        Assert.True(normalFrame.IsOrthonormal(1e-6));
        Assert.True(flippedFrame.IsOrthonormal(1e-6));

        // Normal must be negated
        Assert.Equal(-normalFrame.Normal.X, flippedFrame.Normal.X, 6);
        Assert.Equal(-normalFrame.Normal.Y, flippedFrame.Normal.Y, 6);
        Assert.Equal(-normalFrame.Normal.Z, flippedFrame.Normal.Z, 6);

        // Determinant must remain +1.0
        Assert.Equal(1.0, normalFrame.Determinant, 6);
        Assert.Equal(1.0, flippedFrame.Determinant, 6);
    }

    [Theory]
    [InlineData(1e-4)]
    [InlineData(1e-6)]
    [InlineData(1e-8)]
    [InlineData(1e-10)]
    public void OrthonormalFrame_NearlyCollinearVectors_MaintainsNumericalStability(double angleOffset)
    {
        // Tangent along Z, normal slightly tilted by angleOffset
        var tangent = Vector3D.UnitZ;
        var normal = new Vector3D(Math.Sin(angleOffset), 0, Math.Cos(angleOffset));

        var frame = OrthonormalFrame.Create(tangent, normal);

        Assert.True(frame.IsOrthonormal(1e-6));
        Assert.Equal(1.0, frame.Determinant, 5);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Normal)) < 1e-6);
    }

    #endregion

    #region 4. High-Curvature and Twisting 3D Space Curves

    /// <summary>
    /// Custom adversarial 3D space curve with extreme curvature and rapid torsion.
    /// Parameterized with non-planar helix of small radius and steep pitch.
    /// </summary>
    private sealed class AdversarialTwistingCurve3D : ICurve3D
    {
        private readonly double _radius;
        private readonly double _pitch;
        private readonly double _turns;
        private readonly double _c;
        private readonly double _omega;
        private readonly double _length;

        public double Length => _length;
        public bool IsClosed => false;

        public AdversarialTwistingCurve3D(double radius = 0.5, double pitch = 0.2, double turns = 20.0)
        {
            _radius = radius;
            _pitch = pitch;
            _turns = turns;
            _c = pitch / (2.0 * Math.PI);
            _omega = Math.Sqrt((radius * radius) + (_c * _c));
            _length = _omega * (2.0 * Math.PI * turns);
        }

        public Point3D PointAtArcLength(double s)
        {
            double t = s / _omega;
            return new Point3D(_radius * Math.Cos(t), _radius * Math.Sin(t), _c * t);
        }

        public Vector3D TangentAtArcLength(double s)
        {
            double t = s / _omega;
            return new Vector3D(-_radius * Math.Sin(t) / _omega, _radius * Math.Cos(t) / _omega, _c / _omega);
        }

        public Vector3D NormalAtArcLength(double s)
        {
            double t = s / _omega;
            return new Vector3D(-Math.Cos(t), -Math.Sin(t), 0.0);
        }

        public double ParameterAtArcLength(double s) => s / _omega;
    }

    [Fact]
    public void CalculatePlacements_OnHighCurvatureTwistingCurve_AllFramesStrictlyOrthonormal()
    {
        // High curvature curve: radius 0.5 mm, pitch 0.2 mm, 20 turns (~63mm length)
        var twistingCurve = new AdversarialTwistingCurve3D(radius: 0.5, pitch: 0.2, turns: 20.0);
        double gemSize = 0.8; // min spec gem size
        double spacing = 0.1;

        var placements = _engine.CalculatePlacements(
            twistingCurve, null, GemCutType.RoundBrilliant, gemSize, spacing);

        Assert.NotEmpty(placements);
        Assert.True(placements.Count > 50);

        foreach (var placement in placements)
        {
            var mat = new Matrix4x4D(placement.Matrix4x4);

            // Extract scaled column vectors
            var col0 = new Vector3D(mat.M00 / gemSize, mat.M10 / gemSize, mat.M20 / gemSize);
            var col1 = new Vector3D(mat.M01 / gemSize, mat.M11 / gemSize, mat.M21 / gemSize);
            var col2 = new Vector3D(mat.M02 / gemSize, mat.M12 / gemSize, mat.M22 / gemSize);

            // Check unit length of basis vectors
            Assert.Equal(1.0, col0.Length, 5);
            Assert.Equal(1.0, col1.Length, 5);
            Assert.Equal(1.0, col2.Length, 5);

            // Strict mutual orthogonality (< 10^-6)
            Assert.True(Math.Abs(Vector3D.Dot(col0, col1)) < 1e-6, $"Col0.Col1 dot was {Vector3D.Dot(col0, col1)}");
            Assert.True(Math.Abs(Vector3D.Dot(col0, col2)) < 1e-6, $"Col0.Col2 dot was {Vector3D.Dot(col0, col2)}");
            Assert.True(Math.Abs(Vector3D.Dot(col1, col2)) < 1e-6, $"Col1.Col2 dot was {Vector3D.Dot(col1, col2)}");

            // Determinant must be +1.0
            double detR = col0.Dot(col1.Cross(col2));
            Assert.Equal(1.0, detR, 5);

            // Position must exactly match curve evaluation
            var expectedPt = twistingCurve.PointAtArcLength(placement.ArcPositionMm);
            Assert.Equal(expectedPt.X, mat.M03, 5);
            Assert.Equal(expectedPt.Y, mat.M13, 5);
            Assert.Equal(expectedPt.Z, mat.M23, 5);
        }
    }

    #endregion

    #region 5. Orthonormality Across 10,000 Arbitrary Vectors (Stress Oracle)

    [Fact]
    public void OrthonormalFrame_10000RandomVectors_AllProduceStrictlyOrthonormalBasis()
    {
        var rng = new Random(42); // Deterministic seed for reproducible stress run
        int iterations = 10_000;

        for (int i = 0; i < iterations; i++)
        {
            // Generate non-zero random tangent
            double tx = (rng.NextDouble() * 2.0) - 1.0;
            double ty = (rng.NextDouble() * 2.0) - 1.0;
            double tz = (rng.NextDouble() * 2.0) - 1.0;
            if (Math.Abs(tx) + Math.Abs(ty) + Math.Abs(tz) < 1e-3)
            {
                tx = 1.0;
            }
            var tangent = new Vector3D(tx, ty, tz);

            // Generate random candidate normal, occasionally collinear or zero
            Vector3D normal;
            int mode = rng.Next(10);
            if (mode == 0)
            {
                // Exact parallel
                normal = tangent;
            }
            else if (mode == 1)
            {
                // Exact anti-parallel
                normal = -tangent;
            }
            else if (mode == 2)
            {
                // Zero vector
                normal = Vector3D.Zero;
            }
            else if (mode == 3)
            {
                // Almost parallel (slight perturbation)
                normal = tangent + new Vector3D(1e-7, 1e-7, 1e-7);
            }
            else
            {
                // General random vector
                normal = new Vector3D(
                    (rng.NextDouble() * 2.0) - 1.0,
                    (rng.NextDouble() * 2.0) - 1.0,
                    (rng.NextDouble() * 2.0) - 1.0);
            }

            bool flip = (rng.Next(2) == 1);
            var frame = OrthonormalFrame.Create(tangent, normal, flip);

            // Assertions on frame properties
            Assert.True(frame.IsOrthonormal(1e-6),
                $"Failed orthonormality at iteration {i}: T={frame.Tangent}, N={frame.Normal}, B={frame.Binormal}");
            Assert.Equal(1.0, frame.Determinant, 5);
            Assert.True(Math.Abs(frame.Tangent.Dot(frame.Normal)) < 1e-6);
            Assert.True(Math.Abs(frame.Tangent.Dot(frame.Binormal)) < 1e-6);
            Assert.True(Math.Abs(frame.Normal.Dot(frame.Binormal)) < 1e-6);
        }
    }

    #endregion

    #region 6. Performance Stress: 500-Stone Placement Benchmark (< 10 ms)

    [Fact]
    public void CalculatePlacements_500Stones_MultiIterationBenchmark_StrictlyUnder10Ms()
    {
        // Helix with 30 turns to guarantee > 500 stones
        var helix = new HelixCurve3D(Point3D.Origin, 25.0, 10.0, 30.0);
        double gemSize = 2.0;
        double spacing = 0.5;

        // JIT warm-up
        for (int w = 0; w < 5; w++)
        {
            _ = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
        }

        // Run 20 iterations and measure each
        int iterations = 20;
        long totalTicks = 0;
        long maxTicks = 0;

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var placements = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
            sw.Stop();

            long ticks = sw.ElapsedTicks;
            totalTicks += ticks;
            if (ticks > maxTicks) maxTicks = ticks;

            Assert.True(placements.Count >= 500, $"Iteration {i}: Expected >= 500 stones, got {placements.Count}");
            Assert.True(sw.ElapsedMilliseconds < 10,
                $"Iteration {i} exceeded 10 ms budget: took {sw.ElapsedMilliseconds} ms ({ticks} ticks)");
        }

        double avgMs = (double)totalTicks / iterations / Stopwatch.Frequency * 1000.0;
        double maxMs = (double)maxTicks / Stopwatch.Frequency * 1000.0;

        // Verify average and maximum execution times are well within the 10 ms limit
        Assert.True(avgMs < 5.0, $"Average execution time {avgMs:F2} ms exceeded 5.0 ms target.");
        Assert.True(maxMs < 10.0, $"Max execution time {maxMs:F2} ms exceeded 10.0 ms limit.");
    }

    [Fact]
    public void CalculatePlacements_Scalability_2500Stones_ExecutesLinearly()
    {
        // 2,500 stones on long helix (150 turns)
        var longHelix = new HelixCurve3D(Point3D.Origin, 25.0, 10.0, 150.0);
        double gemSize = 2.0;
        double spacing = 0.5;

        // Warmup
        _ = _engine.CalculatePlacements(longHelix, null, GemCutType.RoundBrilliant, gemSize, spacing);

        var sw = Stopwatch.StartNew();
        var placements = _engine.CalculatePlacements(longHelix, null, GemCutType.RoundBrilliant, gemSize, spacing);
        sw.Stop();

        Assert.True(placements.Count >= 2500, $"Expected >= 2500 stones, got {placements.Count}");
        // 2500 stones should execute comfortably in < 25 ms (~10 microseconds per stone)
        Assert.True(sw.ElapsedMilliseconds < 30,
            $"2,500-stone placement took {sw.ElapsedMilliseconds} ms (must be < 30 ms)");
    }

    #endregion

    #region 7. Affine Matrix Placement Mathematical Invariants

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 1.0, 0.0)]
    [InlineData(GemCutType.RoundBrilliant, 2.5, 45.0)]
    [InlineData(GemCutType.Princess, 1.5, 90.0)]
    [InlineData(GemCutType.Oval, 2.0, 30.0)]
    [InlineData(GemCutType.Pear, 1.8, 60.0)]
    [InlineData(GemCutType.Emerald, 3.0, 180.0)]
    [InlineData(GemCutType.Marquise, 2.2, 270.0)]
    public void Matrix4x4D_CreatePlacement_MatchesExplicitMatrixMultiplication(
        GemCutType cutType, double gemSize, double rotationDeg)
    {
        var tangent = new Vector3D(1, 2, 3).Normalize();
        var candidateNormal = new Vector3D(-1, 0, 1).Normalize();
        var frame = OrthonormalFrame.Create(tangent, candidateNormal);
        var position = new Point3D(10.5, -20.3, 30.1);

        double aspectRatio = GemDimensions.GetDefault(cutType, gemSize).AspectRatio;

        // Method under test: closed-form CreatePlacement
        var closedFormMat = Matrix4x4D.CreatePlacement(frame, position, gemSize, aspectRatio, rotationDeg);

        // Explicit multiplication: M = [B T* N P] * Rz(deg) * S(sx, sy, sz)
        var frameMat = Matrix4x4D.CreateFromFrame(frame, position);
        var rotMat = Matrix4x4D.CreateRotationZDegrees(rotationDeg);
        var scaleMat = Matrix4x4D.CreateScale(gemSize, gemSize * aspectRatio, gemSize);
        var explicitMat = frameMat.Multiply(rotMat).Multiply(scaleMat);

        // Verify all 16 elements match within floating point precision
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                Assert.Equal(explicitMat[r, c], closedFormMat[r, c], 6);
            }
        }

        // Verify determinant of 3x3 submatrix equals gemSize^3 * aspectRatio
        double expectedDet = gemSize * gemSize * gemSize * aspectRatio;
        Assert.Equal(expectedDet, closedFormMat.Determinant3x3(), 5);

        // Verify origin maps to position
        var transformedOrigin = closedFormMat.Transform(Point3D.Origin);
        Assert.Equal(position.X, transformedOrigin.X, 6);
        Assert.Equal(position.Y, transformedOrigin.Y, 6);
        Assert.Equal(position.Z, transformedOrigin.Z, 6);
    }

    #endregion
}
