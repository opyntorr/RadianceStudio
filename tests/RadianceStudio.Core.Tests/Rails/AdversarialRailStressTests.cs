namespace RadianceStudio.Core.Tests.Rails;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Xunit;

/// <summary>
/// Empirical adversarial stress test harness for RailEngine:
/// 1. Verifies sizing accuracy for US, UK, EU, JP across standard and fractional sizes (quarter, half) with error &lt;= 0.01 mm.
/// 2. Independent numerical arc-length integration oracles for EuroShank and CushionSquircle (error &lt;= 0.01 mm vs target circle circumference).
/// 3. Verification of C0/C1 continuity, unit tangent, and normal orthogonality across segment junctions.
/// 4. 3D spatial clearance and non-self-intersection oracle for BypassOverlap curve.
/// 5. Boundary, out-of-range, and degenerate input stress testing.
/// </summary>
public class AdversarialRailStressTests
{
    private const double ToleranceMm = 0.01;
    private const double HighPrecisionTol = 1e-6;

    #region 1. Sizing Precision & Grid Generators: US / North America

    public static IEnumerable<object[]> GenerateUsGrid()
    {
        for (double s = 0.0; s <= 16.0001; s += 0.25)
        {
            yield return new object[] { s };
        }
    }

    [Theory]
    [MemberData(nameof(GenerateUsGrid))]
    public void UsSizing_FullQuarterGrid_MatchesStandardFormulaWithinTolerance(double sizeUs)
    {
        // Standard formula: D = 11.63 + 0.8128 * S
        double expectedDiameter = 11.63 + (sizeUs * 0.8128);
        double actualDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, sizeUs);

        double error = Math.Abs(actualDiameter - expectedDiameter);
        Assert.True(error <= HighPrecisionTol,
            $"US size {sizeUs}: diameter {actualDiameter:F6} deviates from theoretical {expectedDiameter:F6} by {error:E3} mm");

        // Roundtrip verification
        double inverseSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, actualDiameter, out string? ukStr);
        Assert.Null(ukStr);
        Assert.True(Math.Abs(inverseSize - sizeUs) <= HighPrecisionTol,
            $"US size {sizeUs}: inverse size {inverseSize:F6} does not roundtrip");

        // Check circumference matches pi * D
        double circumference = Math.PI * actualDiameter;
        var info = RailEngine.GetSizeInfo(RingSizingStandard.US, sizeUs);
        Assert.True(Math.Abs(info.InnerCircumferenceMm - circumference) <= HighPrecisionTol);
        Assert.True(Math.Abs(info.InnerDiameterMm - actualDiameter) <= HighPrecisionTol);
    }

    #endregion

    #region 2. Sizing Precision & Grid Generators: UK / Australia (BS 6820 Wheatsheaf)

    public static IEnumerable<object[]> GenerateUkGrid()
    {
        // Letters A to Z (k = 0 to 25) with half sizes
        for (double k = 0.0; k <= 25.5001; k += 0.5)
        {
            yield return new object[] { k };
        }
        // Extended sizes Z+1 to Z+6 with half sizes
        for (double k = 26.0; k <= 31.0001; k += 0.5)
        {
            yield return new object[] { k };
        }
    }

    [Theory]
    [MemberData(nameof(GenerateUkGrid))]
    public void UkSizing_FullGrid_MatchesStandardFormulaWithinTolerance(double k)
    {
        // BS 6820 formula: C = 37.82 + 1.25 * k, D = C / PI
        double expectedCircumference = 37.82 + (1.25 * k);
        double expectedDiameter = expectedCircumference / Math.PI;

        double actualDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, k);
        double error = Math.Abs(actualDiameter - expectedDiameter);
        Assert.True(error <= HighPrecisionTol,
            $"UK index {k}: diameter {actualDiameter:F6} deviates from theoretical {expectedDiameter:F6} by {error:E3} mm");

        // Bidirectional roundtrip
        double inverseK = RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, actualDiameter, out string? ukLabel);
        Assert.NotNull(ukLabel);
        Assert.True(Math.Abs(inverseK - k) <= HighPrecisionTol,
            $"UK index {k}: inverse k {inverseK:F6} does not roundtrip");

        // Verify label roundtrips back to same diameter
        double labelDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukLabel);
        Assert.True(Math.Abs(labelDiameter - actualDiameter) <= HighPrecisionTol,
            $"UK label '{ukLabel}' parsed to diameter {labelDiameter:F6} != {actualDiameter:F6}");
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData("b 1/2", "B 1/2")]
    [InlineData("m 1/2", "M 1/2")]
    [InlineData("m½", "M 1/2")]
    [InlineData("m.5", "M 1/2")]
    [InlineData("z", "Z")]
    [InlineData("z+1", "Z+1")]
    [InlineData("Z+2 1/2", "Z+2 1/2")]
    [InlineData("z+6", "Z+6")]
    public void UkSizing_StringParsingVariants_ProducesAccurateLabelAndDiameter(string inputStr, string expectedNormalizedLabel)
    {
        double diameterFromInput = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, inputStr);
        double diameterFromCanonical = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, expectedNormalizedLabel);

        Assert.True(Math.Abs(diameterFromInput - diameterFromCanonical) <= HighPrecisionTol,
            $"Input '{inputStr}' gave diameter {diameterFromInput:F6} != {diameterFromCanonical:F6}");

        RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, diameterFromInput, out string? formattedLabel);
        Assert.Equal(expectedNormalizedLabel, formattedLabel);
    }

    #endregion

    #region 3. Sizing Precision & Grid Generators: Europe / ISO 8653

    public static IEnumerable<object[]> GenerateEuGrid()
    {
        // ISO 8653: inner circumference in mm from 40 to 76 in 0.5 mm steps
        for (double c = 40.0; c <= 76.0001; c += 0.5)
        {
            yield return new object[] { c };
        }
    }

    [Theory]
    [MemberData(nameof(GenerateEuGrid))]
    public void EuSizing_FullSweep_MatchesStandardFormulaWithinTolerance(double circumferenceMm)
    {
        // ISO 8653 formula: D = C / PI
        double expectedDiameter = circumferenceMm / Math.PI;
        double actualDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, circumferenceMm);

        double error = Math.Abs(actualDiameter - expectedDiameter);
        Assert.True(error <= HighPrecisionTol,
            $"EU size {circumferenceMm}: diameter {actualDiameter:F6} deviates from {expectedDiameter:F6} by {error:E3} mm");

        // Bidirectional roundtrip
        double inverseEu = RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, actualDiameter, out string? ukStr);
        Assert.Null(ukStr);
        Assert.True(Math.Abs(inverseEu - circumferenceMm) <= HighPrecisionTol,
            $"EU size {circumferenceMm}: inverse {inverseEu:F6} does not roundtrip");
    }

    #endregion

    #region 4. Sizing Precision & Grid Generators: Japan / East Asia (JIS B 4700)

    public static IEnumerable<object[]> GenerateJpGrid()
    {
        // JIS B 4700 sizes 1 to 30 in 0.5 steps
        for (double s = 1.0; s <= 30.0001; s += 0.5)
        {
            yield return new object[] { s };
        }
    }

    [Theory]
    [MemberData(nameof(GenerateJpGrid))]
    public void JpSizing_FullSweep_MatchesStandardFormulaWithinTolerance(double sizeJp)
    {
        // JIS B 4700 formula: D = (38.0 + S) / 3.0
        double expectedDiameter = (38.0 + sizeJp) / 3.0;
        double actualDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, sizeJp);

        double error = Math.Abs(actualDiameter - expectedDiameter);
        Assert.True(error <= HighPrecisionTol,
            $"JP size {sizeJp}: diameter {actualDiameter:F6} deviates from {expectedDiameter:F6} by {error:E3} mm");

        // Bidirectional roundtrip
        double inverseJp = RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, actualDiameter, out string? ukStr);
        Assert.Null(ukStr);
        Assert.True(Math.Abs(inverseJp - sizeJp) <= HighPrecisionTol,
            $"JP size {sizeJp}: inverse {inverseJp:F6} does not roundtrip");
    }

    #endregion

    #region 5. EuroShank Stress Testing & Numerical Arc-Length Oracle

    [Theory]
    [InlineData(12.038, 0.0)]        // UK A (small)
    [InlineData(16.510, 0.0)]        // US 6
    [InlineData(17.320, 0.0)]        // US 7 (default)
    [InlineData(18.132, 0.0)]        // US 8
    [InlineData(24.635, 0.0)]        // US 16 (large)
    [InlineData(17.320, 2.0)]        // US 7 custom corner radius
    [InlineData(17.320, 4.5)]        // US 7 large corner radius
    public void EuroShank_NumericalArcLengthOracle_MatchesCircumferenceWithinTolerance(
        double innerDiameterMm, double customCornerRadius)
    {
        var euro = new RailEngine.EuroShankCurve3D(innerDiameterMm, customCornerRadius);
        double targetCircumference = Math.PI * innerDiameterMm;

        // Oracle: Numerical Riemann sum of chord lengths along the curve
        const int steps = 10000;
        double numericalPerimeter = 0.0;
        Point3D prevPt = euro.PointAtArcLength(0.0);

        for (int i = 1; i <= steps; i++)
        {
            double s = (i * euro.Length) / steps;
            Point3D pt = euro.PointAtArcLength(s);
            numericalPerimeter += (pt - prevPt).Length;
            prevPt = pt;
        }

        double perimeterError = Math.Abs(numericalPerimeter - targetCircumference);
        Assert.True(perimeterError <= ToleranceMm,
            $"EuroShank (D={innerDiameterMm}, rc={customCornerRadius}): Numerical perimeter {numericalPerimeter:F4} differs from target circumference {targetCircumference:F4} by {perimeterError:F5} mm (tol: {ToleranceMm})");

        Assert.True(Math.Abs(euro.Length - targetCircumference) <= HighPrecisionTol,
            $"Reported euro.Length {euro.Length:F6} must match target circumference {targetCircumference:F6}");
    }

    [Theory]
    [InlineData(16.510)]
    [InlineData(17.320)]
    [InlineData(20.000)]
    public void EuroShank_ContinuityAndUnitTangent_AtSegmentBoundaries(double innerDiameterMm)
    {
        var euro = new RailEngine.EuroShankCurve3D(innerDiameterMm);
        double L = euro.Length;

        // Verify C0 position continuity and C1 unit tangent across 1000 samples
        const int samples = 1000;
        Point3D prevPt = euro.PointAtArcLength(0.0);

        for (int i = 1; i <= samples; i++)
        {
            double s = (i * L) / samples;
            Point3D pt = euro.PointAtArcLength(s);
            Vector3D t = euro.TangentAtArcLength(s);
            Vector3D n = euro.NormalAtArcLength(s);

            // Chord distance between adjacent dense samples must be small
            double ds = (pt - prevPt).Length;
            Assert.True(ds < (2.0 * L / samples), $"Discontinuity detected at s={s:F3}: step distance {ds:F4}");

            // Tangent must be strictly unit length
            double tLen = t.Length;
            Assert.True(Math.Abs(tLen - 1.0) <= 1e-5,
                $"EuroShank tangent at s={s:F3} is not unit length: |T|={tLen:F6}");

            // Normal must be strictly unit length
            double nLen = n.Length;
            Assert.True(Math.Abs(nLen - 1.0) <= 1e-5,
                $"EuroShank normal at s={s:F3} is not unit length: |N|={nLen:F6}");

            // Tangent and normal must be orthogonal
            double dot = Math.Abs(t.Dot(n));
            Assert.True(dot <= 1e-5,
                $"EuroShank tangent and normal are not orthogonal at s={s:F3}: T.N={dot:E3}");

            prevPt = pt;
        }

        // Loop closure: PointAtArcLength(L) must equal PointAtArcLength(0)
        Point3D pt0 = euro.PointAtArcLength(0.0);
        Point3D ptL = euro.PointAtArcLength(L);
        Assert.True(pt0.DistanceTo(ptL) <= 1e-5, "EuroShank curve must close exactly at s=L");
    }

    [Fact]
    public void EuroShank_AntiRotationFlatBottom_AndCornerLobesGeometry()
    {
        double diameter = 17.32;
        var euro = new RailEngine.EuroShankCurve3D(diameter);

        // Find the lowest point and verify the bottom is flat
        int samples = 500;
        double minY = double.MaxValue;
        var bottomPoints = new List<Point3D>();

        for (int i = 0; i < samples; i++)
        {
            double s = (i * euro.Length) / samples;
            Point3D pt = euro.PointAtArcLength(s);
            if (pt.Y < minY) minY = pt.Y;
        }

        for (int i = 0; i < samples; i++)
        {
            double s = (i * euro.Length) / samples;
            Point3D pt = euro.PointAtArcLength(s);
            if (Math.Abs(pt.Y - minY) < 1e-3)
            {
                bottomPoints.Add(pt);
            }
        }

        // Flat bottom span must contain multiple points spanning across X
        Assert.True(bottomPoints.Count >= 10, "EuroShank bottom must have a flat horizontal span.");
        double minX = double.MaxValue, maxX = double.MinValue;
        foreach (var p in bottomPoints)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
        }

        double flatSpan = maxX - minX;
        Assert.True(flatSpan > 3.0, $"EuroShank flat bottom span {flatSpan:F2} mm must be substantial (> 3 mm)");
    }

    #endregion

    #region 6. Cushion Squircle Stress Testing & Numerical Arc-Length Oracle

    [Theory]
    [InlineData(12.038, 2.5)]
    [InlineData(16.510, 3.0)]
    [InlineData(17.320, 2.5)]
    [InlineData(17.320, 3.0)]
    [InlineData(17.320, 3.5)]
    [InlineData(17.320, 4.0)]
    [InlineData(20.000, 3.0)]
    [InlineData(24.635, 3.0)]
    public void CushionSquircle_NumericalArcLengthOracle_MatchesCircumferenceWithinTolerance(
        double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double targetCircumference = Math.PI * innerDiameterMm;

        // Oracle: Numerical Riemann sum of chord lengths along squircle
        const int steps = 10000;
        double numericalPerimeter = 0.0;
        Point3D prevPt = squircle.PointAtArcLength(0.0);

        for (int i = 1; i <= steps; i++)
        {
            double s = (i * squircle.Length) / steps;
            Point3D pt = squircle.PointAtArcLength(s);
            numericalPerimeter += (pt - prevPt).Length;
            prevPt = pt;
        }

        double perimeterError = Math.Abs(numericalPerimeter - targetCircumference);
        Assert.True(perimeterError <= ToleranceMm,
            $"CushionSquircle (D={innerDiameterMm}, n={exponent}): Numerical perimeter {numericalPerimeter:F4} differs from target {targetCircumference:F4} by {perimeterError:F5} mm (tol: {ToleranceMm})");

        Assert.True(Math.Abs(squircle.Length - targetCircumference) <= HighPrecisionTol);
    }

    [Theory]
    [InlineData(17.320, 3.0)]
    [InlineData(18.132, 2.5)]
    [InlineData(20.000, 4.0)]
    public void CushionSquircle_DihedralSymmetry_PreservedAcrossQuadrants(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double L = squircle.Length;

        // In squircle, quadrants at s, L/2 - s, L/2 + s, L - s must possess mirror symmetries
        for (int i = 1; i < 20; i++)
        {
            double s = (i * L) / 80.0; // In first octant
            Point3D p1 = squircle.PointAtArcLength(s);
            Point3D p2 = squircle.PointAtArcLength((0.5 * L) - s); // Quadrant 2 reflection across Y
            Point3D p3 = squircle.PointAtArcLength((0.5 * L) + s); // Quadrant 3 reflection across origin
            Point3D p4 = squircle.PointAtArcLength(L - s);         // Quadrant 4 reflection across X

            Assert.True(Math.Abs(p1.X + p2.X) <= 0.05, $"X symmetry failed between Q1 and Q2 at s={s:F3}: {p1.X:F3} vs {p2.X:F3}");
            Assert.True(Math.Abs(p1.Y - p2.Y) <= 0.05, $"Y symmetry failed between Q1 and Q2 at s={s:F3}: {p1.Y:F3} vs {p2.Y:F3}");
            Assert.True(Math.Abs(p1.X + p3.X) <= 0.05, $"X symmetry failed between Q1 and Q3 at s={s:F3}: {p1.X:F3} vs {p3.X:F3}");
            Assert.True(Math.Abs(p1.Y + p3.Y) <= 0.05, $"Y symmetry failed between Q1 and Q3 at s={s:F3}: {p1.Y:F3} vs {p3.Y:F3}");
            Assert.True(Math.Abs(p1.X - p4.X) <= 0.05, $"X symmetry failed between Q1 and Q4 at s={s:F3}: {p1.X:F3} vs {p4.X:F3}");
            Assert.True(Math.Abs(p1.Y + p4.Y) <= 0.05, $"Y symmetry failed between Q1 and Q4 at s={s:F3}: {p1.Y:F3} vs {p4.Y:F3}");
        }
    }

    [Theory]
    [InlineData(17.320, 3.0)]
    public void CushionSquircle_TangentAndNormal_NonDegenerateAcrossArc(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double L = squircle.Length;

        // Sample points across the squircle, probing cardinal axes and intermediate angles
        int sampleCount = 64;
        for (int i = 0; i < sampleCount; i++)
        {
            double s = (i * L) / sampleCount;
            Vector3D t = squircle.TangentAtArcLength(s);
            Vector3D n = squircle.NormalAtArcLength(s);

            // Tangent and normal vectors must never degenerate to zero
            Assert.False(t.IsZero(), $"Squircle tangent at s={s:F3} (i={i}/{sampleCount}) degenerated to zero!");
            Assert.False(n.IsZero(), $"Squircle normal at s={s:F3} (i={i}/{sampleCount}) degenerated to zero!");

            // Tangent and normal must be unit vectors
            Assert.True(Math.Abs(t.Length - 1.0) <= 0.05, $"Squircle tangent length {t.Length:F4} != 1.0 at s={s:F3}");
            Assert.True(Math.Abs(n.Length - 1.0) <= 0.05, $"Squircle normal length {n.Length:F4} != 1.0 at s={s:F3}");

            // Tangent and normal must be mutually orthogonal
            double dot = Math.Abs(t.Dot(n));
            Assert.True(dot <= 0.05, $"Squircle tangent and normal are not orthogonal at s={s:F3}: T.N={dot:F4}");
        }
    }

    #endregion

    #region 7. Bypass Overlap Stress Testing: 3D Clearance & Non-Self-Intersection Oracle

    [Theory]
    [InlineData(12.038, 20.0, 1.5, 1.0)]
    [InlineData(16.510, 45.0, 2.0, 1.5)]
    [InlineData(17.320, 45.0, 2.0, 1.5)]
    [InlineData(18.132, 60.0, 2.5, 2.0)]
    [InlineData(22.000, 90.0, 3.0, 2.0)]
    public void BypassOverlap_3DSeparationAndNonSelfIntersectionOracle(
        double innerDiameterMm, double overlapDeg, double axialOffsetMm, double radialOffsetMm)
    {
        var bypass = new RailEngine.BypassOverlapCurve3D(
            innerDiameterMm, overlapDeg, axialOffsetMm, radialOffsetMm);

        Assert.False(bypass.IsClosed, "Bypass overlap rail must be an open curve.");
        Assert.True(bypass.Length > Math.PI * innerDiameterMm,
            "Bypass overlap curve length must exceed circular circumference.");

        // Oracle 1: Dense all-pairs non-adjacent collision detection
        const int sampleCount = 200;
        var points = new Point3D[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            double s = (i * bypass.Length) / (sampleCount - 1);
            points[i] = bypass.PointAtArcLength(s);
        }

        double minNonAdjacentDistance = double.MaxValue;
        int minI = -1, minJ = -1;

        // Check all non-adjacent pairs (|i - j| >= 5) to ensure curve never self-intersects
        for (int i = 0; i < sampleCount; i++)
        {
            for (int j = i + 5; j < sampleCount; j++)
            {
                double dist = points[i].DistanceTo(points[j]);
                if (dist < minNonAdjacentDistance)
                {
                    minNonAdjacentDistance = dist;
                    minI = i;
                    minJ = j;
                }
            }
        }

        // Curve must strictly maintain non-zero clearance everywhere
        Assert.True(minNonAdjacentDistance >= 0.8,
            $"Bypass overlap self-intersection or clearance breach: min distance = {minNonAdjacentDistance:F3} mm between i={minI} and j={minJ}");

        // Oracle 2: Verify start and end points maintain guaranteed axial & radial separation
        Point3D startPt = points[0];
        Point3D endPt = points[sampleCount - 1];

        double deltaZ = Math.Abs(endPt.Z - startPt.Z);
        double rStart = Math.Sqrt((startPt.X * startPt.X) + (startPt.Y * startPt.Y));
        double rEnd = Math.Sqrt((endPt.X * endPt.X) + (endPt.Y * endPt.Y));
        double deltaR = Math.Abs(rEnd - rStart);
        double dist3D = startPt.DistanceTo(endPt);

        Assert.True(deltaZ >= 1.35, $"Bypass axial clearance {deltaZ:F3} mm must be >= 1.35 mm");
        Assert.True(deltaR >= 0.85, $"Bypass radial clearance {deltaR:F3} mm must be >= 0.85 mm");
        Assert.True(dist3D >= 1.8, $"Bypass 3D end gap {dist3D:F3} mm must be >= 1.8 mm");

        // Oracle 3: Tangent and Normal invariants across the 3D space curve
        for (int i = 0; i < sampleCount; i++)
        {
            double s = (i * bypass.Length) / (sampleCount - 1);
            Vector3D t = bypass.TangentAtArcLength(s);
            Vector3D n = bypass.NormalAtArcLength(s);

            Assert.False(t.IsZero(), $"Bypass tangent is zero at s={s:F3}");
            Assert.False(n.IsZero(), $"Bypass normal is zero at s={s:F3}");
            Assert.True(Math.Abs(t.Length - 1.0) <= 1e-4, $"Bypass tangent not normalized at s={s:F3}");
            Assert.True(Math.Abs(n.Length - 1.0) <= 1e-4, $"Bypass normal not normalized at s={s:F3}");
            Assert.True(Math.Abs(t.Dot(n)) <= 1e-4, $"Bypass tangent and normal not orthogonal at s={s:F3}");
        }
    }

    #endregion

    #region 8. Degenerate & Boundary Input Robustness

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-100.0)]
    public void DegenerateInputs_NonPositiveDiameter_ThrowsArgumentOutOfRangeException(double invalidDiameter)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, invalidDiameter, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, invalidDiameter, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, invalidDiameter, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, invalidDiameter, out _));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.GetSizeInfoFromDiameter(invalidDiameter));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(invalidDiameter, RailProfileType.CanonicalCircle));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(invalidDiameter, RailProfileType.EuroShank));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(invalidDiameter, RailProfileType.CushionSquircle));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(invalidDiameter, RailProfileType.BypassOverlap));
    }

    [Theory]
    [InlineData("XYZ")]
    [InlineData("Z+")]
    [InlineData("Z+Foo")]
    [InlineData("A 1/3")]
    [InlineData("B++")]
    public void DegenerateInputs_MalformedUkStrings_ThrowsArgumentException(string malformedUkStr)
    {
        Assert.Throws<ArgumentException>(() =>
            RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, malformedUkStr));
    }

    [Fact]
    public void UkSizing_NegativeExtension_ShouldThrowArgumentException()
    {
        // "Z+-1" is semantically invalid and must be rejected
        Assert.Throws<ArgumentException>(() =>
            RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, "Z+-1"));
    }

    [Fact]
    public void ArcLengthNormalization_ExtremeAndPeriodicParameters_HandledSafely()
    {
        var euro = new RailEngine.EuroShankCurve3D(17.32);
        double L = euro.Length;

        // Negative parameter wraps cleanly
        Point3D ptNeg = euro.PointAtArcLength(-L * 0.25);
        Point3D ptPos = euro.PointAtArcLength(L * 0.75);
        Assert.True(ptNeg.DistanceTo(ptPos) <= 1e-5);

        // Parameter > Length wraps cleanly
        Point3D ptLarge = euro.PointAtArcLength(L * 5.25);
        Point3D ptEquiv = euro.PointAtArcLength(L * 0.25);
        Assert.True(ptLarge.DistanceTo(ptEquiv) <= 1e-5);
    }

    #endregion
}
