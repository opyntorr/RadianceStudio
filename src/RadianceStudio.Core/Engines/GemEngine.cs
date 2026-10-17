namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;

/// <summary>
/// High-performance parametric engine for distributing gems along 3D space curves and evaluating orthonormal frames.
/// </summary>
public class GemEngine
{
    /// <summary>
    /// Distributes gems along a curve referenced by its Rhino object Guid.
    /// Evaluates tangent and normal frames and generates combined scale and orientation Transform matrices.
    /// </summary>
    public List<Transform> DistributeGemsOnCurve(Guid curveId, double gemDiameterMm, double spacingMm)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            return new List<Transform>();
        }

        var obj = doc.Objects.FindId(curveId);
        if (obj?.Geometry is not Curve curve)
        {
            return new List<Transform>();
        }

        return DistributeGemsOnCurve(curve, gemDiameterMm, spacingMm);
    }

    /// <summary>
    /// Distributes gems along a 3D curve using arc-length division and Gram-Schmidt orthonormalization.
    /// Returns 4x4 affine Transform matrices from canonical WorldXY (1.0 mm) space to local target frames.
    /// </summary>
    public List<Transform> DistributeGemsOnCurve(Curve curve, double gemDiameterMm, double spacingMm, double startOffsetMm = 0.0, double endOffsetMm = 0.0)
    {
        var transforms = new List<Transform>();
        if (curve == null || gemDiameterMm <= 0.0)
        {
            return transforms;
        }

        double totalLength = curve.GetLength();
        double usableLength = totalLength - (startOffsetMm + endOffsetMm);
        double step = gemDiameterMm + spacingMm;

        if (usableLength < gemDiameterMm || step <= 0.0)
        {
            return transforms;
        }

        int stoneCount = (int)Math.Floor((usableLength - gemDiameterMm) / step) + 1;
        if (stoneCount <= 0)
        {
            return transforms;
        }

        for (int i = 0; i < stoneCount; i++)
        {
            double s = startOffsetMm + (gemDiameterMm / 2.0) + (i * step);
            if (!curve.LengthParameter(s, out double t))
            {
                continue;
            }

            var point = curve.PointAt(t);
            var tangent = curve.TangentAt(t);
            tangent.Unitize();

            // Evaluate curvature / normal vector
            var curvature = curve.CurvatureAt(t);
            var normal = curvature;
            if (normal.Length < 1e-9)
            {
                // Fallback normal orthogonal to tangent
                normal = Vector3d.CrossProduct(tangent, Vector3d.ZAxis);
                if (normal.Length < 1e-9)
                {
                    normal = Vector3d.CrossProduct(tangent, Vector3d.YAxis);
                }
            }
            normal.Unitize();

            // Orthonormal basis via cross product (Gram-Schmidt)
            var binormal = Vector3d.CrossProduct(tangent, normal);
            binormal.Unitize();
            tangent = Vector3d.CrossProduct(normal, binormal);
            tangent.Unitize();

            // Target plane with Z-axis pointing outward along the normal (table up)
            var localPlane = new Plane(point, binormal, tangent);

            // Compute affine matrix: PlaneToPlane * Uniform Scale
            var planeToPlane = Transform.PlaneToPlane(Plane.WorldXY, localPlane);
            var scale = Transform.Scale(Plane.WorldXY, gemDiameterMm, gemDiameterMm, gemDiameterMm);
            var combined = planeToPlane * scale;

            transforms.Add(combined);
        }

        return transforms;
    }
}
