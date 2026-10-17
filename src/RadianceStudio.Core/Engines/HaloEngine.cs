namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric setting engine for procedural halo melee distribution, frame orientation,
/// micro-prong calculation, and supporting platform under-rail mesh generation.
/// Operates in dual mode: pure C# closed 2-manifold GemMesh and RhinoCommon Brep wrapper.
/// </summary>
public static class HaloEngine
{
    /// <summary>
    /// Evaluates the offset halo perimeter curve and distributes melee stones with exact arc-length spacing,
    /// generating 4x4 placement transform frames and micro-prong locations.
    /// </summary>
    public static IReadOnlyList<HaloMeleePlacement> CalculateHaloMeleePlacements(GemDimensions centerGem, HaloParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        const int numCurveSamples = 256;
        var offsetPolygon = new List<Point3D>(numCurveSamples);

        double offsetDistance = parameters.GuardDistanceMm + (parameters.MeleeDiameterMm * 0.5);

        var centerOutline = GemGirdleEvaluator.SampleGirdleOutline(centerGem.CutType, centerGem, 180);

        // Generate dense offset perimeter polyline around the center gem
        for (int k = 0; k < numCurveSamples; k++)
        {
            double phi = (2.0 * Math.PI * k) / numCurveSamples;
            var pt = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, phi);
            var normal = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, phi);

            double candidateX = pt.X + (normal.X * offsetDistance);
            double candidateY = pt.Y + (normal.Y * offsetDistance);

            // Ensure non-collision at sharp corner chamfers
            double curDist = centerOutline.Min(g => Math.Sqrt(Math.Pow(candidateX - g.X, 2) + Math.Pow(candidateY - g.Y, 2)));
            if (curDist < offsetDistance)
            {
                double push = offsetDistance - curDist;
                candidateX += normal.X * push;
                candidateY += normal.Y * push;
            }

            offsetPolygon.Add(new Point3D(candidateX, candidateY, parameters.DropHeightMm));
        }

        // Compute cumulative arc length
        var cumLengths = new double[numCurveSamples + 1];
        cumLengths[0] = 0.0;
        for (int k = 0; k < numCurveSamples; k++)
        {
            var p0 = offsetPolygon[k];
            var p1 = offsetPolygon[(k + 1) % numCurveSamples];
            cumLengths[k + 1] = cumLengths[k] + (p1 - p0).Length;
        }

        double totalLength = cumLengths[numCurveSamples];
        double stepDistance = parameters.MeleeDiameterMm + parameters.SpacingMm;

        int meleeCount = (int)Math.Round(totalLength / stepDistance);
        if (meleeCount < 3) meleeCount = 3;

        double arcStep = totalLength / meleeCount;
        var placements = new List<HaloMeleePlacement>(meleeCount);

        for (int i = 0; i < meleeCount; i++)
        {
            double targetS = i * arcStep;
            var (pos, tangent, normal) = SampleCurveAtArcLength(offsetPolygon, cumLengths, targetS);

            // Guard check at final interpolated placement
            double dPlaced = centerOutline.Min(g => Math.Sqrt(Math.Pow(pos.X - g.X, 2) + Math.Pow(pos.Y - g.Y, 2)));
            if (dPlaced < offsetDistance)
            {
                double push = offsetDistance - dPlaced;
                pos = new Point3D(pos.X + (normal.X * push), pos.Y + (normal.Y * push), pos.Z);
            }

            var binormal = new Vector3D(0, 0, 1);
            double d = parameters.MeleeDiameterMm;

            // Row-major 4x4 affine transform matrix
            var matrix = new double[16]
            {
                tangent.X * d, normal.X * d, binormal.X * d, pos.X,
                tangent.Y * d, normal.Y * d, binormal.Y * d, pos.Y,
                tangent.Z * d, normal.Z * d, binormal.Z * d, pos.Z,
                0.0,           0.0,          0.0,            1.0
            };

            placements.Add(new HaloMeleePlacement
            {
                Index = i,
                CutType = parameters.MeleeGemCut,
                DiameterMm = parameters.MeleeDiameterMm,
                Position = pos,
                Tangent = tangent,
                Normal = normal,
                Binormal = binormal,
                Matrix4x4 = matrix,
                MicroProngPositions = Array.Empty<Point3D>()
            });
        }

        // Compute shared and inner/outer micro-prongs
        var finalPlacements = new List<HaloMeleePlacement>(meleeCount);
        double dMelee = parameters.MeleeDiameterMm;

        for (int i = 0; i < meleeCount; i++)
        {
            var curr = placements[i];
            var next = placements[(i + 1) % meleeCount];

            // Shared prong between current and next stone
            var pShared = new Point3D(
                (curr.Position.X + next.Position.X) * 0.5,
                (curr.Position.Y + next.Position.Y) * 0.5,
                parameters.DropHeightMm + (dMelee * 0.22));

            // Outer and inner retention prongs
            var pOuter = new Point3D(
                curr.Position.X + (curr.Normal.X * (dMelee * 0.55)),
                curr.Position.Y + (curr.Normal.Y * (dMelee * 0.55)),
                parameters.DropHeightMm + (dMelee * 0.18));

            var pInner = new Point3D(
                curr.Position.X - (curr.Normal.X * (dMelee * 0.55)),
                curr.Position.Y - (curr.Normal.Y * (dMelee * 0.55)),
                parameters.DropHeightMm + (dMelee * 0.18));

            finalPlacements.Add(curr with
            {
                MicroProngPositions = new[] { pShared, pOuter, pInner }
            });
        }

        return finalPlacements;
    }

    /// <summary>
    /// Generates a watertight closed 2-manifold metal under-rail / platform mesh beneath the halo melee stones.
    /// </summary>
    public static GemMesh CreateHaloPlatformMesh(GemDimensions centerGem, HaloParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        const int numStations = 64;
        const int numProfilePoints = 4;

        var vertices = new List<Point3D>(numStations * numProfilePoints);
        var faces = new List<int[]>(numStations * numProfilePoints * 2);

        double zTop = parameters.DropHeightMm;
        double zBottom = parameters.DropHeightMm - parameters.TrayDepthMm;

        double offsetCenter = parameters.GuardDistanceMm + (parameters.MeleeDiameterMm * 0.5);
        double offsetInner = parameters.GuardDistanceMm * 0.5;
        double offsetOuter = offsetCenter + (parameters.MeleeDiameterMm * 0.5) + parameters.TrayWallThicknessMm;

        for (int i = 0; i < numStations; i++)
        {
            double phi = (2.0 * Math.PI * i) / numStations;
            var pGirdle = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, phi);
            var normal = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, phi);

            var pInner = pGirdle + (normal * offsetInner);
            var pOuter = pGirdle + (normal * offsetOuter);

            // 4 CCW profile points in the (R, Z) plane:
            // 0: Top outer rim
            // 1: Top inner rim
            // 2: Bottom inner rim
            // 3: Bottom outer rim
            var pt0 = new Point3D(pOuter.X, pOuter.Y, zTop);
            var pt1 = new Point3D(pInner.X, pInner.Y, zTop);
            var pt2 = new Point3D(pInner.X, pInner.Y, zBottom);
            var pt3 = new Point3D(pOuter.X, pOuter.Y, zBottom);

            vertices.Add(pt0);
            vertices.Add(pt1);
            vertices.Add(pt2);
            vertices.Add(pt3);
        }

        for (int i = 0; i < numStations; i++)
        {
            int nextI = (i + 1) % numStations;
            int r0 = i * numProfilePoints;
            int r1 = nextI * numProfilePoints;

            for (int j = 0; j < numProfilePoints; j++)
            {
                int nextJ = (j + 1) % numProfilePoints;

                // Triangles forming quads with consistent outward normals
                faces.Add(new[] { r0 + j, r1 + j, r1 + nextJ });
                faces.Add(new[] { r0 + j, r1 + nextJ, r0 + nextJ });
            }
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
    /// Dual execution RhinoCommon Brep wrapper for halo platform geometry.
    /// </summary>
    public static Brep? CreateHaloPlatformBrep(GemDimensions centerGem, HaloParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        const int numStations = 64;
        double zTop = parameters.DropHeightMm;
        double zBottom = parameters.DropHeightMm - parameters.TrayDepthMm;

        double offsetCenter = parameters.GuardDistanceMm + (parameters.MeleeDiameterMm * 0.5);
        double offsetInner = parameters.GuardDistanceMm * 0.5;
        double offsetOuter = offsetCenter + (parameters.MeleeDiameterMm * 0.5) + parameters.TrayWallThicknessMm;

        var outerPts = new List<Point3d>(numStations + 1);
        var innerPts = new List<Point3d>(numStations + 1);

        for (int i = 0; i <= numStations; i++)
        {
            double phi = (2.0 * Math.PI * (i % numStations)) / numStations;
            var pt = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, phi);
            var n = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, phi);

            var pInner = pt + (n * offsetInner);
            var pOuter = pt + (n * offsetOuter);

            innerPts.Add(new Point3d(pInner.X, pInner.Y, zTop));
            outerPts.Add(new Point3d(pOuter.X, pOuter.Y, zTop));
        }

        var innerPoly = new Polyline(innerPts);
        var outerPoly = new Polyline(outerPts);

        var innerCurve = innerPoly.ToNurbsCurve();
        var outerCurve = outerPoly.ToNurbsCurve();

        var loftBreps = Brep.CreateFromLoft(new[] { outerCurve, innerCurve }, Point3d.Unset, Point3d.Unset, LoftType.Normal, true);
        if (loftBreps == null || loftBreps.Length == 0)
        {
            return null;
        }

        var capped = loftBreps[0].CapPlanarHoles(0.001);
        Brep result = capped ?? loftBreps[0];

        var metadata = ParametricMetadata.Attach(result);
        metadata.Parameters.Set("SettingType", "HaloPlatform");
        metadata.Parameters.Set("MeleeDiameterMm", parameters.MeleeDiameterMm);
        metadata.Parameters.Set("GuardDistanceMm", parameters.GuardDistanceMm);
        metadata.Parameters.Set("SpacingMm", parameters.SpacingMm);

        return result;
    }

    private static (Point3D Pos, Vector3D Tangent, Vector3D Normal) SampleCurveAtArcLength(
        IReadOnlyList<Point3D> polygon,
        double[] cumLengths,
        double targetS)
    {
        int n = polygon.Count;
        double totalLength = cumLengths[n];
        targetS %= totalLength;
        if (targetS < 0) targetS += totalLength;

        // Binary search for segment
        int low = 0;
        int high = n;
        while (low < high - 1)
        {
            int mid = (low + high) / 2;
            if (cumLengths[mid] <= targetS)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        double s0 = cumLengths[low];
        double s1 = cumLengths[low + 1];
        double segLen = s1 - s0;
        double frac = segLen > 1e-12 ? (targetS - s0) / segLen : 0.0;

        var p0 = polygon[low];
        var p1 = polygon[(low + 1) % n];

        var pos = p0 + ((p1 - p0) * frac);
        var tangent = (p1 - p0).Normalize();
        var normal = new Vector3D(tangent.Y, -tangent.X, 0.0);

        if (Vector3D.Dot(normal, new Vector3D(pos.X, pos.Y, 0.0)) < 0)
        {
            normal = -normal;
        }

        return (pos, tangent, normal);
    }
}
