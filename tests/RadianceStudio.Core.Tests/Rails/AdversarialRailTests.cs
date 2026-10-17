namespace RadianceStudio.Core.Tests.Rails;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Xunit;

/// <summary>
/// Independent empirical adversarial challenge and stress suite for Milestone 1 (International Rails).
/// Tests:
/// 1. Sizing boundary conditions (US 0/16, UK A/Z+6, ISO 40/76, JP 1/30) and strict inverse roundtrips (<= 0.001 mm).
/// 2. Comprehensive subdivision mappings across full, half, and quarter sizes.
/// 3. Negative and adversarial input handling (negative/zero diameters, malformed UK strings, out-of-range parameters).
/// 4. Rail profile geometry stress testing (EuroShank, CushionSquircle, BypassOverlap).
/// 5. Orthonormality and coordinate frame construction across rail profiles.
/// </summary>
public class AdversarialRailTests
{
    private const double StrictRoundtripToleranceMm = 0.001; // 1 micrometer

    #region 1. Boundary Condition Sizing & Strict Inverse Roundtrip (<= 0.001 mm)

    [Theory]
    [InlineData(0.0, 11.6300)]      // US Size 0 boundary
    [InlineData(16.0, 24.6348)]     // US Size 16 boundary
    public void UsSizing_BoundaryConditions_RoundtripWithinStrictTolerance(double sizeUs, double expectedDiameterMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, sizeUs);
        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= StrictRoundtripToleranceMm,
            $"US boundary size {sizeUs} diameter {diameter:F4} should equal {expectedDiameterMm:F4}");

        double recoveredSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, diameter, out string? ukLabel);
        Assert.Null(ukLabel);
        Assert.True(Math.Abs(recoveredSize - sizeUs) <= 1e-6,
            $"US boundary size {sizeUs} inverse recovered {recoveredSize:F6}");

        double recoveredDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, recoveredSize);
        double deltaMm = Math.Abs(recoveredDiameter - diameter);
        Assert.True(deltaMm <= StrictRoundtripToleranceMm,
            $"US boundary size {sizeUs} roundtrip diameter drift {deltaMm:E4} exceeds {StrictRoundtripToleranceMm} mm");
    }

    [Theory]
    [InlineData("A", 12.0384, 0.0)]       // UK Size A boundary
    [InlineData("Z+6", 24.3730, 31.0)]    // UK Size Z+6 boundary
    public void UkSizing_BoundaryConditions_RoundtripWithinStrictTolerance(string ukSizeStr, double expectedDiameterMm, double expectedK)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukSizeStr);
        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= 0.01,
            $"UK boundary size '{ukSizeStr}' diameter {diameter:F4} should match expected {expectedDiameterMm:F4}");

        double recoveredK = RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, diameter, out string? formattedLabel);
        Assert.NotNull(formattedLabel);
        Assert.Equal(ukSizeStr.Replace(" ", ""), formattedLabel.Replace(" ", ""));
        Assert.True(Math.Abs(recoveredK - expectedK) <= 1e-6,
            $"UK boundary size '{ukSizeStr}' recovered k {recoveredK} should equal {expectedK}");

        double recoveredDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, formattedLabel);
        double deltaMm = Math.Abs(recoveredDiameter - diameter);
        Assert.True(deltaMm <= StrictRoundtripToleranceMm,
            $"UK boundary size '{ukSizeStr}' roundtrip diameter drift {deltaMm:E4} exceeds {StrictRoundtripToleranceMm} mm");
    }

    [Theory]
    [InlineData(40.0, 12.7324)]     // ISO 40 boundary
    [InlineData(76.0, 24.1915)]     // ISO 76 boundary
    public void EuIsoSizing_BoundaryConditions_RoundtripWithinStrictTolerance(double isoCircumferenceMm, double expectedDiameterMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, isoCircumferenceMm);
        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= StrictRoundtripToleranceMm,
            $"ISO boundary size {isoCircumferenceMm} diameter {diameter:F4} should equal {expectedDiameterMm:F4}");

        double recoveredCircumference = RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, diameter, out string? ukLabel);
        Assert.Null(ukLabel);
        Assert.True(Math.Abs(recoveredCircumference - isoCircumferenceMm) <= 1e-6,
            $"ISO boundary size {isoCircumferenceMm} inverse recovered {recoveredCircumference:F6}");

        double recoveredDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, recoveredCircumference);
        double deltaMm = Math.Abs(recoveredDiameter - diameter);
        Assert.True(deltaMm <= StrictRoundtripToleranceMm,
            $"ISO boundary size {isoCircumferenceMm} roundtrip diameter drift {deltaMm:E4} exceeds {StrictRoundtripToleranceMm} mm");
    }

    [Theory]
    [InlineData(1.0, 13.0000)]      // JP 1 boundary
    [InlineData(30.0, 22.6667)]     // JP 30 boundary
    public void JpSizing_BoundaryConditions_RoundtripWithinStrictTolerance(double sizeJp, double expectedDiameterMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, sizeJp);
        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= StrictRoundtripToleranceMm,
            $"JP boundary size {sizeJp} diameter {diameter:F4} should equal {expectedDiameterMm:F4}");

        double recoveredSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, diameter, out string? ukLabel);
        Assert.Null(ukLabel);
        Assert.True(Math.Abs(recoveredSize - sizeJp) <= 1e-6,
            $"JP boundary size {sizeJp} inverse recovered {recoveredSize:F6}");

        double recoveredDiameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, recoveredSize);
        double deltaMm = Math.Abs(recoveredDiameter - diameter);
        Assert.True(deltaMm <= StrictRoundtripToleranceMm,
            $"JP boundary size {sizeJp} roundtrip diameter drift {deltaMm:E4} exceeds {StrictRoundtripToleranceMm} mm");
    }

    #endregion

    #region 2. Comprehensive Subdivision Inverse Roundtrips (<= 0.001 mm)

    [Fact]
    public void UsSizing_AllQuarterSizes_RoundtripWithinStrictTolerance()
    {
        // Test all quarter sizes from 0.0 to 16.0 (65 distinct sizes)
        for (double s = 0.0; s <= 16.001; s += 0.25)
        {
            double d = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, s);
            double invS = RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, d, out string? ukStr);
            Assert.Null(ukStr);
            Assert.True(Math.Abs(invS - s) <= 1e-6, $"US size {s} recovered {invS}");

            double roundtripD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, invS);
            Assert.True(Math.Abs(roundtripD - d) <= StrictRoundtripToleranceMm,
                $"US size {s} roundtrip diameter delta exceeds {StrictRoundtripToleranceMm} mm");
        }
    }

    [Fact]
    public void UkSizing_AllLettersAndExtendedWithHalfSizes_RoundtripWithinStrictTolerance()
    {
        // 26 standard letters A-Z + 6 extended Z+1 to Z+6, each with whole and half sizes
        var sizesToTest = new List<string>();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            sizesToTest.Add(c.ToString());
            sizesToTest.Add($"{c} 1/2");
        }
        for (int ext = 1; ext <= 6; ext++)
        {
            sizesToTest.Add($"Z+{ext}");
            sizesToTest.Add($"Z+{ext} 1/2");
        }

        foreach (var ukStr in sizesToTest)
        {
            double d = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukStr);
            double k = RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, d, out string? formattedLabel);

            Assert.NotNull(formattedLabel);
            Assert.Equal(ukStr.Replace(" ", ""), formattedLabel.Replace(" ", ""));

            double roundtripD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, formattedLabel);
            double deltaMm = Math.Abs(roundtripD - d);
            Assert.True(deltaMm <= StrictRoundtripToleranceMm,
                $"UK size '{ukStr}' roundtrip diameter drift {deltaMm:E4} exceeds {StrictRoundtripToleranceMm} mm");
        }
    }

    [Fact]
    public void EuIsoSizing_QuarterMillimeterIncrements_RoundtripWithinStrictTolerance()
    {
        // Test 40.0 to 76.0 in 0.25 mm increments (145 sizes)
        for (double c = 40.0; c <= 76.001; c += 0.25)
        {
            double d = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, c);
            double invC = RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, d, out string? ukStr);
            Assert.Null(ukStr);
            Assert.True(Math.Abs(invC - c) <= 1e-6, $"EU size {c} recovered {invC}");

            double roundtripD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, invC);
            Assert.True(Math.Abs(roundtripD - d) <= StrictRoundtripToleranceMm,
                $"EU size {c} roundtrip diameter delta exceeds {StrictRoundtripToleranceMm} mm");
        }
    }

    [Fact]
    public void JpSizing_HalfSizes_RoundtripWithinStrictTolerance()
    {
        // Test 1.0 to 30.0 in 0.5 increments (59 sizes)
        for (double s = 1.0; s <= 30.001; s += 0.5)
        {
            double d = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, s);
            double invS = RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, d, out string? ukStr);
            Assert.Null(ukStr);
            Assert.True(Math.Abs(invS - s) <= 1e-6, $"JP size {s} recovered {invS}");

            double roundtripD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, invS);
            Assert.True(Math.Abs(roundtripD - d) <= StrictRoundtripToleranceMm,
                $"JP size {s} roundtrip diameter delta exceeds {StrictRoundtripToleranceMm} mm");
        }
    }

    [Fact]
    public void ContinuousArbitraryDiameters_RoundtripAcrossAllFourStandards()
    {
        // 500 arbitrary diameters between 11.0 mm and 25.0 mm
        var rand = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            double d = 11.0 + (rand.NextDouble() * 14.0);

            // US roundtrip
            double usSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, d, out _);
            double usD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, usSize);
            Assert.True(Math.Abs(usD - d) <= StrictRoundtripToleranceMm);

            // UK numerical k roundtrip
            double ukK = RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, d, out _);
            double ukD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, ukK);
            Assert.True(Math.Abs(ukD - d) <= StrictRoundtripToleranceMm);

            // EU roundtrip
            double euSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, d, out _);
            double euD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, euSize);
            Assert.True(Math.Abs(euD - d) <= StrictRoundtripToleranceMm);

            // JP roundtrip
            double jpSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, d, out _);
            double jpD = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, jpSize);
            Assert.True(Math.Abs(jpD - d) <= StrictRoundtripToleranceMm);
        }
    }

    #endregion

    #region 3. Negative Testing & Adversarial Input Stress

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.001)]
    [InlineData(-10.0)]
    [InlineData(-1000.0)]
    public void NegativeOrZeroInnerDiameter_ThrowsArgumentOutOfRangeException(double invalidDiameter)
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
            RailEngine.CreateRingRailCurve3D(invalidDiameter));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRail(invalidDiameter));
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("XYZ")]
    [InlineData("Z+")]
    [InlineData("Z+abc")]
    [InlineData("A+1")]
    [InlineData("A 1/3")]
    [InlineData("A 3/4")]
    [InlineData("M quarter")]
    [InlineData("!@#$%")]
    [InlineData("A 1/2 extra")]
    public void MalformedUkStrings_ThrowArgumentException(string malformedInput)
    {
        Assert.Throws<ArgumentException>(() =>
            RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, malformedInput));
    }

    [Theory]
    [InlineData((RingSizingStandard)999)]
    [InlineData((RingSizingStandard)(-1))]
    public void InvalidRingSizingStandardEnum_ThrowsArgumentOutOfRangeException(RingSizingStandard invalidStandard)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.SizeToInnerDiameterMm(invalidStandard, 7.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.InnerDiameterMmToSize(invalidStandard, 17.32, out _));
    }

    [Theory]
    [InlineData((RailProfileType)999)]
    [InlineData((RailProfileType)(-1))]
    public void CreateRingRailCurve3D_InvalidProfileType_ThrowsArgumentOutOfRangeException(RailProfileType invalidType)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(17.32, invalidType));
    }

    #endregion

    #region 4. Defect Reproducer Tests (Adversarial Findings)

    [Theory]
    [InlineData("Z+-1")]
    [InlineData("Z+-2")]
    public void Defect_ParseUkSizeStringToK_AcceptsNegativeExtendedSizes(string negativeExtStr)
    {
        // Bug reproduction: "Z+-1" should throw ArgumentException because extended UK sizes
        // are strictly positive integers (Z+1 to Z+6). Currently int.TryParse parses "-1"
        // and returns k = 24.0 (Size Y).
        // This test asserts the correct specification behavior (must throw ArgumentException).
        Assert.Throws<ArgumentException>(() =>
            RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, negativeExtStr));
    }

    [Fact]
    public void Defect_CushionSquircle_TangentAtCardinalCrownAngle_DegeneratesToZero()
    {
        // Bug reproduction: At s = 0 (theta = 0, the top crown of the rail where the stone sits),
        // CushionSquircleCurve3D.TangentAtArcLength(0) produces dx = 0 and dy = 0 because of
        // erroneous multiplication by Math.Sign(sin(0)) == 0.
        // It normalizes to Vector3D.Zero (length 0.0) instead of unit vector (0, 1, 0).
        double diameter = 17.32;
        var squircle = new RailEngine.CushionSquircleCurve3D(diameter, 3.0);

        Vector3D t0 = squircle.TangentAtArcLength(0.0);
        Vector3D n0 = squircle.NormalAtArcLength(0.0);

        Assert.True(t0.Length > 0.5,
            $"Cushion squircle tangent at s=0 degenerates to zero vector {t0} (length {t0.Length})");
        Assert.True(n0.Length > 0.5,
            $"Cushion squircle normal at s=0 degenerates to zero vector {n0} (length {n0.Length})");

        // Attempting to create an OrthonormalFrame at the crown throws ArgumentException!
        var frame = OrthonormalFrame.Create(t0, n0);
        Assert.True(frame.IsOrthonormal());
    }

    [Theory]
    [InlineData((RailProfileType)999)]
    [InlineData((RailProfileType)(-1))]
    public void Defect_CreateRingRail_HeadlessMode_SilentlyReturnsNullForInvalidEnum(RailProfileType invalidType)
    {
        // Bug reproduction: CreateRingRail checks innerDiameterMm <= 0, but short-circuits
        // with "if (!IsRhinoNativeAvailable) return null;" BEFORE validating profileType.
        // In headless mode, invalid profileType returns null instead of throwing ArgumentOutOfRangeException.
        // Specification requirement: Invalid profile parameter must throw ArgumentOutOfRangeException.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRail(17.32, invalidType));
    }

    #endregion

    #region 5. Rail Profile Parameter Adversarial Stress

    [Theory]
    [InlineData(0.0)]         // cornerRadius 0 -> falls back to 0.35 * r
    [InlineData(-5.0)]        // negative cornerRadius -> falls back to 0.35 * r
    [InlineData(50.0)]        // excessive cornerRadius (> 0.8 * r) -> falls back to 0.35 * r
    public void EuroShank_BoundaryCornerRadii_ProduceValidNormalizedCurve(double cornerRadius)
    {
        double diameter = 17.32;
        var euro = new RailEngine.EuroShankCurve3D(diameter, cornerRadius);

        Assert.True(euro.IsClosed);
        double targetPerimeter = Math.PI * diameter;
        Assert.True(Math.Abs(euro.Length - targetPerimeter) <= StrictRoundtripToleranceMm,
            $"EuroShank length {euro.Length} must equal target {targetPerimeter} within {StrictRoundtripToleranceMm}");

        // Sample points and verify orthonormality and continuity
        const int samples = 64;
        for (int i = 0; i < samples; i++)
        {
            double s = (i * euro.Length) / samples;
            Point3D pt = euro.PointAtArcLength(s);
            Assert.False(double.IsNaN(pt.X) || double.IsNaN(pt.Y) || double.IsNaN(pt.Z));

            Vector3D t = euro.TangentAtArcLength(s);
            Vector3D n = euro.NormalAtArcLength(s);

            Assert.True(Math.Abs(t.Length - 1.0) <= 1e-4, "Tangent must be unit vector.");
            Assert.True(Math.Abs(n.Length - 1.0) <= 1e-4, "Normal must be unit vector.");
            Assert.True(Math.Abs(Vector3D.Dot(t, n)) <= 1e-4, "Tangent and normal must be orthogonal.");
        }
    }

    [Theory]
    [InlineData(0.1)]         // exponent <= 0.5 -> falls back to 3.0
    [InlineData(1.0)]         // exponent 1.0 (rhombus / squircle diamond)
    [InlineData(2.0)]         // exponent 2.0 (canonical circle)
    [InlineData(5.0)]         // exponent 5.0 (sharper rounded square)
    [InlineData(10.0)]        // exponent 10.0 (near-box)
    public void CushionSquircle_ExtremeExponents_ProduceNormalizedLengthAndFinitePoints(double exponent)
    {
        double diameter = 17.32;
        var squircle = new RailEngine.CushionSquircleCurve3D(diameter, exponent);

        Assert.True(squircle.IsClosed);
        double targetPerimeter = Math.PI * diameter;
        Assert.True(Math.Abs(squircle.Length - targetPerimeter) <= StrictRoundtripToleranceMm,
            $"Squircle length {squircle.Length} must equal target {targetPerimeter} within {StrictRoundtripToleranceMm}");

        // Verify all evaluated points are finite
        const int samples = 64;
        for (int i = 0; i < samples; i++)
        {
            double s = (i * squircle.Length) / samples;
            Point3D pt = squircle.PointAtArcLength(s);
            Assert.False(double.IsNaN(pt.X) || double.IsNaN(pt.Y) || double.IsNaN(pt.Z));
        }
    }

    [Theory]
    [InlineData(5.0, 0.0, 0.0)]       // below clamp minimums (20 deg, 1.5 mm, 1.0 mm)
    [InlineData(180.0, 10.0, 5.0)]    // above overlap max (clamped to 90 deg)
    [InlineData(-30.0, -2.0, -1.0)]   // negative parameters (clamped to minimums)
    public void BypassOverlap_ExtremeParameters_ClampedCleanlyAndMaintainsSeparation(
        double overlapDeg,
        double axialOffsetMm,
        double radialOffsetMm)
    {
        double diameter = 17.32;
        var bypass = new RailEngine.BypassOverlapCurve3D(diameter, overlapDeg, axialOffsetMm, radialOffsetMm);

        Assert.False(bypass.IsClosed);
        Assert.True(bypass.OverlapDegrees >= 20.0 && bypass.OverlapDegrees <= 90.0);
        Assert.True(bypass.AxialOffsetMm >= 1.5);
        Assert.True(bypass.RadialOffsetMm >= 1.0);

        Point3D startPt = bypass.PointAtArcLength(0.0);
        Point3D endPt = bypass.PointAtArcLength(bypass.Length);

        double deltaZ = Math.Abs(endPt.Z - startPt.Z);
        Assert.True(deltaZ >= 1.45, $"Axial separation {deltaZ:F3} must meet >= 1.5 mm specification.");

        // Verify pairwise separation along crossover to ensure NO self-intersection
        const int samples = 60;
        var pts = new List<Point3D>();
        for (int i = 0; i <= samples; i++)
        {
            double s = (i * bypass.Length) / samples;
            pts.Add(bypass.PointAtArcLength(s));
        }

        // Verify that distinct segments (far apart in index) do not self-intersect
        for (int i = 0; i < pts.Count; i++)
        {
            for (int j = i + 10; j < pts.Count; j++)
            {
                double dist = pts[i].DistanceTo(pts[j]);
                Assert.True(dist > 0.05,
                    $"Bypass curve self-intersection detected between indices {i} and {j}: distance = {dist:F4} mm");
            }
        }
    }

    #endregion

    #region 6. RingSizeInfo Consistency Stress

    [Fact]
    public void RingSizeInfo_AllBoundarySizes_MaintainGeometricConsistency()
    {
        var boundaries = new (RingSizingStandard standard, double size, string? ukStr)[]
        {
            (RingSizingStandard.US, 0.0, null),
            (RingSizingStandard.US, 16.0, null),
            (RingSizingStandard.UK, 0.0, "A"),
            (RingSizingStandard.UK, 0.0, "Z+6"),
            (RingSizingStandard.EU, 40.0, null),
            (RingSizingStandard.EU, 76.0, null),
            (RingSizingStandard.JP, 1.0, null),
            (RingSizingStandard.JP, 30.0, null)
        };

        foreach (var b in boundaries)
        {
            RingSizeInfo info = RailEngine.GetSizeInfo(b.standard, b.size, b.ukStr);

            Assert.Equal(b.standard, info.Standard);
            Assert.True(info.InnerDiameterMm > 0);
            Assert.Equal(info.InnerDiameterMm / 2.0, info.RadiusMm, 4);
            Assert.Equal(Math.PI * info.InnerDiameterMm, info.InnerCircumferenceMm, 4);
            Assert.False(string.IsNullOrWhiteSpace(info.SizeLabel));

            // Verify GetSizeInfoFromDiameter consistency
            RingSizeInfo derived = RailEngine.GetSizeInfoFromDiameter(info.InnerDiameterMm);
            Assert.Equal(info.InnerDiameterMm, derived.InnerDiameterMm, 4);
            Assert.Equal(info.InnerCircumferenceMm, derived.InnerCircumferenceMm, 4);
        }
    }

    #endregion
}
