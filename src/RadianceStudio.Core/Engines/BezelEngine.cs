namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric setting engine for procedural full and semi-bezel generation.
/// Generates inner profiles matching the girdle contours of all 13 canonical gem cut types,
/// offsets outer walls enforcing structural casting thickness (&gt;= 0.4 mm) and draft angles,
/// sculpts horizontal bearing seat ledges and conical light holes,
/// and produces smooth parametric lateral arches when semi-bezel mode is enabled.
/// Operates in dual mode: pure C# closed 2-manifold GemMesh and RhinoCommon Brep wrapper.
/// </summary>
public static class BezelEngine
{
    /// <summary>
    /// Generates a watertight closed 2-manifold mesh representation of the bezel setting.
    /// </summary>
    public static GemMesh CreateBezelMesh(GemDimensions centerGem, BezelParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        const int numStations = 64;
        const int numProfilePoints = 7;

        var vertices = new List<Point3D>(numStations * numProfilePoints);
        var faces = new List<int[]>(numStations * numProfilePoints * 2);

        double d = centerGem.DiameterMm;
        double g = d * (centerGem.GirdleThicknessPercent / 100.0);
        double hp = d * (centerGem.PavilionDepthPercent / 100.0);
        double zCulet = -((g * 0.5) + hp);
        double zBase = zCulet;

        double seatDepthMm = (g * 0.5) * (parameters.SeatDepthPercent / 100.0);
        double zSeat = -seatDepthMm;
        double zTopFull = (g * 0.5) + parameters.HeightAboveGirdleMm;

        double draftRad = parameters.DraftAngleDeg * (Math.PI / 180.0);
        double tanDraft = Math.Tan(draftRad);

        double openingHalfRad = (parameters.SemiBezelOpeningAngleDeg * (Math.PI / 180.0)) * 0.5;

        for (int i = 0; i < numStations; i++)
        {
            double phi = (2.0 * Math.PI * i) / numStations;
            var pGirdle = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, phi);
            var normal = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, phi);

            // Determine effective top height for full vs semi-bezel
            double zTopEffective = zTopFull;
            if (parameters.IsSemiBezel)
            {
                // Opposing openings centered at 0 and PI
                double d0 = Math.Min(Math.Abs(phi), Math.Abs(phi - (2.0 * Math.PI)));
                double dPi = Math.Abs(phi - Math.PI);
                double minDelta = Math.Min(d0, dPi);

                if (minDelta < openingHalfRad)
                {
                    double tArch = minDelta / openingHalfRad;
                    // Smooth cosine transition down to seat ledge
                    double blend = 0.5 - (0.5 * Math.Cos(Math.PI * tArch));
                    zTopEffective = zSeat + ((zTopFull - zSeat) * blend);
                }
            }

            // Radial offsets
            double wallThick = parameters.WallThicknessMm;
            double hSpan = zTopEffective - zBase;
            double wTop = wallThick + (hSpan * tanDraft);
            double wGirdle = wallThick + ((0.0 - zBase) * tanDraft);
            double wSeat = wallThick + ((zSeat - zBase) * tanDraft);
            double wBase = wallThick;

            // Seat ledge step inward
            double seatStepMm = Math.Min(0.25, wallThick * 0.40);

            // Conical central light hole at base
            double lightHoleRadius = (d * 0.5) * parameters.LightHoleDiameterRatio;
            var pLightHole = new Point3D(
                lightHoleRadius * Math.Cos(phi),
                lightHoleRadius * Math.Sin(phi),
                zBase);

            // 7 CCW profile points in the (R, Z) plane:
            // 0: Top outer rim
            // 1: Top inner rim
            // 2: Girdle inner contact
            // 3: Seat inner ledge step
            // 4: Light hole inner rim at base
            // 5: Base outer rim
            // 6: Outer wall at girdle

            var p0 = new Point3D(pGirdle.X + (normal.X * wTop), pGirdle.Y + (normal.Y * wTop), zTopEffective);
            var p1 = new Point3D(pGirdle.X, pGirdle.Y, zTopEffective);
            var p2 = new Point3D(pGirdle.X, pGirdle.Y, 0.0);
            var p3 = new Point3D(pGirdle.X - (normal.X * seatStepMm), pGirdle.Y - (normal.Y * seatStepMm), zSeat);
            var p4 = pLightHole;
            var p5 = new Point3D(pLightHole.X + (normal.X * wBase), pLightHole.Y + (normal.Y * wBase), zBase);
            var p6 = new Point3D(pGirdle.X + (normal.X * wGirdle), pGirdle.Y + (normal.Y * wGirdle), 0.0);

            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p2);
            vertices.Add(p3);
            vertices.Add(p4);
            vertices.Add(p5);
            vertices.Add(p6);
        }

        // Generate swept quad topology (split into 2 triangles with consistent outward orientation)
        for (int i = 0; i < numStations; i++)
        {
            int nextI = (i + 1) % numStations;
            int r0 = i * numProfilePoints;
            int r1 = nextI * numProfilePoints;

            for (int j = 0; j < numProfilePoints; j++)
            {
                int nextJ = (j + 1) % numProfilePoints;

                // Triangle 1 & 2 forming quad with outward-pointing normals
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
    /// Dual execution RhinoCommon Brep wrapper for bezel setting geometry.
    /// </summary>
    public static Brep? CreateBezelBrep(GemDimensions centerGem, BezelParameters parameters)
    {
        if (centerGem == null) throw new ArgumentNullException(nameof(centerGem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        const int numStations = 64;
        double d = centerGem.DiameterMm;
        double g = d * (centerGem.GirdleThicknessPercent / 100.0);
        double hp = d * (centerGem.PavilionDepthPercent / 100.0);
        double zBase = -((g * 0.5) + hp);
        double zTop = (g * 0.5) + parameters.HeightAboveGirdleMm;

        var outerPts = new List<Point3d>(numStations + 1);
        var innerPts = new List<Point3d>(numStations + 1);

        for (int i = 0; i <= numStations; i++)
        {
            double phi = (2.0 * Math.PI * (i % numStations)) / numStations;
            var pt = GemGirdleEvaluator.EvaluateGirdlePoint(centerGem.CutType, centerGem, phi);
            var n = GemGirdleEvaluator.EvaluateGirdleNormal(centerGem.CutType, centerGem, phi);

            innerPts.Add(new Point3d(pt.X, pt.Y, zTop));
            outerPts.Add(new Point3d(pt.X + (n.X * parameters.WallThicknessMm), pt.Y + (n.Y * parameters.WallThicknessMm), zTop));
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
        metadata.Parameters.Set("SettingType", "Bezel");
        metadata.Parameters.Set("WallThicknessMm", parameters.WallThicknessMm);
        metadata.Parameters.Set("HeightAboveGirdleMm", parameters.HeightAboveGirdleMm);
        metadata.Parameters.Set("IsSemiBezel", parameters.IsSemiBezel);

        return result;
    }
}
