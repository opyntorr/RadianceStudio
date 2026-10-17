namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric setting engine for procedural prong head generation.
/// Supports 4, 6, and 8 prong heads across all 13 canonical gem cut types,
/// featuring Princess corner alignment, V-prong chevrons, rounded dome / flat / claw tips,
/// and structural base / gallery wire rings.
/// Operates in dual mode: pure C# closed 2-manifold GemMesh and RhinoCommon Brep wrapper.
/// </summary>
public static class ProngEngine
{
    /// <summary>
    /// Generates a watertight closed 2-manifold mesh representation of the prong setting head.
    /// </summary>
    public static GemMesh CreateProngHeadMesh(GemDimensions centerGem, ProngParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        var vertices = new List<Point3D>();
        var faces = new List<int[]>();

        // 1. Determine radial angular positions for prongs
        var prongAngles = CalculateProngAngles(centerGem.CutType, centerGem, parameters.ProngCount);

        // 2. Geometry heights
        double d = centerGem.DiameterMm;
        double g = d * (centerGem.GirdleThicknessPercent / 100.0);
        double hp = d * (centerGem.PavilionDepthPercent / 100.0);
        double hc = d * (centerGem.CrownHeightPercent / 100.0);

        double zCulet = -((g * 0.5) + hp);
        double zCrown = (g * 0.5) + hc;
        double zBase = zCulet;

        double totalHeight = Math.Max(
            parameters.ProngHeightMm,
            Math.Abs(zBase) + (hc * 0.7) + parameters.GirdleOverhangMm);

        double zTop = zBase + totalHeight;
        double rakeRad = parameters.RakeAngleDeg * (Math.PI / 180.0);

        var baseWirePoints = new List<Point3D>(prongAngles.Count);
        var galleryWirePoints = new List<Point3D>(prongAngles.Count);

        // 3. Generate each prong mesh component
        for (int i = 0; i < prongAngles.Count; i++)
        {
            double angle = prongAngles[i];
            var pGirdle = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, angle);
            var normal = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, angle);

            double rProng = parameters.ProngDiameterMm * 0.5;
            double rTop = rProng * (1.0 - (parameters.TaperPercent / 100.0));

            // Overhang inward shift: center is placed so inner edge wraps over girdle by GirdleOverhangMm
            var centerGirdle = pGirdle + (normal * (rProng - parameters.GirdleOverhangMm));

            // Base shifted inward by rake angle
            double baseInwardOffset = Math.Abs(zBase) * Math.Tan(rakeRad);
            var centerBase = new Point3D(
                centerGirdle.X - (normal.X * baseInwardOffset),
                centerGirdle.Y - (normal.Y * baseInwardOffset),
                zBase);

            // Top center shifted slightly inward to clasp the crown facet
            var centerTop = new Point3D(
                centerGirdle.X - (normal.X * (parameters.GirdleOverhangMm * 0.5)),
                centerGirdle.Y - (normal.Y * (parameters.GirdleOverhangMm * 0.5)),
                zTop);

            baseWirePoints.Add(centerBase);
            galleryWirePoints.Add(new Point3D(
                (centerBase.X + centerGirdle.X) * 0.5,
                (centerBase.Y + centerGirdle.Y) * 0.5,
                (centerBase.Z + 0.0) * 0.5));

            bool isVProng = parameters.Style == ProngStyle.VProng && IsCornerOrTipProng(centerGem.CutType, angle);

            if (isVProng)
            {
                GenerateVProng(
                    centerGem.CutType,
                    angle,
                    centerBase,
                    centerTop,
                    parameters.ProngDiameterMm,
                    normal,
                    vertices,
                    faces);
            }
            else
            {
                GenerateProngCylinder(
                    centerBase,
                    centerTop,
                    rProng,
                    rTop,
                    parameters.Style,
                    normal,
                    vertices,
                    faces);
            }
        }

        // 4. Structural Base Wire ring
        if (parameters.IncludeBaseWire && baseWirePoints.Count >= 3)
        {
            GenerateWireRing(
                baseWirePoints,
                parameters.BaseWireDiameterMm * 0.5,
                vertices,
                faces);
        }

        // 5. Gallery Wire ring
        if (parameters.IncludeGalleryWire && galleryWirePoints.Count >= 3)
        {
            GenerateWireRing(
                galleryWirePoints,
                parameters.GalleryWireDiameterMm * 0.5,
                vertices,
                faces);
        }

        var normals = new Vector3D[faces.Count];
        for (int f = 0; f < faces.Count; f++)
        {
            normals[f] = GemMesh.ComputeFaceNormal(vertices, faces[f]);
        }

        return new GemMesh
        {
            CutType = centerGem.CutType,
            DiameterMm = centerGem.DiameterMm,
            Vertices = vertices,
            Faces = faces,
            Normals = normals
        };
    }

    /// <summary>
    /// Dual execution RhinoCommon Brep wrapper for prong head geometry.
    /// </summary>
    public static Brep? CreateProngHeadBrep(GemDimensions centerGem, ProngParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var prongAngles = CalculateProngAngles(centerGem.CutType, centerGem, parameters.ProngCount);
        double d = centerGem.DiameterMm;
        double g = d * (centerGem.GirdleThicknessPercent / 100.0);
        double hp = d * (centerGem.PavilionDepthPercent / 100.0);
        double hc = d * (centerGem.CrownHeightPercent / 100.0);

        double zBase = -((g * 0.5) + hp);
        double totalHeight = parameters.ProngHeightMm > 0.0
            ? parameters.ProngHeightMm
            : (Math.Abs(zBase) + (hc * 0.6) + parameters.GirdleOverhangMm);
        double zTop = zBase + totalHeight;
        double rakeRad = parameters.RakeAngleDeg * (Math.PI / 180.0);
        double rProng = parameters.ProngDiameterMm * 0.5;

        var solidBreps = new List<Brep>();

        for (int i = 0; i < prongAngles.Count; i++)
        {
            double angle = prongAngles[i];
            var pGirdle = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, angle);
            var normal = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, angle);

            var centerGirdle = pGirdle + (normal * (rProng - parameters.GirdleOverhangMm));
            double baseInwardOffset = Math.Abs(zBase) * Math.Tan(rakeRad);
            var centerBase = new Point3d(
                centerGirdle.X - (normal.X * baseInwardOffset),
                centerGirdle.Y - (normal.Y * baseInwardOffset),
                zBase);
            var centerTop = new Point3d(
                centerGirdle.X - (normal.X * (parameters.GirdleOverhangMm * 0.5)),
                centerGirdle.Y - (normal.Y * (parameters.GirdleOverhangMm * 0.5)),
                zTop);

            var line = new Line(centerBase, centerTop);
            var pipeBreps = Brep.CreatePipe(line.ToNurbsCurve(), rProng, false, PipeCapMode.Round, false, 0.001, 0.001);
            if (pipeBreps != null && pipeBreps.Length > 0)
            {
                solidBreps.Add(pipeBreps[0]);
            }
        }

        if (solidBreps.Count == 0) return null;

        Brep result = solidBreps[0];
        if (solidBreps.Count > 1)
        {
            var union = Brep.CreateBooleanUnion(solidBreps, 0.001);
            if (union != null && union.Length > 0)
            {
                result = union[0];
            }
        }

        var metadata = ParametricMetadata.Attach(result);
        metadata.Parameters.Set("SettingType", "ProngHead");
        metadata.Parameters.Set("ProngCount", parameters.ProngCount);
        metadata.Parameters.Set("ProngDiameterMm", parameters.ProngDiameterMm);
        metadata.Parameters.Set("GirdleOverhangMm", parameters.GirdleOverhangMm);

        return result;
    }

    /// <summary>
    /// Calculates radial angular positions for N prongs based on the cut's canonical symmetry.
    /// </summary>
    public static IReadOnlyList<double> CalculateProngAngles(GemCutType cutType, GemDimensions dim, int count)
    {
        var angles = new List<double>(count);

        switch (cutType)
        {
            case GemCutType.Princess:
                // 4 corners: 45, 135, 225, 315 deg
                if (count == 4)
                {
                    angles.AddRange(new[] { Math.PI * 0.25, Math.PI * 0.75, Math.PI * 1.25, Math.PI * 1.75 });
                }
                else if (count == 6)
                {
                    angles.AddRange(new[] { 0.0, Math.PI * 0.25, Math.PI * 0.75, Math.PI, Math.PI * 1.25, Math.PI * 1.75 });
                }
                else
                {
                    for (int k = 0; k < 8; k++) angles.Add(k * Math.PI * 0.25);
                }
                break;

            case GemCutType.Emerald:
            case GemCutType.Radiant:
            case GemCutType.Asscher:
            case GemCutType.Baguette:
            case GemCutType.TaperedBaguette:
            {
                double thetaCorner = Math.Atan2(dim.AspectRatio, 1.0);
                if (count == 4)
                {
                    angles.AddRange(new[]
                    {
                        thetaCorner,
                        Math.PI - thetaCorner,
                        Math.PI + thetaCorner,
                        (2.0 * Math.PI) - thetaCorner
                    });
                }
                else if (count == 6)
                {
                    angles.AddRange(new[]
                    {
                        thetaCorner,
                        Math.PI * 0.5,
                        Math.PI - thetaCorner,
                        Math.PI + thetaCorner,
                        Math.PI * 1.5,
                        (2.0 * Math.PI) - thetaCorner
                    });
                }
                else
                {
                    angles.AddRange(new[]
                    {
                        0.0,
                        thetaCorner,
                        Math.PI * 0.5,
                        Math.PI - thetaCorner,
                        Math.PI,
                        Math.PI + thetaCorner,
                        Math.PI * 1.5,
                        (2.0 * Math.PI) - thetaCorner
                    });
                }
                break;
            }

            case GemCutType.Marquise:
            {
                // Tips at 90 and 270 deg
                if (count == 4)
                {
                    angles.AddRange(new[] { Math.PI * 0.25, Math.PI * 0.75, Math.PI * 1.25, Math.PI * 1.75 });
                }
                else if (count == 6)
                {
                    angles.AddRange(new[]
                    {
                        Math.PI * 0.25,
                        Math.PI * 0.5,
                        Math.PI * 0.75,
                        Math.PI * 1.25,
                        Math.PI * 1.5,
                        Math.PI * 1.75
                    });
                }
                else
                {
                    for (int k = 0; k < count; k++) angles.Add(k * (2.0 * Math.PI / count));
                }
                break;
            }

            case GemCutType.Pear:
            {
                // Tip at 90 deg (pi/2)
                if (count == 4)
                {
                    angles.AddRange(new[] { Math.PI * 0.5, Math.PI * 1.0, Math.PI * 1.5, 0.0 });
                }
                else if (count == 6)
                {
                    angles.AddRange(new[]
                    {
                        Math.PI * 0.5,
                        Math.PI * 0.85,
                        Math.PI * 1.25,
                        Math.PI * 1.5,
                        Math.PI * 1.75,
                        Math.PI * 0.15
                    });
                }
                else
                {
                    for (int k = 0; k < count; k++) angles.Add(k * (2.0 * Math.PI / count));
                }
                break;
            }

            default:
            {
                // Round, Oval, Cushion, Trilliant, Heart
                double startOffset = (count == 4 || count == 6) ? (Math.PI / count) : 0.0;
                double step = (2.0 * Math.PI) / count;
                for (int k = 0; k < count; k++)
                {
                    angles.Add(startOffset + (k * step));
                }
                break;
            }
        }

        return angles;
    }

    private static bool IsCornerOrTipProng(GemCutType cutType, double angleRad)
    {
        double deg = (angleRad * 180.0 / Math.PI) % 360.0;
        if (deg < 0) deg += 360.0;

        return cutType switch
        {
            GemCutType.Princess => Math.Abs(deg - 45) < 5 || Math.Abs(deg - 135) < 5 || Math.Abs(deg - 225) < 5 || Math.Abs(deg - 315) < 5,
            GemCutType.Marquise => Math.Abs(deg - 90) < 5 || Math.Abs(deg - 270) < 5,
            GemCutType.Pear => Math.Abs(deg - 90) < 5,
            _ => false
        };
    }

    private static void GenerateProngCylinder(
        Point3D pBase,
        Point3D pTop,
        double rBase,
        double rTop,
        ProngStyle style,
        Vector3D outwardNormal,
        List<Point3D> vertices,
        List<int[]> faces)
    {
        const int radialSegments = 12;
        int vertexOffset = vertices.Count;

        // Local coordinate frame for cylinder cross sections
        var zAxis = (pTop - pBase).Normalize();
        var yAxis = outwardNormal;
        var xAxis = Vector3D.Cross(yAxis, zAxis).Normalize();
        yAxis = Vector3D.Cross(zAxis, xAxis).Normalize();

        double tGirdle = (0.0 - pBase.Z) / (pTop.Z - pBase.Z);
        double[] tSlices;
        if (tGirdle > 0.05 && tGirdle < 0.95)
        {
            tSlices = new[] { 0.0, tGirdle * 0.5, tGirdle, tGirdle + ((1.0 - tGirdle) * 0.5), 1.0 };
        }
        else
        {
            tSlices = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
        }

        int heightSlices = tSlices.Length - 1;

        // Generate rings along cylinder height
        for (int s = 0; s <= heightSlices; s++)
        {
            double t = tSlices[s];
            var center = pBase + ((pTop - pBase) * t);
            double r = rBase + ((rTop - rBase) * t);

            for (int k = 0; k < radialSegments; k++)
            {
                double phi = (2.0 * Math.PI * k) / radialSegments;
                double cosP = Math.Cos(phi);
                double sinP = Math.Sin(phi);
                var pt = center + (xAxis * (r * sinP)) + (yAxis * (r * cosP));
                vertices.Add(pt);
            }
        }

        // Side quads between slices
        for (int s = 0; s < heightSlices; s++)
        {
            int r0 = vertexOffset + (s * radialSegments);
            int r1 = vertexOffset + ((s + 1) * radialSegments);

            for (int k = 0; k < radialSegments; k++)
            {
                int nextK = (k + 1) % radialSegments;
                // Quad: (r0+k, r0+nextK, r1+nextK, r1+k) split into 2 CCW triangles
                faces.Add(new[] { r0 + k, r0 + nextK, r1 + nextK });
                faces.Add(new[] { r0 + k, r1 + nextK, r1 + k });
            }
        }

        // Bottom cap (normal points towards -zAxis)
        int bottomCenterIdx = vertices.Count;
        vertices.Add(pBase);
        int baseRingStart = vertexOffset;
        for (int k = 0; k < radialSegments; k++)
        {
            int nextK = (k + 1) % radialSegments;
            // CCW orientation for bottom exterior cap
            faces.Add(new[] { bottomCenterIdx, baseRingStart + nextK, baseRingStart + k });
        }

        // Top styling
        int topRingStart = vertexOffset + (heightSlices * radialSegments);

        if (style == ProngStyle.RoundFlat)
        {
            int topCenterIdx = vertices.Count;
            vertices.Add(pTop);
            for (int k = 0; k < radialSegments; k++)
            {
                int nextK = (k + 1) % radialSegments;
                faces.Add(new[] { topCenterIdx, topRingStart + k, topRingStart + nextK });
            }
        }
        else if (style == ProngStyle.Claw)
        {
            // Pointed claw tip angled inward towards the gem
            int apexIdx = vertices.Count;
            var clawApex = pTop - (yAxis * (rTop * 0.6)) + (zAxis * (rTop * 0.8));
            vertices.Add(clawApex);
            for (int k = 0; k < radialSegments; k++)
            {
                int nextK = (k + 1) % radialSegments;
                faces.Add(new[] { apexIdx, topRingStart + k, topRingStart + nextK });
            }
        }
        else // RoundDome and others
        {
            // 2 dome rings + apex pole
            const int domeSlices = 2;
            int prevRingStart = topRingStart;

            for (int ds = 1; ds <= domeSlices; ds++)
            {
                double theta = (Math.PI * 0.5 * ds) / (domeSlices + 1);
                double cosT = Math.Cos(theta);
                double sinT = Math.Sin(theta);
                double rDome = rTop * cosT;
                var center = pTop + (zAxis * (rTop * sinT));

                int currentRingStart = vertices.Count;
                for (int k = 0; k < radialSegments; k++)
                {
                    double phi = (2.0 * Math.PI * k) / radialSegments;
                    var pt = center + (xAxis * (rDome * Math.Sin(phi))) + (yAxis * (rDome * Math.Cos(phi)));
                    vertices.Add(pt);
                }

                for (int k = 0; k < radialSegments; k++)
                {
                    int nextK = (k + 1) % radialSegments;
                    faces.Add(new[] { prevRingStart + k, prevRingStart + nextK, currentRingStart + nextK });
                    faces.Add(new[] { prevRingStart + k, currentRingStart + nextK, currentRingStart + k });
                }

                prevRingStart = currentRingStart;
            }

            int poleIdx = vertices.Count;
            vertices.Add(pTop + (zAxis * rTop));
            for (int k = 0; k < radialSegments; k++)
            {
                int nextK = (k + 1) % radialSegments;
                faces.Add(new[] { poleIdx, prevRingStart + k, prevRingStart + nextK });
            }
        }
    }

    private static void GenerateVProng(
        GemCutType cutType,
        double cornerAngleRad,
        Point3D pBase,
        Point3D pTop,
        double prongDiameterMm,
        Vector3D normal,
        List<Point3D> vertices,
        List<int[]> faces)
    {
        // 6-vertex chevron cross-section wrapping around corner
        double w = prongDiameterMm;
        double t = w * 0.45;
        var zAxis = (pTop - pBase).Normalize();
        var yAxis = normal;
        var xAxis = Vector3D.Cross(yAxis, zAxis).Normalize();
        yAxis = Vector3D.Cross(zAxis, xAxis).Normalize();

        // 6 2D points in (xAxis, yAxis) frame forming chevron
        var localProfile = new (double X, double Y)[]
        {
            ( 0.0,  w),      // 0: outer corner apex
            ( w,    0.0),    // 1: outer wing right
            ( w - t, -t),    // 2: inner wing right
            ( 0.0,  w - 2*t),// 3: inner corner notch
            (-w + t, -t),    // 4: inner wing left
            (-w,    0.0)     // 5: outer wing left
        };

        int vertexOffset = vertices.Count;
        int m = localProfile.Length;

        // Base ring (Z = pBase.Z)
        for (int j = 0; j < m; j++)
        {
            var (lx, ly) = localProfile[j];
            vertices.Add(pBase + (xAxis * lx) + (yAxis * ly));
        }

        // Top ring (Z = pTop.Z)
        for (int j = 0; j < m; j++)
        {
            var (lx, ly) = localProfile[j];
            vertices.Add(pTop + (xAxis * (lx * 0.85)) + (yAxis * (ly * 0.85)));
        }

        // Side walls (6 quads -> 12 triangles)
        for (int j = 0; j < m; j++)
        {
            int nextJ = (j + 1) % m;
            int b0 = vertexOffset + j;
            int b1 = vertexOffset + nextJ;
            int t0 = vertexOffset + m + j;
            int t1 = vertexOffset + m + nextJ;

            faces.Add(new[] { b0, b1, t1 });
            faces.Add(new[] { b0, t1, t0 });
        }

        // Base cap: triangulate 6-gon from interior
        int baseCenter = vertices.Count;
        vertices.Add(pBase);
        for (int j = 0; j < m; j++)
        {
            int nextJ = (j + 1) % m;
            faces.Add(new[] { baseCenter, vertexOffset + nextJ, vertexOffset + j });
        }

        // Top cap: triangulate 6-gon
        int topCenter = vertices.Count;
        vertices.Add(pTop);
        for (int j = 0; j < m; j++)
        {
            int nextJ = (j + 1) % m;
            faces.Add(new[] { topCenter, vertexOffset + m + j, vertexOffset + m + nextJ });
        }
    }

    private static void GenerateWireRing(
        IReadOnlyList<Point3D> spinePoints,
        double wireRadius,
        List<Point3D> vertices,
        List<int[]> faces)
    {
        int nStations = spinePoints.Count;
        const int crossSegments = 8;
        int vertexOffset = vertices.Count;

        for (int i = 0; i < nStations; i++)
        {
            var pPrev = spinePoints[(i + nStations - 1) % nStations];
            var pCurr = spinePoints[i];
            var pNext = spinePoints[(i + 1) % nStations];

            var tangent = (pNext - pPrev).Normalize();
            var up = new Vector3D(0, 0, 1);
            var radial = Vector3D.Cross(up, tangent).Normalize();
            var normal = Vector3D.Cross(tangent, radial).Normalize();

            for (int j = 0; j < crossSegments; j++)
            {
                double phi = (2.0 * Math.PI * j) / crossSegments;
                var pt = pCurr + (radial * (wireRadius * Math.Cos(phi))) + (normal * (wireRadius * Math.Sin(phi)));
                vertices.Add(pt);
            }
        }

        for (int i = 0; i < nStations; i++)
        {
            int nextI = (i + 1) % nStations;
            int r0 = vertexOffset + (i * crossSegments);
            int r1 = vertexOffset + (nextI * crossSegments);

            for (int j = 0; j < crossSegments; j++)
            {
                int nextJ = (j + 1) % crossSegments;
                faces.Add(new[] { r0 + j, r0 + nextJ, r1 + nextJ });
                faces.Add(new[] { r0 + j, r1 + nextJ, r1 + j });
            }
        }
    }
}
