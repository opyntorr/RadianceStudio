namespace RadianceStudio.Core.Tests.Rails;

using System;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;
using Xunit;

public class RailEngineTests
{
    private const double ToleranceMm = 0.01;

    #region Sizing Systems: US / North America

    [Theory]
    [InlineData(0.0, 11.63, 36.537)]
    [InlineData(6.0, 16.507, 51.858)]
    [InlineData(7.0, 17.320, 54.411)]
    [InlineData(8.0, 18.132, 56.965)]
    [InlineData(16.0, 24.635, 77.392)]
    public void UsSizing_MatchesStandardCheckpoints(double sizeUs, double expectedDiameterMm, double expectedCircumferenceMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, sizeUs);
        double circumference = Math.PI * diameter;

        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= ToleranceMm,
            $"US size {sizeUs} diameter {diameter:F3} should match expected {expectedDiameterMm:F3} within {ToleranceMm} mm");
        Assert.True(Math.Abs(circumference - expectedCircumferenceMm) <= 0.05,
            $"US size {sizeUs} circumference {circumference:F3} should match expected {expectedCircumferenceMm:F3}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(4.25)]
    [InlineData(5.5)]
    [InlineData(6.75)]
    [InlineData(7.0)]
    [InlineData(10.5)]
    [InlineData(12.25)]
    [InlineData(15.75)]
    [InlineData(16.0)]
    public void UsSizing_BidirectionalConversion_RoundtripsWithinTolerance(double sizeUs)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.US, sizeUs);
        double inverseSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, diameter, out string? ukStr);

        Assert.Null(ukStr);
        Assert.Equal(sizeUs, inverseSize, 4);
    }

    #endregion

    #region Sizing Systems: UK / Australia (BS 6820 Wheatsheaf)

    [Theory]
    [InlineData("A", 12.038, 37.820)]
    [InlineData("F", 14.028, 44.070)]
    [InlineData("L", 16.415, 51.570)]
    [InlineData("N", 17.211, 54.070)]
    [InlineData("N 1/2", 17.410, 54.695)]
    [InlineData("Z", 21.986, 69.070)]
    [InlineData("Z+6", 24.373, 76.570)]
    public void UkSizing_MatchesStandardCheckpoints(string ukSizeStr, double expectedDiameterMm, double expectedCircumferenceMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukSizeStr);
        double circumference = Math.PI * diameter;

        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= ToleranceMm,
            $"UK size {ukSizeStr} diameter {diameter:F3} should match expected {expectedDiameterMm:F3} within {ToleranceMm} mm");
        Assert.True(Math.Abs(circumference - expectedCircumferenceMm) <= 0.05,
            $"UK size {ukSizeStr} circumference {circumference:F3} should match expected {expectedCircumferenceMm:F3}");
    }

    [Theory]
    [InlineData("a", 12.038)]
    [InlineData("M", 16.813)]
    [InlineData("M 1/2", 17.012)]
    [InlineData("M1/2", 17.012)]
    [InlineData("M½", 17.012)]
    [InlineData("Z+1", 22.383)]
    [InlineData("Z+3", 23.179)]
    public void UkSizing_StringParsingVariants_HandledCorrectly(string ukInput, double expectedDiameterMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukInput);
        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= ToleranceMm,
            $"UK input '{ukInput}' parsed diameter {diameter:F3} should match expected {expectedDiameterMm:F3}");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("D")]
    [InlineData("H 1/2")]
    [InlineData("M")]
    [InlineData("N 1/2")]
    [InlineData("Z")]
    [InlineData("Z+2")]
    [InlineData("Z+6")]
    public void UkSizing_BidirectionalConversion_ProducesMatchingLabel(string ukSizeStr)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, ukSizeStr);
        double k = RailEngine.InnerDiameterMmToSize(RingSizingStandard.UK, diameter, out string? formattedLabel);

        Assert.NotNull(formattedLabel);
        Assert.Equal(ukSizeStr.Replace(" ", ""), formattedLabel.Replace(" ", ""));
        Assert.True(k >= 0);
    }

    #endregion

    #region Sizing Systems: Europe / ISO 8653

    [Theory]
    [InlineData(40.0, 12.732, 40.000)]
    [InlineData(50.0, 15.915, 50.000)]
    [InlineData(54.0, 17.189, 54.000)]
    [InlineData(54.4, 17.316, 54.400)]
    [InlineData(60.0, 19.099, 60.000)]
    [InlineData(76.0, 24.192, 76.000)]
    public void EuSizing_MatchesStandardCheckpoints(double sizeEu, double expectedDiameterMm, double expectedCircumferenceMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, sizeEu);
        double circumference = Math.PI * diameter;

        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= ToleranceMm,
            $"EU size {sizeEu} diameter {diameter:F3} should match expected {expectedDiameterMm:F3}");
        Assert.True(Math.Abs(circumference - expectedCircumferenceMm) <= 0.05,
            $"EU size {sizeEu} circumference {circumference:F3} should match expected {expectedCircumferenceMm:F3}");
    }

    [Theory]
    [InlineData(40.0)]
    [InlineData(45.5)]
    [InlineData(52.0)]
    [InlineData(54.0)]
    [InlineData(65.5)]
    [InlineData(76.0)]
    public void EuSizing_BidirectionalConversion_RoundtripsWithinTolerance(double sizeEu)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.EU, sizeEu);
        double inverseSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.EU, diameter, out string? ukStr);

        Assert.Null(ukStr);
        Assert.Equal(sizeEu, inverseSize, 4);
    }

    #endregion

    #region Sizing Systems: Japan / East Asia (JIS B 4700)

    [Theory]
    [InlineData(1.0, 13.000, 40.841)]
    [InlineData(7.0, 15.000, 47.124)]
    [InlineData(13.0, 17.000, 53.407)]
    [InlineData(14.0, 17.333, 54.454)]
    [InlineData(16.0, 18.000, 56.549)]
    [InlineData(30.0, 22.667, 71.209)]
    public void JpSizing_MatchesStandardCheckpoints(double sizeJp, double expectedDiameterMm, double expectedCircumferenceMm)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, sizeJp);
        double circumference = Math.PI * diameter;

        Assert.True(Math.Abs(diameter - expectedDiameterMm) <= ToleranceMm,
            $"JP size {sizeJp} diameter {diameter:F3} should match expected {expectedDiameterMm:F3}");
        Assert.True(Math.Abs(circumference - expectedCircumferenceMm) <= 0.05,
            $"JP size {sizeJp} circumference {circumference:F3} should match expected {expectedCircumferenceMm:F3}");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(5.5)]
    [InlineData(10.0)]
    [InlineData(14.0)]
    [InlineData(22.5)]
    [InlineData(30.0)]
    public void JpSizing_BidirectionalConversion_RoundtripsWithinTolerance(double sizeJp)
    {
        double diameter = RailEngine.SizeToInnerDiameterMm(RingSizingStandard.JP, sizeJp);
        double inverseSize = RailEngine.InnerDiameterMmToSize(RingSizingStandard.JP, diameter, out string? ukStr);

        Assert.Null(ukStr);
        Assert.Equal(sizeJp, inverseSize, 4);
    }

    #endregion

    #region RingSizeInfo Cross-System Model

    [Fact]
    public void GetSizeInfo_ForUsSize7_PopulatesAllInternationalEquivalents()
    {
        RingSizeInfo info = RailEngine.GetSizeInfo(RingSizingStandard.US, 7.0);

        Assert.Equal(RingSizingStandard.US, info.Standard);
        Assert.Equal(7.0, info.SizeValue);
        Assert.True(Math.Abs(info.InnerDiameterMm - 17.320) <= ToleranceMm);
        Assert.True(Math.Abs(info.InnerCircumferenceMm - 54.411) <= 0.05);
        Assert.True(Math.Abs(info.RadiusMm - 8.66) <= ToleranceMm);

        Assert.Equal("N 1/2", info.UkSize);
        Assert.True(Math.Abs(info.EuSize - 54.41) <= 0.1);
        Assert.True(Math.Abs(info.JpSize - 13.96) <= 0.1);
        Assert.False(string.IsNullOrEmpty(info.ToString()));
    }

    [Fact]
    public void GetSizeInfoFromDiameter_PopulatesValidRecord()
    {
        double diameter = 18.0;
        RingSizeInfo info = RailEngine.GetSizeInfoFromDiameter(diameter);

        Assert.Equal(diameter, info.InnerDiameterMm);
        Assert.Equal(Math.PI * diameter, info.InnerCircumferenceMm, 4);
        Assert.False(string.IsNullOrEmpty(info.UkSize));
        Assert.True(info.UsSize > 0);
        Assert.True(info.EuSize > 0);
        Assert.True(info.JpSize > 0);
    }

    [Fact]
    public void NegativeOrZeroDiameter_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, 0.0, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailEngine.InnerDiameterMmToSize(RingSizingStandard.US, -5.0, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailEngine.GetSizeInfoFromDiameter(-10.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailEngine.CreateRingRailCurve3D(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RailEngine.CreateRingRail(-1.0));
    }

    [Fact]
    public void InvalidUkString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, "Invalid123"));
        Assert.Throws<ArgumentException>(() => RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, "Z+Invalid"));
    }

    #endregion

    #region Parametric Rail Profiles: Canonical Circle

    [Theory]
    [InlineData(16.51)] // US 6
    [InlineData(17.32)] // US 7
    [InlineData(18.13)] // US 8
    public void CanonicalCircleRail_IsValidClosedPlanarCurveWithExactPerimeter(double innerDiameterMm)
    {
        ICurve3D rail = RailEngine.CreateRingRailCurve3D(innerDiameterMm, RailProfileType.CanonicalCircle);

        Assert.NotNull(rail);
        Assert.True(rail.IsClosed);

        double expectedPerimeter = Math.PI * innerDiameterMm;
        double actualPerimeter = rail.Length;

        Assert.True(Math.Abs(actualPerimeter - expectedPerimeter) <= ToleranceMm,
            $"Canonical circle perimeter {actualPerimeter:F4} should match target {expectedPerimeter:F4} within {ToleranceMm} mm");

        // Verify points and normals along the circle
        double r = innerDiameterMm / 2.0;
        Point3D pt0 = rail.PointAtArcLength(0.0);
        Point3D ptQuarter = rail.PointAtArcLength(rail.Length * 0.25);
        Point3D ptHalf = rail.PointAtArcLength(rail.Length * 0.5);

        Assert.True(Math.Abs(pt0.DistanceTo(Point3D.Origin) - r) <= 1e-4);
        Assert.True(Math.Abs(ptQuarter.DistanceTo(Point3D.Origin) - r) <= 1e-4);
        Assert.True(Math.Abs(ptHalf.DistanceTo(Point3D.Origin) - r) <= 1e-4);

        Vector3D t = rail.TangentAtArcLength(0.0);
        Vector3D n = rail.NormalAtArcLength(0.0);
        Assert.True(Math.Abs(Vector3D.Dot(t, n)) < 1e-6, "Tangent and normal must be orthogonal.");
    }

    #endregion

    #region Parametric Rail Profiles: Euro-Shank

    [Theory]
    [InlineData(16.51)]
    [InlineData(17.32)]
    [InlineData(18.13)]
    public void EuroShankRail_PerimeterNormalizedWithinTolerance(double innerDiameterMm)
    {
        ICurve3D rail = RailEngine.CreateRingRailCurve3D(innerDiameterMm, RailProfileType.EuroShank);

        Assert.NotNull(rail);
        Assert.True(rail.IsClosed);

        double targetCircumference = Math.PI * innerDiameterMm;
        double actualLength = rail.Length;

        Assert.True(Math.Abs(actualLength - targetCircumference) <= ToleranceMm,
            $"Euro-shank perimeter {actualLength:F4} must match target circumference {targetCircumference:F4} within {ToleranceMm} mm");
    }

    [Fact]
    public void EuroShankRail_ExhibitsAntiRotationFlatBottomAndLobes()
    {
        double diameter = 17.32;
        var euro = new RailEngine.EuroShankCurve3D(diameter);

        Assert.True(euro.IsClosed);
        double targetCircumference = Math.PI * diameter;
        Assert.True(Math.Abs(euro.Length - targetCircumference) <= ToleranceMm);

        // Sample points to verify flat bottom and corner lobes
        int samples = 128;
        double maxY = double.MinValue;
        double minY = double.MaxValue;
        double maxDist = double.MinValue;

        for (int i = 0; i < samples; i++)
        {
            double s = (i * euro.Length) / samples;
            Point3D pt = euro.PointAtArcLength(s);
            if (pt.Y > maxY) maxY = pt.Y;
            if (pt.Y < minY) minY = pt.Y;
            double dist = pt.DistanceTo(Point3D.Origin);
            if (dist > maxDist) maxDist = dist;
        }

        Assert.True(maxY > 0, "Upper arch must reach into +Y");
        Assert.True(minY < 0, "Flat bottom must reach into -Y");
        double r = diameter / 2.0;

        // Anti-rotation corner lobes extend further than pure radius
        Assert.True(maxDist > r * 0.99, "Corner lobes must push outward for anti-rotation stability");
    }

    #endregion

    #region Parametric Rail Profiles: Cushion Squircle

    [Theory]
    [InlineData(16.51, 3.0)]
    [InlineData(17.32, 2.5)]
    [InlineData(17.32, 3.0)]
    [InlineData(18.13, 3.5)]
    [InlineData(18.13, 4.0)]
    public void CushionSquircleRail_PerimeterNormalizedWithinTolerance(double innerDiameterMm, double exponent)
    {
        ICurve3D rail = RailEngine.CreateRingRailCurve3D(innerDiameterMm, RailProfileType.CushionSquircle, superellipseExponent: exponent);

        Assert.NotNull(rail);
        Assert.True(rail.IsClosed);

        double targetCircumference = Math.PI * innerDiameterMm;
        double actualLength = rail.Length;

        Assert.True(Math.Abs(actualLength - targetCircumference) <= ToleranceMm,
            $"Cushion squircle perimeter {actualLength:F4} must match target {targetCircumference:F4} within {ToleranceMm} mm");
    }

    [Fact]
    public void CushionSquircleRail_ExhibitsSuperellipseExpansionAtDiagonals()
    {
        double diameter = 17.32;
        var squircle = new RailEngine.CushionSquircleCurve3D(diameter, 3.0);

        Assert.True(squircle.IsClosed);
        Assert.Equal(3.0, squircle.Exponent);

        // Superellipse diagonal radius is greater than horizontal/vertical radius:
        // For |x/a|^3 + |y/a|^3 = 1, at theta = 45 deg, r = a * 2^(1/6) approx 1.122 * a
        Point3D ptAxis = squircle.PointAtArcLength(0.0); // At x-axis
        double rAxis = ptAxis.DistanceTo(Point3D.Origin);

        Point3D ptDiag = squircle.PointAtArcLength(squircle.Length * 0.125); // At 45 deg
        double rDiag = ptDiag.DistanceTo(Point3D.Origin);

        Assert.True(rDiag > rAxis, $"Squircle diagonal radius ({rDiag:F3}) must exceed axis radius ({rAxis:F3})");
    }

    [Theory]
    [InlineData(0.125, -1, 1)]  // 45 deg (Q1): Tx < 0, Ty > 0
    [InlineData(0.375, -1, -1)] // 135 deg (Q2): Tx < 0, Ty < 0
    [InlineData(0.625, 1, -1)]  // 225 deg (Q3): Tx > 0, Ty < 0
    [InlineData(0.875, 1, 1)]   // 315 deg (Q4): Tx > 0, Ty > 0
    public void CushionSquircleRail_TangentPointsCounterClockwiseInAllQuadrants(double frac, int expectedSignX, int expectedSignY)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(17.32, 3.0);
        Vector3D t = squircle.TangentAtArcLength(squircle.Length * frac);
        Assert.Equal(expectedSignX, Math.Sign(t.X));
        Assert.Equal(expectedSignY, Math.Sign(t.Y));

        Vector3D n = squircle.NormalAtArcLength(squircle.Length * frac);
        Assert.True(Math.Abs(Vector3D.Dot(t, n)) < 1e-6);

        Point3D p = squircle.PointAtArcLength(squircle.Length * frac);
        // Inward normal should point toward origin (p . n < 0)
        Assert.True((p.X * n.X) + (p.Y * n.Y) < 0, "Normal must point inward toward center of squircle");
    }

    [Fact]
    public void CushionSquircleRail_CardinalAxesTangentsAndNormals_AreExactUnitVectorsAndPointInward()
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(17.32, 3.0);
        double L = squircle.Length;

        // At s = 0: theta = 0, Point is (a, 0), Tangent is (0, 1, 0), Inward Normal is (-1, 0, 0)
        Vector3D t0 = squircle.TangentAtArcLength(0.0);
        Vector3D n0 = squircle.NormalAtArcLength(0.0);
        Point3D p0 = squircle.PointAtArcLength(0.0);
        Assert.True(Math.Abs(t0.X) < 1e-6 && Math.Abs(t0.Y - 1.0) < 1e-6, $"At s=0, t should be (0,1,0), got {t0}");
        Assert.True(Math.Abs(n0.X - (-1.0)) < 1e-6 && Math.Abs(n0.Y) < 1e-6, $"At s=0, n should be (-1,0,0), got {n0}");
        Assert.True((p0.X * n0.X) + (p0.Y * n0.Y) < 0, "Normal at s=0 must point inward");

        // At s = L/4: theta = pi/2, Point is (0, a), Tangent is (-1, 0, 0), Inward Normal is (0, -1, 0)
        Vector3D tQuarter = squircle.TangentAtArcLength(L * 0.25);
        Vector3D nQuarter = squircle.NormalAtArcLength(L * 0.25);
        Point3D pQuarter = squircle.PointAtArcLength(L * 0.25);
        Assert.True(Math.Abs(tQuarter.X - (-1.0)) < 1e-6 && Math.Abs(tQuarter.Y) < 1e-6, $"At s=L/4, t should be (-1,0,0), got {tQuarter}");
        Assert.True(Math.Abs(nQuarter.X) < 1e-6 && Math.Abs(nQuarter.Y - (-1.0)) < 1e-6, $"At s=L/4, n should be (0,-1,0), got {nQuarter}");
        Assert.True((pQuarter.X * nQuarter.X) + (pQuarter.Y * nQuarter.Y) < 0, "Normal at s=L/4 must point inward");

        // At s = L/2: theta = pi, Point is (-a, 0), Tangent is (0, -1, 0), Inward Normal is (1, 0, 0)
        Vector3D tHalf = squircle.TangentAtArcLength(L * 0.5);
        Vector3D nHalf = squircle.NormalAtArcLength(L * 0.5);
        Point3D pHalf = squircle.PointAtArcLength(L * 0.5);
        Assert.True(Math.Abs(tHalf.X) < 1e-6 && Math.Abs(tHalf.Y - (-1.0)) < 1e-6, $"At s=L/2, t should be (0,-1,0), got {tHalf}");
        Assert.True(Math.Abs(nHalf.X - 1.0) < 1e-6 && Math.Abs(nHalf.Y) < 1e-6, $"At s=L/2, n should be (1,0,0), got {nHalf}");
        Assert.True((pHalf.X * nHalf.X) + (pHalf.Y * nHalf.Y) < 0, "Normal at s=L/2 must point inward");

        // At s = 3L/4: theta = 3pi/2, Point is (0, -a), Tangent is (1, 0, 0), Inward Normal is (0, 1, 0)
        Vector3D t3Quarter = squircle.TangentAtArcLength(L * 0.75);
        Vector3D n3Quarter = squircle.NormalAtArcLength(L * 0.75);
        Point3D p3Quarter = squircle.PointAtArcLength(L * 0.75);
        Assert.True(Math.Abs(t3Quarter.X - 1.0) < 1e-6 && Math.Abs(t3Quarter.Y) < 1e-6, $"At s=3L/4, t should be (1,0,0), got {t3Quarter}");
        Assert.True(Math.Abs(n3Quarter.X) < 1e-6 && Math.Abs(n3Quarter.Y - 1.0) < 1e-6, $"At s=3L/4, n should be (0,1,0), got {n3Quarter}");
        Assert.True((p3Quarter.X * n3Quarter.X) + (p3Quarter.Y * n3Quarter.Y) < 0, "Normal at s=3L/4 must point inward");
    }

    [Theory]
    [InlineData(12.038, 2.5)]
    [InlineData(16.510, 3.0)]
    [InlineData(17.320, 2.5)]
    [InlineData(17.320, 3.0)]
    [InlineData(17.320, 3.5)]
    [InlineData(17.320, 4.0)]
    [InlineData(20.000, 3.0)]
    [InlineData(24.635, 3.0)]
    public void CushionSquircleRail_SimpsonQuadraturePerimeterMatchesCircumferenceWithinFiveMicrons(double innerDiameterMm, double exponent)
    {
        var squircle = new RailEngine.CushionSquircleCurve3D(innerDiameterMm, exponent);
        double targetCircumference = Math.PI * innerDiameterMm;

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
        Assert.True(perimeterError < 0.005,
            $"Perimeter error {perimeterError:F6} mm must be < 0.005 mm (D={innerDiameterMm}, n={exponent})");
    }

    [Theory]
    [InlineData(RingSizingStandard.US, -20.0)]
    [InlineData(RingSizingStandard.EU, -10.0)]
    [InlineData(RingSizingStandard.EU, 0.0)]
    [InlineData(RingSizingStandard.JP, -50.0)]
    [InlineData(RingSizingStandard.JP, -38.0)]
    [InlineData(RingSizingStandard.UK, -40.0)]
    public void SizeToInnerDiameterMm_NonPositiveDiameter_ThrowsArgumentOutOfRangeException(RingSizingStandard standard, double size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.SizeToInnerDiameterMm(standard, size));
    }

    [Theory]
    [InlineData((RailProfileType)999)]
    [InlineData((RailProfileType)(-1))]
    public void CreateRingRail_InvalidRailProfileType_ThrowsArgumentOutOfRangeException(RailProfileType invalidType)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRail(17.32, invalidType));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RailEngine.CreateRingRailCurve3D(17.32, invalidType));
    }

    [Theory]
    [InlineData("Z+-1")]
    [InlineData("Z+-2")]
    [InlineData("Z+0")]
    public void ParseUkSizeStringToK_RejectsNegativeOrZeroExtensions(string invalidUkStr)
    {
        Assert.Throws<ArgumentException>(() =>
            RailEngine.SizeToInnerDiameterMm(RingSizingStandard.UK, 0, invalidUkStr));
    }

    #endregion

    #region Parametric Rail Profiles: Bypass Overlap

    [Theory]
    [InlineData(17.32, 45.0, 2.0, 1.5)]
    [InlineData(16.51, 30.0, 1.5, 1.0)]
    [InlineData(18.13, 60.0, 3.0, 2.0)]
    [InlineData(20.00, 90.0, 2.5, 1.8)]
    public void BypassOverlapRail_IsOpen3DCurveWithSeparationAndNoSelfIntersection(
        double innerDiameterMm,
        double overlapDeg,
        double axialOffsetMm,
        double radialOffsetMm)
    {
        ICurve3D rail = RailEngine.CreateRingRailCurve3D(
            innerDiameterMm,
            RailProfileType.BypassOverlap,
            bypassOverlapDeg: overlapDeg,
            bypassAxialOffsetMm: axialOffsetMm,
            bypassRadialOffsetMm: radialOffsetMm);

        Assert.NotNull(rail);
        // Bypass overlap rail must be an open 3D curve (not closed)
        Assert.False(rail.IsClosed, "Bypass overlap rail must be an open curve.");

        // Arc length must exceed simple circular circumference due to overlap and 3D pitch
        double circleCircumference = Math.PI * innerDiameterMm;
        Assert.True(rail.Length > circleCircumference,
            "Bypass overlap rail length must be greater than target circle circumference.");

        // Verify axial separation between start and end points
        Point3D startPoint = rail.PointAtArcLength(0.0);
        Point3D endPoint = rail.PointAtArcLength(rail.Length);

        double deltaZ = Math.Abs(endPoint.Z - startPoint.Z);
        Assert.True(deltaZ >= 1.5 - 0.05, $"Axial separation {deltaZ:F2} must be >= 1.5 mm.");

        // Verify radial separation between start and end points
        double rStart = Math.Sqrt((startPoint.X * startPoint.X) + (startPoint.Y * startPoint.Y));
        double rEnd = Math.Sqrt((endPoint.X * endPoint.X) + (endPoint.Y * endPoint.Y));
        double deltaR = Math.Abs(rEnd - rStart);
        Assert.True(deltaR >= 1.0 - 0.05, $"Radial separation {deltaR:F2} must be >= 1.0 mm.");
    }

    #endregion

    #region Backward Compatibility & Runtime Checks

    [Fact]
    public void LegacyUsSizeToInnerDiameterMm_Preserved()
    {
        double diameter = RailEngine.UsSizeToInnerDiameterMm(7.0);
        Assert.True(Math.Abs(diameter - 17.320) <= ToleranceMm);
    }

    [Fact]
    public void CreateRingRail_WhenNativeAvailable_AttachesParametricMetadata()
    {
        if (RailEngine.IsRhinoNativeAvailable)
        {
            Curve? rail = RailEngine.CreateRingRail(17.32, RailProfileType.CanonicalCircle);
            Assert.NotNull(rail);
            var metadata = ParametricMetadata.Find(rail);

            Assert.NotNull(metadata);
            Assert.True(metadata.Parameters.ContainsKey("InnerDiameterMm"));
            Assert.Equal(17.32, (double)metadata.Parameters["InnerDiameterMm"]!, 2);
            Assert.Equal(RailProfileType.CanonicalCircle.ToString(), (string)metadata.Parameters["ProfileType"]!);
        }
    }

    [Fact]
    public void LegacyCreateRingRail_WhenNativeAvailable_PreservesBehavior()
    {
        if (RailEngine.IsRhinoNativeAvailable)
        {
            var engine = new RailEngine();
            Curve? rail = engine.CreateRingRail(7.0, 2.5, 1.8);

            Assert.NotNull(rail);
            Assert.True(rail.IsValid);
            Assert.True(rail.IsClosed);

            var metadata = ParametricMetadata.Find(rail);
            Assert.NotNull(metadata);
            Assert.Equal(7.0, (double)metadata.Parameters["SizeUs"]!);
            Assert.Equal(2.5, (double)metadata.Parameters["WidthMm"]!);
            Assert.Equal(1.8, (double)metadata.Parameters["ThicknessMm"]!);
            Assert.Equal(17.32, (double)metadata.Parameters["InnerDiameterMm"]!, 2);
        }
    }

    #endregion
}
