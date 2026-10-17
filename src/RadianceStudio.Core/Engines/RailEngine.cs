namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using System.Globalization;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric engine for calculating international standard ring rails and generating specialized profiles.
/// Supports US, UK/AU, EU/ISO 8653, and JP/East Asia sizing standards with numerical accuracy &lt;= 0.01 mm,
/// and procedural rail profiles: Canonical Circle, Euro-Shank, Cushion Squircle, and 3D Bypass Overlap.
/// Provides both RhinoCommon Curve generation (when running in Rhino) and headless ICurve3D curve generation.
/// </summary>
public class RailEngine
{
    private static bool? _isRhinoNativeAvailable;

    /// <summary>
    /// Indicates whether RhinoCommon's unmanaged C++ runtime (rhcommon_c.dll) is available in the current process.
    /// In headless test runners without Rhino.exe hosting, this returns false to safely avoid native DLL crashes.
    /// </summary>
    public static bool IsRhinoNativeAvailable
    {
        get
        {
            if (!_isRhinoNativeAvailable.HasValue)
            {
                try
                {
                    var pt = new Point3d(0, 0, 0);
                    var c = new Circle(Plane.WorldXY, pt, 1.0);
                    _isRhinoNativeAvailable = c.ToNurbsCurve() != null;
                }
                catch
                {
                    _isRhinoNativeAvailable = false;
                }
            }
            return _isRhinoNativeAvailable.Value;
        }
    }

    #region International Sizing Conversions

    /// <summary>
    /// Converts a ring size in the specified international standard to the corresponding inner diameter in millimeters.
    /// </summary>
    /// <param name="standard">The international sizing standard (US, UK, EU, JP).</param>
    /// <param name="sizeValue">The numerical size value.</param>
    /// <param name="ukSizeStr">Optional UK Wheatsheaf size string (e.g. "A", "M 1/2", "Z+2") when querying UK standard.</param>
    /// <returns>Inner diameter in millimeters with precision &lt;= 0.01 mm.</returns>
    public static double SizeToInnerDiameterMm(RingSizingStandard standard, double sizeValue, string? ukSizeStr = null)
    {
        double diameter;
        switch (standard)
        {
            case RingSizingStandard.US:
                // Standard US formula: D = 11.63 + 0.8128 * S
                diameter = 11.63 + (sizeValue * 0.8128);
                break;

            case RingSizingStandard.UK:
                // UK BS 6820 Wheatsheaf scale: C = 37.82 + 1.25 * k, D = C / PI
                double k;
                if (!string.IsNullOrWhiteSpace(ukSizeStr))
                {
                    k = ParseUkSizeStringToK(ukSizeStr);
                }
                else
                {
                    k = sizeValue;
                }
                double cUk = 37.82 + (1.25 * k);
                diameter = cUk / Math.PI;
                break;

            case RingSizingStandard.EU:
                // European / ISO 8653: Size is the inner circumference in mm. D = S / PI
                diameter = sizeValue / Math.PI;
                break;

            case RingSizingStandard.JP:
                // Japan JIS B 4700: D = 13.00 + (S - 1) / 3 = (38 + S) / 3
                diameter = (38.0 + sizeValue) / 3.0;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(standard), standard, "Unsupported ring sizing standard.");
        }

        if (diameter <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeValue), sizeValue, "Ring size produces a non-positive diameter.");
        }

        return diameter;
    }

    /// <summary>
    /// Converts an inner diameter in millimeters to the size value in the specified international standard.
    /// </summary>
    /// <param name="standard">The international sizing standard.</param>
    /// <param name="innerDiameterMm">The inner diameter in millimeters.</param>
    /// <param name="ukSizeStr">Outputs the UK size label if UK standard was requested (e.g. "N 1/2", "Z+2"), otherwise null.</param>
    /// <returns>The numerical size value in the specified standard.</returns>
    public static double InnerDiameterMmToSize(RingSizingStandard standard, double innerDiameterMm, out string? ukSizeStr)
    {
        if (innerDiameterMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
        }

        switch (standard)
        {
            case RingSizingStandard.US:
                // Inverse US: S = (D - 11.63) / 0.8128
                ukSizeStr = null;
                return (innerDiameterMm - 11.63) / 0.8128;

            case RingSizingStandard.UK:
                // Inverse UK: C = PI * D; k = (C - 37.82) / 1.25
                double cUk = Math.PI * innerDiameterMm;
                double k = (cUk - 37.82) / 1.25;
                ukSizeStr = FormatKToUkSizeString(k);
                return k;

            case RingSizingStandard.EU:
                // Inverse EU: S = PI * D
                ukSizeStr = null;
                return Math.PI * innerDiameterMm;

            case RingSizingStandard.JP:
                // Inverse JP: S = 3 * D - 38
                ukSizeStr = null;
                return (3.0 * innerDiameterMm) - 38.0;

            default:
                throw new ArgumentOutOfRangeException(nameof(standard), standard, "Unsupported ring sizing standard.");
        }
    }

    /// <summary>
    /// Backward-compatible helper calculating the inner diameter for a given US ring size.
    /// </summary>
    public static double UsSizeToInnerDiameterMm(double sizeUs)
    {
        return SizeToInnerDiameterMm(RingSizingStandard.US, sizeUs);
    }

    /// <summary>
    /// Retrieves a comprehensive RingSizeInfo record containing dimensions and cross-system equivalent values.
    /// </summary>
    public static RingSizeInfo GetSizeInfo(RingSizingStandard standard, double sizeValue, string? ukSizeStr = null)
    {
        double innerDiameterMm = SizeToInnerDiameterMm(standard, sizeValue, ukSizeStr);
        return BuildRingSizeInfo(standard, sizeValue, ukSizeStr, innerDiameterMm);
    }

    /// <summary>
    /// Retrieves a comprehensive RingSizeInfo record derived directly from inner diameter.
    /// </summary>
    public static RingSizeInfo GetSizeInfoFromDiameter(double innerDiameterMm)
    {
        if (innerDiameterMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
        }
        double usSize = (innerDiameterMm - 11.63) / 0.8128;
        return BuildRingSizeInfo(RingSizingStandard.US, usSize, null, innerDiameterMm);
    }

    #endregion

    #region Specialized Rail Profiles (Rhino Curve & ICurve3D)

    /// <summary>
    /// Generates a specialized parametric ring rail curve as a pure C# ICurve3D space curve.
    /// Fully functional in headless test runners and cross-platform environments without native dependencies.
    /// </summary>
    public static ICurve3D CreateRingRailCurve3D(
        double innerDiameterMm,
        RailProfileType profileType = RailProfileType.CanonicalCircle,
        double cornerRadius = 0,
        double superellipseExponent = 3.0,
        double bypassOverlapDeg = 45.0,
        double bypassAxialOffsetMm = 2.0,
        double bypassRadialOffsetMm = 1.5)
    {
        if (innerDiameterMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
        }

        if (!Enum.IsDefined(typeof(RailProfileType), profileType))
        {
            throw new ArgumentOutOfRangeException(nameof(profileType), profileType, "Unsupported rail profile type.");
        }

        return profileType switch
        {
            RailProfileType.CanonicalCircle => new CircleCurve3D(Point3D.Origin, innerDiameterMm / 2.0),
            RailProfileType.EuroShank => new EuroShankCurve3D(innerDiameterMm, cornerRadius),
            RailProfileType.CushionSquircle => new CushionSquircleCurve3D(innerDiameterMm, superellipseExponent),
            RailProfileType.BypassOverlap => new BypassOverlapCurve3D(innerDiameterMm, bypassOverlapDeg, bypassAxialOffsetMm, bypassRadialOffsetMm),
            _ => throw new ArgumentOutOfRangeException(nameof(profileType), profileType, "Unsupported rail profile type.")
        };
    }

    /// <summary>
    /// Generates a specialized parametric ring rail curve according to the specified profile type.
    /// When running inside Rhino, returns a Rhino.Geometry.Curve with attached ParametricMetadata.
    /// In headless test execution where native Rhino libraries are absent, safely returns null.
    /// </summary>
    public static Curve? CreateRingRail(
        double innerDiameterMm,
        RailProfileType profileType = RailProfileType.CanonicalCircle,
        double cornerRadius = 0,
        double superellipseExponent = 3.0,
        double bypassOverlapDeg = 45.0,
        double bypassAxialOffsetMm = 2.0,
        double bypassRadialOffsetMm = 1.5)
    {
        if (innerDiameterMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
        }

        if (!Enum.IsDefined(typeof(RailProfileType), profileType))
        {
            throw new ArgumentOutOfRangeException(nameof(profileType), profileType, "Unsupported rail profile type.");
        }

        if (!IsRhinoNativeAvailable)
        {
            return null;
        }

        Curve railCurve = profileType switch
        {
            RailProfileType.CanonicalCircle => CreateCanonicalCircleRail(innerDiameterMm),
            RailProfileType.EuroShank => CreateEuroShankRail(innerDiameterMm, cornerRadius),
            RailProfileType.CushionSquircle => CreateCushionSquircleRail(innerDiameterMm, superellipseExponent),
            RailProfileType.BypassOverlap => CreateBypassOverlapRail(innerDiameterMm, bypassOverlapDeg, bypassAxialOffsetMm, bypassRadialOffsetMm),
            _ => throw new ArgumentOutOfRangeException(nameof(profileType), profileType, "Unsupported rail profile type.")
        };

        var metadata = ParametricMetadata.Attach(railCurve);
        metadata.Parameters.Set("InnerDiameterMm", innerDiameterMm);
        metadata.Parameters.Set("ProfileType", profileType.ToString());
        metadata.Parameters.Set("CreatedUtc", DateTime.UtcNow.ToString("o"));

        return railCurve;
    }

    /// <summary>
    /// Backward-compatible instance method for generating a parametric circular ring rail
    /// centered at the origin on the XY plane with attached ParametricMetadata.
    /// </summary>
    public Curve? CreateRingRail(double sizeUs, double widthMm, double thicknessMm)
    {
        double innerDiameter = UsSizeToInnerDiameterMm(sizeUs);
        var curve = CreateRingRail(innerDiameter, RailProfileType.CanonicalCircle);

        if (curve != null)
        {
            var metadata = ParametricMetadata.Attach(curve);
            metadata.Parameters.Set("SizeUs", sizeUs);
            metadata.Parameters.Set("WidthMm", widthMm);
            metadata.Parameters.Set("ThicknessMm", thicknessMm);
            metadata.Parameters.Set("InnerDiameterMm", innerDiameter);
            metadata.Parameters.Set("CreatedUtc", DateTime.UtcNow.ToString("o"));
        }

        return curve;
    }

    #endregion

    #region Rhino Native Curve Builders

    private static Curve CreateCanonicalCircleRail(double innerDiameterMm)
    {
        double radius = innerDiameterMm / 2.0;
        var circle = new Circle(Plane.WorldXY, Point3d.Origin, radius);
        return circle.ToNurbsCurve();
    }

    private static Curve CreateEuroShankRail(double innerDiameterMm, double cornerRadius)
    {
        double r = innerDiameterMm / 2.0;
        double targetCircumference = Math.PI * innerDiameterMm;

        double rc = (cornerRadius > 0 && cornerRadius < 0.8 * r) ? cornerRadius : (0.35 * r);
        double yBottom = r;
        double yCorner = yBottom - rc;

        var topArc = new Arc(new Point3d(r, 0, 0), new Point3d(0, r, 0), new Point3d(-r, 0, 0));
        var leftLine = new Line(new Point3d(-r, 0, 0), new Point3d(-r, -yCorner, 0));

        double diag = rc * Math.Sqrt(0.5);
        var leftCornerMid = new Point3d(-r + rc - diag, -yCorner - diag, 0);
        var leftCornerArc = new Arc(new Point3d(-r, -yCorner, 0), leftCornerMid, new Point3d(-r + rc, -yBottom, 0));

        var bottomLine = new Line(new Point3d(-r + rc, -yBottom, 0), new Point3d(r - rc, -yBottom, 0));

        var rightCornerMid = new Point3d(r - rc + diag, -yCorner - diag, 0);
        var rightCornerArc = new Arc(new Point3d(r - rc, -yBottom, 0), rightCornerMid, new Point3d(r, -yCorner, 0));

        var rightLine = new Line(new Point3d(r, -yCorner, 0), new Point3d(r, 0, 0));

        var poly = new PolyCurve();
        poly.Append(new ArcCurve(topArc));
        poly.Append(new LineCurve(leftLine));
        poly.Append(new ArcCurve(leftCornerArc));
        poly.Append(new LineCurve(bottomLine));
        poly.Append(new ArcCurve(rightCornerArc));
        poly.Append(new LineCurve(rightLine));
        poly.MakeClosed(1e-4);

        Curve result = poly.ToNurbsCurve() ?? (Curve)poly;

        double unscaledLength = result.GetLength();
        if (unscaledLength > 1e-6)
        {
            double scale = targetCircumference / unscaledLength;
            result.Transform(Transform.Scale(Point3d.Origin, scale));
        }

        return result;
    }

    private static Curve CreateCushionSquircleRail(double innerDiameterMm, double exponent)
    {
        double r = innerDiameterMm / 2.0;
        double targetCircumference = Math.PI * innerDiameterMm;
        double n = exponent > 0.5 ? exponent : 3.0;

        const int sampleCount = 32;
        var points = new Point3d[sampleCount];
        double invN = 2.0 / n;

        for (int i = 0; i < sampleCount; i++)
        {
            double theta = (2.0 * Math.PI * i) / sampleCount;
            double cosT = Math.Cos(theta);
            double sinT = Math.Sin(theta);

            double signX = Math.Sign(cosT);
            double signY = Math.Sign(sinT);

            double x = r * signX * Math.Pow(Math.Abs(cosT), invN);
            double y = r * signY * Math.Pow(Math.Abs(sinT), invN);

            points[i] = new Point3d(x, y, 0);
        }

        var curve = Curve.CreateInterpolatedCurve(points, 3, CurveKnotStyle.ChordPeriodic);
        if (curve == null)
        {
            var polyline = new Polyline(points);
            polyline.Add(points[0]);
            curve = polyline.ToNurbsCurve();
        }

        double initialLength = curve.GetLength();
        if (initialLength > 1e-6)
        {
            double scale = targetCircumference / initialLength;
            curve.Transform(Transform.Scale(Point3d.Origin, scale));
        }

        return curve;
    }

    private static Curve CreateBypassOverlapRail(
        double innerDiameterMm,
        double bypassOverlapDeg,
        double bypassAxialOffsetMm,
        double bypassRadialOffsetMm)
    {
        double r = innerDiameterMm / 2.0;
        double overlapDeg = Math.Clamp(bypassOverlapDeg, 20.0, 90.0);
        double overlapRad = overlapDeg * Math.PI / 180.0;
        double dZ = Math.Max(bypassAxialOffsetMm, 1.5);
        double dR = Math.Max(bypassRadialOffsetMm, 1.0);

        double totalSpan = (2.0 * Math.PI) + overlapRad;
        double tStart = (Math.PI / 2.0) - (overlapRad / 2.0);

        const int pointCount = 96;
        var points = new Point3d[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            double u = (double)i / (pointCount - 1);
            double t = tStart + (u * totalSpan);

            double psi = (3.0 * u * u) - (2.0 * u * u * u);

            double currentRadius = r + (dR * (psi - 0.5));
            double currentZ = dZ * (psi - 0.5);

            double x = currentRadius * Math.Cos(t);
            double y = currentRadius * Math.Sin(t);
            double z = currentZ;

            points[i] = new Point3d(x, y, z);
        }

        var curve = Curve.CreateInterpolatedCurve(points, 3, CurveKnotStyle.Chord);
        if (curve == null)
        {
            var polyline = new Polyline(points);
            curve = polyline.ToNurbsCurve();
        }

        return curve;
    }

    #endregion

    #region Helper Methods

    private static double ParseUkSizeStringToK(string ukStr)
    {
        string s = ukStr.Trim().ToUpperInvariant();

        // Handle extended sizes "Z+1", "Z+2", etc.
        if (s.StartsWith("Z+"))
        {
            string remainder = s.Substring(2).Trim();
            bool hasHalf = remainder.Contains("1/2") || remainder.Contains("½") || remainder.Contains(".5");
            string numPart = remainder.Replace("1/2", string.Empty).Replace("½", string.Empty).Replace(".5", string.Empty).Trim();
            if (int.TryParse(numPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int extNum) && extNum >= 1)
            {
                return 25.0 + extNum + (hasHalf ? 0.5 : 0.0);
            }
            throw new ArgumentException($"Invalid extended UK ring size string: '{ukStr}'", nameof(ukStr));
        }

        // Handle standard letters A-Z, potentially with 1/2 or ½
        if (s.Length > 0 && s[0] >= 'A' && s[0] <= 'Z')
        {
            int baseK = s[0] - 'A';
            string remainder = s.Substring(1).Trim();
            if (string.IsNullOrEmpty(remainder))
            {
                return baseK;
            }

            if (remainder == "1/2" || remainder == "½" || remainder == ".5")
            {
                return baseK + 0.5;
            }

            throw new ArgumentException($"Invalid UK ring size modifier: '{ukStr}'", nameof(ukStr));
        }

        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double numericVal))
        {
            if (numericVal < 0)
            {
                throw new ArgumentException($"UK ring size cannot be negative: '{ukStr}'", nameof(ukStr));
            }
            return numericVal;
        }

        throw new ArgumentException($"Invalid UK ring size string: '{ukStr}'", nameof(ukStr));
    }

    private static string FormatKToUkSizeString(double k)
    {
        double roundedK = Math.Round(k * 2.0, MidpointRounding.AwayFromZero) / 2.0;
        if (roundedK < 0)
        {
            roundedK = 0;
        }

        int intPart = (int)Math.Floor(roundedK);
        bool hasHalf = Math.Abs(roundedK - intPart - 0.5) < 1e-4;

        if (intPart <= 25)
        {
            char letter = (char)('A' + intPart);
            return hasHalf ? $"{letter} 1/2" : $"{letter}";
        }
        else
        {
            int ext = intPart - 25;
            return hasHalf ? $"Z+{ext} 1/2" : $"Z+{ext}";
        }
    }

    private static RingSizeInfo BuildRingSizeInfo(RingSizingStandard standard, double sizeValue, string? ukSizeStr, double innerDiameterMm)
    {
        double circumference = Math.PI * innerDiameterMm;
        double usSize = (innerDiameterMm - 11.63) / 0.8128;
        InnerDiameterMmToSize(RingSizingStandard.UK, innerDiameterMm, out string? ukStr);
        double euSize = circumference;
        double jpSize = (3.0 * innerDiameterMm) - 38.0;

        string label = standard switch
        {
            RingSizingStandard.UK => ukSizeStr ?? ukStr ?? string.Empty,
            RingSizingStandard.US => FormatUsSize(sizeValue),
            RingSizingStandard.EU => sizeValue.ToString("F1", CultureInfo.InvariantCulture),
            RingSizingStandard.JP => sizeValue.ToString("F1", CultureInfo.InvariantCulture),
            _ => sizeValue.ToString(CultureInfo.InvariantCulture)
        };

        return new RingSizeInfo
        {
            Standard = standard,
            SizeValue = sizeValue,
            SizeLabel = label,
            InnerDiameterMm = innerDiameterMm,
            InnerCircumferenceMm = circumference,
            UsSize = usSize,
            UkSize = ukStr ?? string.Empty,
            EuSize = euSize,
            JpSize = jpSize
        };
    }

    private static string FormatUsSize(double sizeUs)
    {
        double quarter = Math.Round(sizeUs * 4.0, MidpointRounding.AwayFromZero) / 4.0;
        int whole = (int)Math.Floor(quarter);
        double frac = quarter - whole;

        if (Math.Abs(frac) < 1e-4)
        {
            return whole.ToString(CultureInfo.InvariantCulture);
        }
        if (Math.Abs(frac - 0.25) < 1e-4)
        {
            return $"{whole} 1/4";
        }
        if (Math.Abs(frac - 0.5) < 1e-4)
        {
            return $"{whole} 1/2";
        }
        if (Math.Abs(frac - 0.75) < 1e-4)
        {
            return $"{whole} 3/4";
        }

        return sizeUs.ToString("0.##", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Pure C# 3D Space Curve Implementations

    /// <summary>
    /// Parametric anti-rotation Euro-shank rail curve modeled from geometric first principles.
    /// Features a circular upper arch, corner lobes at 225 deg and 315 deg, flat bottom span,
    /// and normalized total perimeter within &lt;= 0.01 mm of finger circumference.
    /// </summary>
    public sealed class EuroShankCurve3D : ICurve3D
    {
        private readonly double _radius;
        private readonly double _targetCircumference;
        private readonly double _scale;
        private readonly double _l1, _l2, _l3, _l4, _l5, _l6;
        private readonly double _rc, _yc, _yb;

        public double Length => _targetCircumference;
        public bool IsClosed => true;
        public double Radius => _radius;

        public EuroShankCurve3D(double innerDiameterMm, double cornerRadius = 0)
        {
            if (innerDiameterMm <= 1e-6)
            {
                throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
            }

            _radius = innerDiameterMm / 2.0;
            _targetCircumference = Math.PI * innerDiameterMm;

            _rc = (cornerRadius > 0 && cornerRadius < 0.8 * _radius) ? cornerRadius : (0.35 * _radius);
            _yb = _radius;
            _yc = _yb - _rc;

            // Unscaled lengths of 6 piecewise G1 segments:
            double unscaledL1 = Math.PI * _radius;       // Upper semicircle
            double unscaledL2 = _radius - _rc;           // Left vertical segment
            double unscaledL3 = 0.5 * Math.PI * _rc;     // Left corner arc
            double unscaledL4 = 2.0 * (_radius - _rc);   // Bottom horizontal flat segment
            double unscaledL5 = 0.5 * Math.PI * _rc;     // Right corner arc
            double unscaledL6 = _radius - _rc;           // Right vertical segment
            double unscaledTotal = unscaledL1 + unscaledL2 + unscaledL3 + unscaledL4 + unscaledL5 + unscaledL6;

            _scale = _targetCircumference / unscaledTotal;
            _l1 = unscaledL1 * _scale;
            _l2 = unscaledL2 * _scale;
            _l3 = unscaledL3 * _scale;
            _l4 = unscaledL4 * _scale;
            _l5 = unscaledL5 * _scale;
            _l6 = unscaledL6 * _scale;
        }

        public Point3D PointAtArcLength(double s)
        {
            s = NormalizeS(s);

            if (s <= _l1)
            {
                // Segment 1: Upper semicircle from angle 0 to PI
                double theta = (s / _l1) * Math.PI;
                return new Point3D(_radius * _scale * Math.Cos(theta), _radius * _scale * Math.Sin(theta), 0.0);
            }
            s -= _l1;

            if (s <= _l2)
            {
                // Segment 2: Left vertical from (-r, 0) down to (-r, -yc)
                double t = s / _l2;
                return new Point3D(-_radius * _scale, -t * _yc * _scale, 0.0);
            }
            s -= _l2;

            if (s <= _l3)
            {
                // Segment 3: Left corner arc from angle PI to 1.5*PI
                double phi = Math.PI + ((s / _l3) * 0.5 * Math.PI);
                double cx = (-_radius + _rc) * _scale;
                double cy = -_yc * _scale;
                return new Point3D(cx + (_rc * _scale * Math.Cos(phi)), cy + (_rc * _scale * Math.Sin(phi)), 0.0);
            }
            s -= _l3;

            if (s <= _l4)
            {
                // Segment 4: Bottom horizontal flat from x = -r+rc to r-rc
                double t = s / _l4;
                double x = ((-_radius + _rc) + (t * 2.0 * (_radius - _rc))) * _scale;
                return new Point3D(x, -_yb * _scale, 0.0);
            }
            s -= _l4;

            if (s <= _l5)
            {
                // Segment 5: Right corner arc from angle 1.5*PI to 2*PI
                double phi = (1.5 * Math.PI) + ((s / _l5) * 0.5 * Math.PI);
                double cx = (_radius - _rc) * _scale;
                double cy = -_yc * _scale;
                return new Point3D(cx + (_rc * _scale * Math.Cos(phi)), cy + (_rc * _scale * Math.Sin(phi)), 0.0);
            }
            s -= _l5;

            // Segment 6: Right vertical from (r, -yc) up to (r, 0)
            double t6 = s / _l6;
            return new Point3D(_radius * _scale, (-_yc + (t6 * _yc)) * _scale, 0.0);
        }

        public Vector3D TangentAtArcLength(double s)
        {
            s = NormalizeS(s);

            if (s <= _l1)
            {
                double theta = (s / _l1) * Math.PI;
                return new Vector3D(-Math.Sin(theta), Math.Cos(theta), 0.0).Normalize();
            }
            s -= _l1;
            if (s <= _l2)
            {
                return new Vector3D(0.0, -1.0, 0.0);
            }
            s -= _l2;
            if (s <= _l3)
            {
                double phi = Math.PI + ((s / _l3) * 0.5 * Math.PI);
                return new Vector3D(-Math.Sin(phi), Math.Cos(phi), 0.0).Normalize();
            }
            s -= _l3;
            if (s <= _l4)
            {
                return new Vector3D(1.0, 0.0, 0.0);
            }
            s -= _l4;
            if (s <= _l5)
            {
                double phi = (1.5 * Math.PI) + ((s / _l5) * 0.5 * Math.PI);
                return new Vector3D(-Math.Sin(phi), Math.Cos(phi), 0.0).Normalize();
            }

            return new Vector3D(0.0, 1.0, 0.0);
        }

        public Vector3D NormalAtArcLength(double s)
        {
            var t = TangentAtArcLength(s);
            // Inward normal in XY plane
            return new Vector3D(-t.Y, t.X, 0.0).Normalize();
        }

        public double ParameterAtArcLength(double s) => NormalizeS(s) / _targetCircumference;

        private double NormalizeS(double s)
        {
            double rem = s % _targetCircumference;
            if (rem < 0) rem += _targetCircumference;
            return rem;
        }
    }

    /// <summary>
    /// Parametric soft squircle rail modeled via Lamé superellipse (|x/a|^n + |y/b|^n = 1).
    /// Perimeter-normalized to finger circumference within &lt; 0.005 mm using 2,048-point composite Simpson quadrature.
    /// Tangent and normal vectors analytically handled at cardinal axes and continuous across all 4 quadrants.
    /// </summary>
    public sealed class CushionSquircleCurve3D : ICurve3D
    {
        private readonly double _radius;
        private readonly double _targetCircumference;
        private readonly double _exponent;
        private readonly double _a;
        private const int TableSize = 2048;
        private readonly double[] _cumLengths = new double[TableSize + 1];

        public double Length => _targetCircumference;
        public bool IsClosed => true;
        public double Exponent => _exponent;
        public double SemiAxis => _a;

        public CushionSquircleCurve3D(double innerDiameterMm, double exponent = 3.0)
        {
            if (innerDiameterMm <= 1e-6)
            {
                throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be strictly positive.");
            }

            _radius = innerDiameterMm / 2.0;
            _targetCircumference = Math.PI * innerDiameterMm;
            _exponent = exponent > 0.5 ? exponent : 3.0;

            // 2,048-point composite Simpson's rule quadrature of unscaled superellipse perimeter.
            // Using non-singular transformation: ds/dy with respect to y in horizontal-dominant octants,
            // and ds/dx with respect to x in vertical-dominant octants.
            double unscaledLength = 0.0;
            _cumLengths[0] = 0.0;

            for (int i = 1; i <= TableSize; i++)
            {
                double t0 = (2.0 * Math.PI * (i - 1)) / TableSize;
                double t1 = (2.0 * Math.PI * i) / TableSize;

                double segLen = ComputeSegmentArcLength(t0, t1, _radius, _exponent);
                unscaledLength += segLen;
                _cumLengths[i] = unscaledLength;
            }

            double scale = _targetCircumference / unscaledLength;
            _a = _radius * scale;

            for (int i = 1; i <= TableSize; i++)
            {
                _cumLengths[i] *= scale;
            }
            _cumLengths[TableSize] = _targetCircumference;
        }

        private static double ComputeSegmentArcLength(double t0, double t1, double r, double n)
        {
            double tm = 0.5 * (t0 + t1);
            double cosM = Math.Abs(Math.Cos(tm));
            double sinM = Math.Abs(Math.Sin(tm));

            double invN = 2.0 / n;
            double p = 2.0 * (n - 1.0);

            if (cosM >= sinM)
            {
                double y0 = r * Math.Sign(Math.Sin(t0)) * Math.Pow(Math.Abs(Math.Sin(t0)), invN);
                double y1 = r * Math.Sign(Math.Sin(t1)) * Math.Pow(Math.Abs(Math.Sin(t1)), invN);
                double dy = y1 - y0;
                if (Math.Abs(dy) < 1e-15) return 0.0;

                double f(double yVal)
                {
                    double ay = Math.Abs(yVal);
                    if (ay < 1e-15) return 1.0;
                    double rN = Math.Pow(r, n);
                    double yN = Math.Pow(ay, n);
                    double diff = Math.Max(1e-15, rN - yN);
                    double xVal = Math.Pow(diff, 1.0 / n);
                    double ratio = ay / xVal;
                    return Math.Sqrt(1.0 + Math.Pow(ratio, p));
                }

                double h = dy / 4.0;
                double s = f(y0) + (4.0 * f(y0 + h)) + (2.0 * f(y0 + (2.0 * h))) + (4.0 * f(y0 + (3.0 * h))) + f(y1);
                return Math.Abs(s * h / 3.0);
            }
            else
            {
                double x0 = r * Math.Sign(Math.Cos(t0)) * Math.Pow(Math.Abs(Math.Cos(t0)), invN);
                double x1 = r * Math.Sign(Math.Cos(t1)) * Math.Pow(Math.Abs(Math.Cos(t1)), invN);
                double dx = x1 - x0;
                if (Math.Abs(dx) < 1e-15) return 0.0;

                double g(double xVal)
                {
                    double ax = Math.Abs(xVal);
                    if (ax < 1e-15) return 1.0;
                    double rN = Math.Pow(r, n);
                    double xN = Math.Pow(ax, n);
                    double diff = Math.Max(1e-15, rN - xN);
                    double yVal = Math.Pow(diff, 1.0 / n);
                    double ratio = ax / yVal;
                    return Math.Sqrt(1.0 + Math.Pow(ratio, p));
                }

                double h = dx / 4.0;
                double s = g(x0) + (4.0 * g(x0 + h)) + (2.0 * g(x0 + (2.0 * h))) + (4.0 * g(x0 + (3.0 * h))) + g(x1);
                return Math.Abs(s * h / 3.0);
            }
        }

        public Point3D PointAtArcLength(double s)
        {
            double theta = ThetaFromS(s);
            double invN = 2.0 / _exponent;

            double cos = Math.Cos(theta);
            double sin = Math.Sin(theta);

            double x = _a * Math.Sign(cos) * Math.Pow(Math.Abs(cos), invN);
            double y = _a * Math.Sign(sin) * Math.Pow(Math.Abs(sin), invN);

            return new Point3D(x, y, 0.0);
        }

        public Vector3D TangentAtArcLength(double s)
        {
            double theta = ThetaFromS(s);
            return EvaluateTangentAtTheta(theta);
        }

        public Vector3D NormalAtArcLength(double s)
        {
            var t = TangentAtArcLength(s);
            return new Vector3D(-t.Y, t.X, 0.0).Normalize();
        }

        public double ParameterAtArcLength(double s) => ThetaFromS(s) / (2.0 * Math.PI);

        private Vector3D EvaluateTangentAtTheta(double theta)
        {
            double twoPi = 2.0 * Math.PI;
            double tNorm = theta % twoPi;
            if (tNorm < 0) tNorm += twoPi;

            const double cardinalTol = 1e-7;

            // Cardinal axes analytic evaluation:
            // At theta = 0 (or 2pi): point is (a, 0), tangent is purely +Y: (0, 1, 0)
            if (tNorm < cardinalTol || tNorm > twoPi - cardinalTol)
            {
                return new Vector3D(0.0, 1.0, 0.0);
            }

            // At theta = pi/2: point is (0, a), tangent is purely -X: (-1, 0, 0)
            if (Math.Abs(tNorm - (Math.PI * 0.5)) < cardinalTol)
            {
                return new Vector3D(-1.0, 0.0, 0.0);
            }

            // At theta = pi: point is (-a, 0), tangent is purely -Y: (0, -1, 0)
            if (Math.Abs(tNorm - Math.PI) < cardinalTol)
            {
                return new Vector3D(0.0, -1.0, 0.0);
            }

            // At theta = 3pi/2: point is (0, -a), tangent is purely +X: (1, 0, 0)
            if (Math.Abs(tNorm - (Math.PI * 1.5)) < cardinalTol)
            {
                return new Vector3D(1.0, 0.0, 0.0);
            }

            // General non-cardinal angle:
            // x(theta) = a * sgn(cos) * |cos|^(2/n)
            // y(theta) = a * sgn(sin) * |sin|^(2/n)
            // dx/dtheta = -a * (2/n) * |cos|^(2/n - 1) * sin
            // dy/dtheta =  a * (2/n) * |sin|^(2/n - 1) * cos
            // Note: NO Math.Sign(cos) or Math.Sign(sin) factors!
            double invN = 2.0 / _exponent;
            double cos = Math.Cos(tNorm);
            double sin = Math.Sin(tNorm);
            double absCos = Math.Max(1e-12, Math.Abs(cos));
            double absSin = Math.Max(1e-12, Math.Abs(sin));

            double dx = -invN * Math.Pow(absCos, invN - 1.0) * sin;
            double dy = invN * Math.Pow(absSin, invN - 1.0) * cos;

            var tangent = new Vector3D(dx, dy, 0.0);
            if (tangent.LengthSquared < 1e-24)
            {
                return new Vector3D(-sin, cos, 0.0).Normalize();
            }

            return tangent.Normalize();
        }

        private double ThetaFromS(double s)
        {
            double rem = s % _targetCircumference;
            if (rem < 0) rem += _targetCircumference;

            // Binary search in cumulative arc lengths
            int low = 0;
            int high = TableSize;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (_cumLengths[mid] < rem) low = mid + 1;
                else high = mid - 1;
            }

            int idx = Math.Clamp(low - 1, 0, TableSize - 1);
            double s0 = _cumLengths[idx];
            double s1 = _cumLengths[idx + 1];
            double frac = (s1 > s0) ? ((rem - s0) / (s1 - s0)) : 0.0;

            double t0 = (2.0 * Math.PI * idx) / TableSize;
            double t1 = (2.0 * Math.PI * (idx + 1)) / TableSize;
            return t0 + (frac * (t1 - t0));
        }
    }

    /// <summary>
    /// Parametric 3D open bypass overlap crossover rail curve.
    /// Overlaps across angular span at the crown with vertical Z and radial clearances, without self-intersection.
    /// </summary>
    public sealed class BypassOverlapCurve3D : ICurve3D
    {
        private readonly double _radius;
        private readonly double _overlapDeg;
        private readonly double _overlapRad;
        private readonly double _dZ;
        private readonly double _dR;
        private readonly double _totalSpan;
        private readonly double _tStart;
        private readonly double _length;
        private const int StepCount = 128;
        private readonly double[] _cumLengths = new double[StepCount + 1];

        public double Length => _length;
        public bool IsClosed => false;
        public double OverlapDegrees => _overlapDeg;
        public double AxialOffsetMm => _dZ;
        public double RadialOffsetMm => _dR;

        public BypassOverlapCurve3D(
            double innerDiameterMm,
            double bypassOverlapDeg = 45.0,
            double bypassAxialOffsetMm = 2.0,
            double bypassRadialOffsetMm = 1.5)
        {
            if (innerDiameterMm <= 1e-6)
            {
                throw new ArgumentOutOfRangeException(nameof(innerDiameterMm), "Inner diameter must be positive.");
            }

            _radius = innerDiameterMm / 2.0;
            _overlapDeg = Math.Clamp(bypassOverlapDeg, 20.0, 90.0);
            _overlapRad = _overlapDeg * Math.PI / 180.0;
            _dZ = Math.Max(bypassAxialOffsetMm, 1.5);
            _dR = Math.Max(bypassRadialOffsetMm, 1.0);

            _totalSpan = (2.0 * Math.PI) + _overlapRad;
            _tStart = (Math.PI / 2.0) - (_overlapRad / 2.0);

            // Integrate 3D curve length
            double accLen = 0.0;
            _cumLengths[0] = 0.0;
            Point3D prevPt = EvaluatePointAtU(0.0);

            for (int i = 1; i <= StepCount; i++)
            {
                double u = (double)i / StepCount;
                Point3D curPt = EvaluatePointAtU(u);
                accLen += (curPt - prevPt).Length;
                _cumLengths[i] = accLen;
                prevPt = curPt;
            }

            _length = accLen;
        }

        public Point3D PointAtArcLength(double s)
        {
            double u = UFromS(s);
            return EvaluatePointAtU(u);
        }

        public Vector3D TangentAtArcLength(double s)
        {
            double u = UFromS(s);
            double t = _tStart + (u * _totalSpan);
            double psi = (3.0 * u * u) - (2.0 * u * u * u);
            double dpsi = 6.0 * u * (1.0 - u);

            double r = _radius + (_dR * (psi - 0.5));
            double dr = _dR * dpsi;
            double dz = _dZ * dpsi;

            double dx = (dr * Math.Cos(t)) - (r * _totalSpan * Math.Sin(t));
            double dy = (dr * Math.Sin(t)) + (r * _totalSpan * Math.Cos(t));

            return new Vector3D(dx, dy, dz).Normalize();
        }

        public Vector3D NormalAtArcLength(double s)
        {
            var t = TangentAtArcLength(s);
            // Frenet-like principal normal pointing inwards toward helix axis
            double u = UFromS(s);
            double theta = _tStart + (u * _totalSpan);
            var inward = new Vector3D(-Math.Cos(theta), -Math.Sin(theta), 0.0);
            return (inward - (t * Vector3D.Dot(inward, t))).Normalize();
        }

        public double ParameterAtArcLength(double s) => UFromS(s);

        private Point3D EvaluatePointAtU(double u)
        {
            u = Math.Clamp(u, 0.0, 1.0);
            double t = _tStart + (u * _totalSpan);
            double psi = (3.0 * u * u) - (2.0 * u * u * u);

            double currentRadius = _radius + (_dR * (psi - 0.5));
            double currentZ = _dZ * (psi - 0.5);

            double x = currentRadius * Math.Cos(t);
            double y = currentRadius * Math.Sin(t);
            double z = currentZ;

            return new Point3D(x, y, z);
        }

        private double UFromS(double s)
        {
            s = Math.Clamp(s, 0.0, _length);
            int low = 0;
            int high = StepCount;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (_cumLengths[mid] < s) low = mid + 1;
                else high = mid - 1;
            }

            int idx = Math.Clamp(low - 1, 0, StepCount - 1);
            double s0 = _cumLengths[idx];
            double s1 = _cumLengths[idx + 1];
            double frac = (s1 > s0) ? ((s - s0) / (s1 - s0)) : 0.0;

            return (idx + frac) / StepCount;
        }
    }

    #endregion
}
