namespace RadianceStudio.Core.Engines;

using System;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric engine for calculating and generating international standard ring rails and cross-section profiles.
/// </summary>
public class RailEngine
{
    /// <summary>
    /// Calculates the inner diameter in millimeters for a given US ring size.
    /// Standard formula: Diameter (mm) = 11.63 + (sizeUs * 0.8128)
    /// </summary>
    public static double UsSizeToInnerDiameterMm(double sizeUs)
    {
        return 11.63 + (sizeUs * 0.8128);
    }

    /// <summary>
    /// Generates a parametric circular ring rail centered at the origin on the XY plane
    /// with attached ParametricMetadata for persistent history.
    /// </summary>
    public Curve CreateRingRail(double sizeUs, double widthMm, double thicknessMm)
    {
        double innerDiameter = UsSizeToInnerDiameterMm(sizeUs);
        double radius = innerDiameter / 2.0;

        var circle = new Circle(Plane.WorldXY, Point3d.Origin, radius);
        var curve = circle.ToNurbsCurve();

        var metadata = ParametricMetadata.Attach(curve);
        metadata.Parameters.Set("SizeUs", sizeUs);
        metadata.Parameters.Set("WidthMm", widthMm);
        metadata.Parameters.Set("ThicknessMm", thicknessMm);
        metadata.Parameters.Set("InnerDiameterMm", innerDiameter);
        metadata.Parameters.Set("CreatedUtc", DateTime.UtcNow.ToString("o"));

        return curve;
    }
}
