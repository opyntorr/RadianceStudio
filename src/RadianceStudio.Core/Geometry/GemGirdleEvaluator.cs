namespace RadianceStudio.Core.Geometry;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;

/// <summary>
/// Provides exact procedural 2D girdle perimeter outline and normal evaluation for all 13 canonical gem cut types.
/// </summary>
public static class GemGirdleEvaluator
{
    /// <summary>
    /// Evaluates the 2D point (Z=0) on the gem girdle perimeter at polar angle phi (in radians).
    /// </summary>
    public static Point3D EvaluateGirdlePoint(GemCutType cutType, GemDimensions dim, double phi)
    {
        double d = dim.DiameterMm;
        double a = d * 0.5;
        double aspect = dim.AspectRatio;
        double b = a * aspect;

        // Wrap phi into [0, 2*PI)
        phi = NormalizeAngle(phi);

        switch (cutType)
        {
            case GemCutType.RoundBrilliant:
                return new Point3D(a * Math.Cos(phi), a * Math.Sin(phi), 0.0);

            case GemCutType.Princess:
            {
                double denom = Math.Max(Math.Abs(Math.Cos(phi)), Math.Abs(Math.Sin(phi)));
                if (denom < 1e-12) denom = 1e-12;
                double r = a / denom;
                double maxCorner = a * Math.Sqrt(2.0) * 0.98;
                if (r > maxCorner) r = maxCorner;
                return new Point3D(r * Math.Cos(phi), r * Math.Sin(phi), 0.0);
            }

            case GemCutType.Oval:
                return new Point3D(a * Math.Cos(phi), b * Math.Sin(phi), 0.0);

            case GemCutType.Pear:
            {
                double sinVal = Math.Sin(phi);
                double cosVal = Math.Cos(phi);
                double y = b * sinVal;
                double widthFactor = sinVal <= 0 ? 1.0 : (1.0 - 0.75 * sinVal);
                double x = a * cosVal * widthFactor;
                return new Point3D(x, y, 0.0);
            }

            case GemCutType.Emerald:
            {
                double c = d * 0.06;
                var poly = CreateOctagon(a, b, c);
                return IntersectRayWithPolygon(poly, phi);
            }

            case GemCutType.Marquise:
            {
                double rArc = (a * a + b * b) / (2.0 * a);
                double xCenter = rArc - a;
                double y = b * Math.Sin(phi);
                double cosVal = Math.Cos(phi);
                double sign = cosVal >= 0 ? 1.0 : -1.0;
                double disc = Math.Max(0.0, rArc * rArc - y * y);
                double x = sign * (-xCenter + Math.Sqrt(disc));
                return new Point3D(x, y, 0.0);
            }

            case GemCutType.Baguette:
            {
                var poly = new Point3D[]
                {
                    new( a,  b, 0),
                    new(-a,  b, 0),
                    new(-a, -b, 0),
                    new( a, -b, 0)
                };
                return IntersectRayWithPolygon(poly, phi);
            }

            case GemCutType.Cushion:
            {
                const double power = 2.0 / 2.5; // 0.8
                double cosVal = Math.Cos(phi);
                double sinVal = Math.Sin(phi);
                double sgnX = cosVal >= 0 ? 1.0 : -1.0;
                double sgnY = sinVal >= 0 ? 1.0 : -1.0;
                double x = a * sgnX * Math.Pow(Math.Abs(cosVal), power);
                double y = b * sgnY * Math.Pow(Math.Abs(sinVal), power);
                return new Point3D(x, y, 0.0);
            }

            case GemCutType.Asscher:
            {
                double c = d * 0.15;
                var poly = CreateOctagon(a, a, c);
                return IntersectRayWithPolygon(poly, phi);
            }

            case GemCutType.Radiant:
            {
                double c = d * 0.08;
                var poly = CreateOctagon(a, b, c);
                return IntersectRayWithPolygon(poly, phi);
            }

            case GemCutType.TaperedBaguette:
            {
                double aNarrow = a * 0.70;
                var poly = new Point3D[]
                {
                    new( aNarrow,  b, 0),
                    new(-aNarrow,  b, 0),
                    new(-a,       -b, 0),
                    new( a,       -b, 0)
                };
                return IntersectRayWithPolygon(poly, phi);
            }

            case GemCutType.Heart:
            {
                return EvaluateHeart(d, aspect, phi);
            }

            case GemCutType.Trilliant:
            {
                return EvaluateTrilliant(d, phi);
            }

            default:
                return new Point3D(a * Math.Cos(phi), a * Math.Sin(phi), 0.0);
        }
    }

    /// <summary>
    /// Evaluates the outward unit normal vector on the girdle perimeter at polar angle phi.
    /// </summary>
    public static Vector3D EvaluateGirdleNormal(GemCutType cutType, GemDimensions dim, double phi)
    {
        const double eps = 1e-4;
        var pPlus = EvaluateGirdlePoint(cutType, dim, phi + eps);
        var pMinus = EvaluateGirdlePoint(cutType, dim, phi - eps);

        var tangent = new Vector3D(pPlus.X - pMinus.X, pPlus.Y - pMinus.Y, 0.0);
        if (tangent.Length < 1e-12)
        {
            return new Vector3D(Math.Cos(phi), Math.Sin(phi), 0.0);
        }

        tangent = tangent.Normalize();
        // Outward normal in counter-clockwise 2D curve is (Ty, -Tx)
        var normal = new Vector3D(tangent.Y, -tangent.X, 0.0);
        var p0 = EvaluateGirdlePoint(cutType, dim, phi);
        if (Vector3D.Dot(normal, new Vector3D(p0.X, p0.Y, 0.0)) < 0)
        {
            normal = -normal;
        }

        return normal;
    }

    /// <summary>
    /// Samples N ordered points along the closed girdle perimeter.
    /// </summary>
    public static IReadOnlyList<Point3D> SampleGirdleOutline(GemCutType cutType, GemDimensions dim, int sampleCount = 64)
    {
        if (sampleCount < 4) sampleCount = 4;
        var points = new Point3D[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            double phi = (2.0 * Math.PI * i) / sampleCount;
            points[i] = EvaluateGirdlePoint(cutType, dim, phi);
        }
        return points;
    }

    /// <summary>
    /// Computes the total perimeter length of the gem's girdle outline.
    /// </summary>
    public static double ComputePerimeterLength(GemCutType cutType, GemDimensions dim, int sampleCount = 128)
    {
        var pts = SampleGirdleOutline(cutType, dim, sampleCount);
        double len = 0.0;
        for (int i = 0; i < pts.Count; i++)
        {
            var p0 = pts[i];
            var p1 = pts[(i + 1) % pts.Count];
            len += (p1 - p0).Length;
        }
        return len;
    }

    private static double NormalizeAngle(double rad)
    {
        const double twoPi = 2.0 * Math.PI;
        rad %= twoPi;
        if (rad < 0) rad += twoPi;
        return rad;
    }

    private static Point3D[] CreateOctagon(double a, double b, double c)
    {
        return new Point3D[8]
        {
            new( a,      b - c, 0),
            new( a - c,  b,     0),
            new(-a + c,  b,     0),
            new(-a,      b - c, 0),
            new(-a,     -b + c, 0),
            new(-a + c, -b,     0),
            new( a - c, -b,     0),
            new( a,     -b + c, 0)
        };
    }

    private static Point3D IntersectRayWithPolygon(Point3D[] poly, double phi)
    {
        double dx = Math.Cos(phi);
        double dy = Math.Sin(phi);
        int n = poly.Length;

        for (int i = 0; i < n; i++)
        {
            var p0 = poly[i];
            var p1 = poly[(i + 1) % n];

            double ex = p1.X - p0.X;
            double ey = p1.Y - p0.Y;

            double det = dx * ey - dy * ex;
            if (Math.Abs(det) < 1e-12) continue;

            double u = (p0.X * dy - p0.Y * dx) / det;
            double t = (p0.X * ey - p0.Y * ex) / det;

            if (t > 1e-9 && u >= -1e-7 && u <= 1.0000001)
            {
                return new Point3D(t * dx, t * dy, 0.0);
            }
        }

        // Fallback to bounding box distance
        return new Point3D(poly[0].X * Math.Cos(phi), poly[0].Y * Math.Sin(phi), 0.0);
    }

    private static Point3D EvaluateHeart(double diameterMm, double aspect, double phi)
    {
        double t = (Math.PI * 0.5) - phi;
        while (t < 0.0) t += 2.0 * Math.PI;
        while (t >= 2.0 * Math.PI) t -= 2.0 * Math.PI;

        double sinT = Math.Sin(t);
        double cosT = Math.Cos(t);
        double cos2T = Math.Cos(2.0 * t);
        double cos3T = Math.Cos(3.0 * t);
        double cos4T = Math.Cos(4.0 * t);

        double rawX = 16.0 * sinT * sinT * sinT;
        double rawY = 13.0 * cosT - 5.0 * cos2T - 2.0 * cos3T - cos4T;

        // Canonical heart bounds: X in [-16, 16] (span 32), Y in [-17, 12] (span 29)
        double scaleX = diameterMm / 32.0;
        double scaleY = (diameterMm * aspect) / 29.0;
        double offsetY = -0.5 * (-17.0 + 12.0) * scaleY;

        return new Point3D(rawX * scaleX, (rawY * scaleY) + offsetY, 0.0);
    }

    private static Point3D EvaluateTrilliant(double diameterMm, double phi)
    {
        const double beta = 0.22;
        double angleDiff = 3.0 * (phi - (Math.PI * 0.5));
        double r = 1.0 / (1.0 - beta * Math.Cos(angleDiff));

        double rawX = r * Math.Cos(phi);
        double rawY = r * Math.Sin(phi);

        // Normalize to diameterMm
        double span = 1.0 / (1.0 - beta); // ~1.282
        double factor = (diameterMm * 0.5) / span;

        return new Point3D(rawX * factor, rawY * factor, 0.0);
    }
}
