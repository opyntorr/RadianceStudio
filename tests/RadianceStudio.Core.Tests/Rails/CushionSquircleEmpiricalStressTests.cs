namespace RadianceStudio.Core.Tests.Rails;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Geometry;
using Xunit;

/// <summary>
/// Empirical Challenger Stress Test Suite for CushionSquircleCurve3D:
/// 1. Arc-length perimeter quadrature accuracy oracle across varied diameters and superellipse exponents (n in [2.1, 4.0]).
/// 2. Tangents and normals at cardinal angles (s=0, L/4, L/2, 3L/4, L) and intermediate angles across all 4 quadrants.
/// 3. Cardinal epsilon perturbation stability.
/// 4. Inward-pointing normal invariant (P . N &lt; 0) and orthonormality (|T.N| &lt; 1e-5, |T| == 1, |N| == 1).
/// </summary>
public class CushionSquircleEmpiricalStressTests
{
    private const double MaxPerimeterErrorMm = 0.005; // 5 micrometers tolerance requirement
    private const double VectorTolerance = 1e-5;

    public static IEnumerable<object[]> GenerateQuadratureGrid()
    {
        double[] exponents = { 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 3.0, 3.2, 3.4, 3.5, 3.6, 3.8, 4.0 };
        double[] diameters =
        {
            10.0,    // Petite ring
            11.63,   // US Size 0 boundary
            12.038,  // UK Size A boundary
            12.732,  // EU Size 40 boundary
            13.0,    // JP Size 1 boundary
            15.0,    // Intermediate
            16.51,   // US Size 6
            17.32,   // US Size 7 (Canonical standard)
            18.132,  // US Size 8
            20.0,    // Intermediate large
            22.667,  // JP Size 30 boundary
            24.191,  // EU Size 76 boundary
            24.635,  // US Size 16 boundary
            30.0     // Extra large shank
        };

        foreach (double d in diameters)
        {
            foreach (double n in exponents)
            {
                yield return new object[] { d, n };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GenerateQuadratureGrid))]
    public void PerimeterQuadrature_ComprehensiveGrid_MatchesTargetWithinFiveMicrons(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double targetCircumference = Math.PI * innerDiameterMm;

        // Verify Curve Length property matches target circle circumference
        Assert.True(Math.Abs(squircle.Length - targetCircumference) <= 1e-6,
            $"Squircle.Length {squircle.Length:F6} deviates from target {targetCircumference:F6}");

        // Numerical arc-length integration oracle: 10,000 Riemann chord steps
        const int steps = 10000;
        double numericalPerimeter = 0.0;
        Point3D prevPt = squircle.PointAtArcLength(0.0);

        for (int i = 1; i <= steps; i++)
        {
            double s = (i * squircle.Length) / steps;
            Point3D pt = squircle.PointAtArcLength(s);
            numericalPerimeter += pt.DistanceTo(prevPt);
            prevPt = pt;
        }

        double perimeterError = Math.Abs(numericalPerimeter - targetCircumference);
        Assert.True(perimeterError <= MaxPerimeterErrorMm,
            $"CushionSquircle (D={innerDiameterMm}, n={exponent}): Numerical perimeter {numericalPerimeter:F6} differs from target {targetCircumference:F6} by {perimeterError:F6} mm (allowed <= {MaxPerimeterErrorMm} mm)");
    }

    [Theory]
    [InlineData(12.038, 2.1)]
    [InlineData(17.32, 2.5)]
    [InlineData(17.32, 3.0)]
    [InlineData(17.32, 3.5)]
    [InlineData(17.32, 4.0)]
    [InlineData(24.635, 3.0)]
    public void CardinalAngles_ExactTangentsAndInwardNormals(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double L = squircle.Length;
        double a = squircle.SemiAxis;

        // Cardinal 1: s = 0 (theta = 0)
        // Point is (a, 0, 0), Tangent is (0, 1, 0), Inward Normal is (-1, 0, 0)
        Point3D p0 = squircle.PointAtArcLength(0.0);
        Vector3D t0 = squircle.TangentAtArcLength(0.0);
        Vector3D n0 = squircle.NormalAtArcLength(0.0);

        Assert.True(Math.Abs(p0.X - a) <= 1e-5, $"p0.X {p0.X:F6} != {a:F6}");
        Assert.True(Math.Abs(p0.Y) <= 1e-5, $"p0.Y {p0.Y:F6} != 0");
        Assert.True(Math.Abs(t0.X) <= 1e-6 && Math.Abs(t0.Y - 1.0) <= 1e-6, $"t0 {t0} != (0, 1, 0)");
        Assert.True(Math.Abs(n0.X - (-1.0)) <= 1e-6 && Math.Abs(n0.Y) <= 1e-6, $"n0 {n0} != (-1, 0, 0)");
        Assert.True(Math.Abs(t0.Length - 1.0) <= 1e-6, $"|t0| {t0.Length:F6} != 1.0");
        Assert.True(Math.Abs(n0.Length - 1.0) <= 1e-6, $"|n0| {n0.Length:F6} != 1.0");
        Assert.True(Math.Abs(Vector3D.Dot(t0, n0)) <= 1e-6, $"t0 . n0 {Vector3D.Dot(t0, n0):E3} != 0");
        Assert.True(Vector3D.Dot(new Vector3D(p0.X, p0.Y, 0.0), n0) < -0.1, "n0 must point inward (P . N < 0)");

        // Cardinal 2: s = L / 4 (theta = pi / 2)
        // Tangent is (-1, 0, 0), Inward Normal is (0, -1, 0)
        Point3D pQuarter = squircle.PointAtArcLength(0.25 * L);
        Vector3D tQuarter = squircle.TangentAtArcLength(0.25 * L);
        Vector3D nQuarter = squircle.NormalAtArcLength(0.25 * L);

        Assert.True(Math.Abs(tQuarter.X - (-1.0)) <= 1e-6 && Math.Abs(tQuarter.Y) <= 1e-6, $"tQuarter {tQuarter} != (-1, 0, 0)");
        Assert.True(Math.Abs(nQuarter.X) <= 1e-6 && Math.Abs(nQuarter.Y - (-1.0)) <= 1e-6, $"nQuarter {nQuarter} != (0, -1, 0)");
        Assert.True(Math.Abs(tQuarter.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(nQuarter.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(tQuarter, nQuarter)) <= 1e-6);
        Assert.True(Vector3D.Dot(new Vector3D(pQuarter.X, pQuarter.Y, 0.0), nQuarter) < -0.1, "nQuarter must point inward");

        // Coordinates at s = L / 4: Point should be at crown top (0, a, 0)
        Assert.True(Math.Abs(pQuarter.X) <= 0.001, $"pQuarter.X {pQuarter.X:F6} != 0");
        Assert.True(Math.Abs(pQuarter.Y - a) <= 0.001, $"pQuarter.Y {pQuarter.Y:F6} != {a:F6}");

        // Cardinal 3: s = L / 2 (theta = pi)
        // Point is (-a, 0, 0), Tangent is (0, -1, 0), Inward Normal is (1, 0, 0)
        Point3D pHalf = squircle.PointAtArcLength(0.5 * L);
        Vector3D tHalf = squircle.TangentAtArcLength(0.5 * L);
        Vector3D nHalf = squircle.NormalAtArcLength(0.5 * L);

        Assert.True(Math.Abs(pHalf.X - (-a)) <= 0.001, $"pHalf.X {pHalf.X:F6} != {-a:F6}");
        Assert.True(Math.Abs(pHalf.Y) <= 0.001, $"pHalf.Y {pHalf.Y:F6} != 0");
        Assert.True(Math.Abs(tHalf.X) <= 1e-6 && Math.Abs(tHalf.Y - (-1.0)) <= 1e-6, $"tHalf {tHalf} != (0, -1, 0)");
        Assert.True(Math.Abs(nHalf.X - 1.0) <= 1e-6 && Math.Abs(nHalf.Y) <= 1e-6, $"nHalf {nHalf} != (1, 0, 0)");
        Assert.True(Math.Abs(tHalf.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(nHalf.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(tHalf, nHalf)) <= 1e-6);
        Assert.True(Vector3D.Dot(new Vector3D(pHalf.X, pHalf.Y, 0.0), nHalf) < -0.1, "nHalf must point inward");

        // Cardinal 4: s = 3L / 4 (theta = 3pi / 2)
        // Point is (0, -a, 0), Tangent is (1, 0, 0), Inward Normal is (0, 1, 0)
        Point3D p3Quarter = squircle.PointAtArcLength(0.75 * L);
        Vector3D t3Quarter = squircle.TangentAtArcLength(0.75 * L);
        Vector3D n3Quarter = squircle.NormalAtArcLength(0.75 * L);

        Assert.True(Math.Abs(p3Quarter.X) <= 0.001, $"p3Quarter.X {p3Quarter.X:F6} != 0");
        Assert.True(Math.Abs(p3Quarter.Y - (-a)) <= 0.001, $"p3Quarter.Y {p3Quarter.Y:F6} != {-a:F6}");
        Assert.True(Math.Abs(t3Quarter.X - 1.0) <= 1e-6 && Math.Abs(t3Quarter.Y) <= 1e-6, $"t3Quarter {t3Quarter} != (1, 0, 0)");
        Assert.True(Math.Abs(n3Quarter.X) <= 1e-6 && Math.Abs(n3Quarter.Y - 1.0) <= 1e-6, $"n3Quarter {n3Quarter} != (0, 1, 0)");
        Assert.True(Math.Abs(t3Quarter.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(n3Quarter.Length - 1.0) <= 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(t3Quarter, n3Quarter)) <= 1e-6);
        Assert.True(Vector3D.Dot(new Vector3D(p3Quarter.X, p3Quarter.Y, 0.0), n3Quarter) < -0.1, "n3Quarter must point inward");

        // Loop closure at s = L
        Point3D pL = squircle.PointAtArcLength(L);
        Vector3D tL = squircle.TangentAtArcLength(L);
        Vector3D nL = squircle.NormalAtArcLength(L);
        Assert.True(p0.DistanceTo(pL) <= 1e-6, "Squircle must close exactly at s=L");
        Assert.True(Math.Abs(t0.X - tL.X) <= 1e-6 && Math.Abs(t0.Y - tL.Y) <= 1e-6, "t(0) must equal t(L)");
        Assert.True(Math.Abs(n0.X - nL.X) <= 1e-6 && Math.Abs(n0.Y - nL.Y) <= 1e-6, "n(0) must equal n(L)");
    }

    [Theory]
    [InlineData(17.32, 2.5)]
    [InlineData(17.32, 3.0)]
    [InlineData(17.32, 4.0)]
    public void CardinalPerturbations_StabilityNearAxes(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double L = squircle.Length;
        double[] cardinalS = { 0.0, 0.25 * L, 0.5 * L, 0.75 * L };
        double[] deltas = { 1e-7, 1e-6, 1e-5, 1e-4, 1e-3, 1e-2, 0.1 };

        foreach (double sc in cardinalS)
        {
            foreach (double delta in deltas)
            {
                foreach (double sign in new[] { -1.0, 1.0 })
                {
                    double s = sc + (sign * delta);
                    Point3D p = squircle.PointAtArcLength(s);
                    Vector3D t = squircle.TangentAtArcLength(s);
                    Vector3D n = squircle.NormalAtArcLength(s);

                    Assert.False(t.IsZero(), $"t at s={s:F7} degenerated to zero");
                    Assert.False(n.IsZero(), $"n at s={s:F7} degenerated to zero");
                    Assert.True(Math.Abs(t.Length - 1.0) <= VectorTolerance, $"|t|={t.Length:F6} != 1.0 at s={s:F7}");
                    Assert.True(Math.Abs(n.Length - 1.0) <= VectorTolerance, $"|n|={n.Length:F6} != 1.0 at s={s:F7}");
                    Assert.True(Math.Abs(Vector3D.Dot(t, n)) <= VectorTolerance, $"t.n={Vector3D.Dot(t, n):E3} != 0 at s={s:F7}");

                    double dotInward = (p.X * n.X) + (p.Y * n.Y);
                    Assert.True(dotInward < -0.1, $"P . N ({dotInward:F4}) must be strictly negative at s={s:F7}");
                }
            }
        }
    }

    [Theory]
    [InlineData(12.038, 2.2)]
    [InlineData(16.51, 2.8)]
    [InlineData(17.32, 3.0)]
    [InlineData(18.132, 3.5)]
    [InlineData(24.635, 4.0)]
    public void DenseArcSweep_AllFourQuadrants_InwardAndOrthonormal(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double L = squircle.Length;
        const int samples = 1000;

        for (int i = 0; i < samples; i++)
        {
            double s = (i * L) / samples;
            Point3D p = squircle.PointAtArcLength(s);
            Vector3D t = squircle.TangentAtArcLength(s);
            Vector3D n = squircle.NormalAtArcLength(s);

            // Vector non-zero and unit length
            Assert.False(t.IsZero(), $"Tangent at s={s:F4} is zero");
            Assert.False(n.IsZero(), $"Normal at s={s:F4} is zero");
            Assert.True(Math.Abs(t.Length - 1.0) <= VectorTolerance, $"|T|={t.Length:F6} at s={s:F4}");
            Assert.True(Math.Abs(n.Length - 1.0) <= VectorTolerance, $"|N|={n.Length:F6} at s={s:F4}");

            // Orthonormality
            Assert.True(Math.Abs(Vector3D.Dot(t, n)) <= VectorTolerance, $"T.N={Vector3D.Dot(t, n):E3} at s={s:F4}");

            // Inward-pointing condition: P . N < 0
            double dotInward = (p.X * n.X) + (p.Y * n.Y);
            Assert.True(dotInward < -0.1, $"Normal not inward at s={s:F4}: P.N = {dotInward:F4}");

            // Directional quadrant orientation checks (counterclockwise traversal)
            double frac = s / L;
            if (frac > 0.01 && frac < 0.24)
            {
                // Quadrant 1: Tx < 0, Ty > 0
                Assert.True(t.X < 0, $"Q1 Tx should be negative at s={s:F4}, got {t.X:F4}");
                Assert.True(t.Y > 0, $"Q1 Ty should be positive at s={s:F4}, got {t.Y:F4}");
            }
            else if (frac > 0.26 && frac < 0.49)
            {
                // Quadrant 2: Tx < 0, Ty < 0
                Assert.True(t.X < 0, $"Q2 Tx should be negative at s={s:F4}, got {t.X:F4}");
                Assert.True(t.Y < 0, $"Q2 Ty should be negative at s={s:F4}, got {t.Y:F4}");
            }
            else if (frac > 0.51 && frac < 0.74)
            {
                // Quadrant 3: Tx > 0, Ty < 0
                Assert.True(t.X > 0, $"Q3 Tx should be positive at s={s:F4}, got {t.X:F4}");
                Assert.True(t.Y < 0, $"Q3 Ty should be negative at s={s:F4}, got {t.Y:F4}");
            }
            else if (frac > 0.76 && frac < 0.99)
            {
                // Quadrant 4: Tx > 0, Ty > 0
                Assert.True(t.X > 0, $"Q4 Tx should be positive at s={s:F4}, got {t.X:F4}");
                Assert.True(t.Y > 0, $"Q4 Ty should be positive at s={s:F4}, got {t.Y:F4}");
            }
        }
    }

    [Theory]
    [InlineData(12.038, 2.1)]
    [InlineData(17.32, 3.0)]
    [InlineData(24.635, 4.0)]
    public void HighResolutionChordOracle_FiftyThousandSteps_MatchesTargetWithinTwoMicrons(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double targetCircumference = Math.PI * innerDiameterMm;

        // Ultra high resolution oracle: 50,000 steps
        const int steps = 50000;
        double numericalPerimeter = 0.0;
        Point3D prevPt = squircle.PointAtArcLength(0.0);

        for (int i = 1; i <= steps; i++)
        {
            double s = (i * squircle.Length) / steps;
            Point3D pt = squircle.PointAtArcLength(s);
            numericalPerimeter += pt.DistanceTo(prevPt);
            prevPt = pt;
        }

        double perimeterError = Math.Abs(numericalPerimeter - targetCircumference);
        Assert.True(perimeterError < 0.002,
            $"CushionSquircle (D={innerDiameterMm}, n={exponent}): 50,000-step perimeter {numericalPerimeter:F6} deviates from target {targetCircumference:F6} by {perimeterError:F6} mm");
    }

    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public CushionSquircleEmpiricalStressTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StatisticalProfile_PerimeterAndOrthonormalityMetrics_AcrossFullGrid()
    {
        double[] exponents = { 2.1, 2.3, 2.5, 2.7, 3.0, 3.3, 3.5, 3.8, 4.0 };
        double[] diameters = { 10.0, 11.63, 12.038, 12.732, 13.0, 16.51, 17.32, 20.0, 22.667, 24.635, 30.0 };

        double maxPerimeterError = 0.0;
        double minPerimeterError = double.MaxValue;
        double sumPerimeterError = 0.0;
        int count = 0;
        string worstPerimeterCase = "";

        double maxOrthogonalityError = 0.0;
        double maxTangentLengthError = 0.0;
        double maxNormalLengthError = 0.0;
        double maxInwardDot = double.MinValue; // Should be negative, so maxInwardDot should be < 0

        foreach (double d in diameters)
        {
            foreach (double n in exponents)
            {
                var squircle = new RailEngine.CushionSquircleCurve3D(d, n);
                double targetCircumference = Math.PI * d;

                const int steps = 10000;
                double numericalPerimeter = 0.0;
                Point3D prevPt = squircle.PointAtArcLength(0.0);

                for (int i = 1; i <= steps; i++)
                {
                    double s = (i * squircle.Length) / steps;
                    Point3D pt = squircle.PointAtArcLength(s);
                    numericalPerimeter += pt.DistanceTo(prevPt);
                    prevPt = pt;

                    // Also check frame and vectors at sample points
                    if (i % 100 == 0)
                    {
                        Vector3D t = squircle.TangentAtArcLength(s);
                        Vector3D norm = squircle.NormalAtArcLength(s);

                        double dot = Math.Abs(Vector3D.Dot(t, norm));
                        if (dot > maxOrthogonalityError) maxOrthogonalityError = dot;

                        double tLenErr = Math.Abs(t.Length - 1.0);
                        if (tLenErr > maxTangentLengthError) maxTangentLengthError = tLenErr;

                        double nLenErr = Math.Abs(norm.Length - 1.0);
                        if (nLenErr > maxNormalLengthError) maxNormalLengthError = nLenErr;

                        double pDotN = (pt.X * norm.X) + (pt.Y * norm.Y);
                        if (pDotN > maxInwardDot) maxInwardDot = pDotN;

                        var frame = OrthonormalFrame.Create(t, norm);
                        Assert.True(frame.IsOrthonormal());
                    }
                }

                double error = Math.Abs(numericalPerimeter - targetCircumference);
                if (error > maxPerimeterError)
                {
                    maxPerimeterError = error;
                    worstPerimeterCase = $"D={d}mm, n={n}";
                }
                if (error < minPerimeterError) minPerimeterError = error;
                sumPerimeterError += error;
                count++;
            }
        }

        double meanError = sumPerimeterError / count;

        _output.WriteLine($"[PROFILE] Tested Configurations: {count}");
        _output.WriteLine($"[PROFILE] Max Perimeter Error: {maxPerimeterError:F6} mm ({worstPerimeterCase}) [Requirement <= 0.005 mm]");
        _output.WriteLine($"[PROFILE] Min Perimeter Error: {minPerimeterError:F6} mm");
        _output.WriteLine($"[PROFILE] Mean Perimeter Error: {meanError:F6} mm");
        _output.WriteLine($"[PROFILE] Max Tangent Length Error (|T| - 1): {maxTangentLengthError:E3}");
        _output.WriteLine($"[PROFILE] Max Normal Length Error (|N| - 1): {maxNormalLengthError:E3}");
        _output.WriteLine($"[PROFILE] Max Orthogonality Error (|T . N|): {maxOrthogonalityError:E3}");
        _output.WriteLine($"[PROFILE] Max Inward Dot Product (P . N): {maxInwardDot:F4} (must be < 0)");

        Assert.True(maxPerimeterError <= MaxPerimeterErrorMm);
        Assert.True(maxOrthogonalityError <= 1e-5);
        Assert.True(maxInwardDot < -0.1);
    }

    [Fact]
    public void NegativeOrZeroInnerDiameter_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RailEngine.CushionSquircleCurve3D(0.0, 3.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RailEngine.CushionSquircleCurve3D(-1.0, 3.0));
    }
}
