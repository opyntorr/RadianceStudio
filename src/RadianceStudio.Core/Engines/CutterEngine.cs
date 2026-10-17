namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Parametric cutter and boolean subtraction engine for procedural jewelry stone seating.
/// Generates closed watertight 2-manifold cutter solids for all 13 canonical gem cut types,
/// featuring upper crown piercing extensions with outward draft, girdle tolerance clearance bands,
/// angled bearing seats (70-deg, 90-deg, flat shelf, conical), and lower piercing light hole punches.
/// Operates in dual mode: pure C# closed 2-manifold GemMesh and RhinoCommon Brep wrapper with adaptive boolean subtraction.
/// </summary>
public static class CutterEngine
{
    private const int StationCount = 64;
    private const int RingCount = 8;
    private const double DraftAngleDeg = 2.0;

    /// <summary>
    /// Generates a closed, watertight 2-manifold cutter solid mesh specifically designed
    /// for boolean subtraction against metal (shanks, bezels, channels, halos).
    /// </summary>
    /// <param name="gem">Canonical proportional dimensions and cut type of the gem.</param>
    /// <param name="parameters">Parametric cutter configuration tolerances and depths.</param>
    /// <returns>Watertight 2-manifold GemMesh cutter solid.</returns>
    public static GemMesh CreateGemCutterMesh(GemDimensions gem, CutterParameters parameters)
    {
        if (gem == null) throw new ArgumentNullException(nameof(gem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        double d = gem.DiameterMm;
        double g = d * (gem.GirdleThicknessPercent / 100.0);
        double hc = d * (gem.CrownHeightPercent / 100.0);
        double hp = d * (gem.PavilionDepthPercent / 100.0);

        // Z Coordinate reference planes
        double zGirdleTop = g * 0.5;
        double zGirdleBottom = -(g * 0.5);
        double zTable = zGirdleTop + hc;
        double zTop = zTable + parameters.CrownHeightExtensionMm;
        double zCulet = zGirdleBottom - hp;
        double zBottom = zCulet - parameters.LightHoleDepthExtensionMm;

        // Seat and pavilion transition heights
        double seatDepthMm = parameters.SeatAngle == CutterSeatStyle.FlatShelf
            ? 0.05 * hp
            : 0.20 * hp;
        double zSeat = zGirdleBottom - seatDepthMm;
        double zMidPav = 0.5 * (zSeat + zCulet);

        // Radial draft calculations
        double draftRad = DraftAngleDeg * (Math.PI / 180.0);
        double tanDraft = Math.Tan(draftRad);
        double cGirdle = parameters.GirdleClearanceMm;

        double deltaZTop = zTop - zGirdleTop;
        double offsetTop = cGirdle + (deltaZTop * tanDraft);
        double offsetTable = cGirdle + (hc * tanDraft);

        // Light hole shaft dimensions
        double baseHoleRatio = parameters.LightHoleDiameterRatio;
        double shaftDraftRad = parameters.TaperShaft ? (3.0 * Math.PI / 180.0) : 0.0;
        double shaftTaperDelta = parameters.LightHoleDepthExtensionMm * Math.Tan(shaftDraftRad);

        // 514 Vertices: 1 top center + (8 rings * 64 stations) + 1 bottom center
        var vertices = new List<Point3D>(1 + (RingCount * StationCount) + 1)
        {
            // Vertex 0: Top cap center
            new Point3D(0.0, 0.0, zTop)
        };

        // Ring vertices
        for (int k = 0; k < RingCount; k++)
        {
            for (int i = 0; i < StationCount; i++)
            {
                double phi = (2.0 * Math.PI * i) / StationCount;
                var pGirdle = GemGirdleEvaluator.EvaluateGirdlePoint(gem.CutType, gem, phi);
                var normal = GemGirdleEvaluator.EvaluateGirdleNormal(gem.CutType, gem, phi);
                double pLen = Math.Sqrt((pGirdle.X * pGirdle.X) + (pGirdle.Y * pGirdle.Y));

                Point3D pt;
                switch (k)
                {
                    case 0: // Ring 0: Top cap perimeter (Z = zTop)
                        pt = new Point3D(
                            pGirdle.X + (normal.X * offsetTop),
                            pGirdle.Y + (normal.Y * offsetTop),
                            zTop);
                        break;

                    case 1: // Ring 1: Table level with draft (Z = zTable)
                        pt = new Point3D(
                            pGirdle.X + (normal.X * offsetTable),
                            pGirdle.Y + (normal.Y * offsetTable),
                            zTable);
                        break;

                    case 2: // Ring 2: Girdle top outer clearance (Z = zGirdleTop)
                        pt = new Point3D(
                            pGirdle.X + (normal.X * cGirdle),
                            pGirdle.Y + (normal.Y * cGirdle),
                            zGirdleTop);
                        break;

                    case 3: // Ring 3: Girdle bottom outer clearance (Z = zGirdleBottom)
                        pt = new Point3D(
                            pGirdle.X + (normal.X * cGirdle),
                            pGirdle.Y + (normal.Y * cGirdle),
                            zGirdleBottom);
                        break;

                    case 4: // Ring 4: Bearing seat ledge (Z = zSeat)
                    {
                        double deltaR = parameters.SeatAngle switch
                        {
                            CutterSeatStyle.Degree70 => seatDepthMm * Math.Tan(35.0 * Math.PI / 180.0),
                            CutterSeatStyle.Degree90 => seatDepthMm * Math.Tan(45.0 * Math.PI / 180.0),
                            CutterSeatStyle.FlatShelf => cGirdle + Math.Min(0.25, d * 0.08),
                            CutterSeatStyle.Conical => seatDepthMm / Math.Tan(Math.Max(gem.PavilionAngleDeg, 20.0) * Math.PI / 180.0),
                            _ => seatDepthMm * Math.Tan(35.0 * Math.PI / 180.0)
                        };

                        double maxInward = 0.40 * pLen;
                        if (deltaR > maxInward) deltaR = maxInward;

                        double sSeat = cGirdle - deltaR;
                        pt = new Point3D(
                            pGirdle.X + (normal.X * sSeat),
                            pGirdle.Y + (normal.Y * sSeat),
                            zSeat);
                        break;
                    }

                    case 5: // Ring 5: Mid pavilion clearance (Z = zMidPav)
                    {
                        double cPav = d * (parameters.PavilionOffsetPercent / 100.0);
                        double tMid = (zMidPav - zCulet) / hp;
                        pt = new Point3D(
                            (pGirdle.X * tMid) + (normal.X * cPav),
                            (pGirdle.Y * tMid) + (normal.Y * cPav),
                            zMidPav);
                        break;
                    }

                    case 6: // Ring 6: Culet level / light hole entry (Z = zCulet)
                    {
                        double rBottom = pLen * baseHoleRatio;
                        double rTop = Math.Max(0.10 * pLen, rBottom - shaftTaperDelta);
                        double topRatio = rTop / Math.Max(pLen, 1e-9);
                        pt = new Point3D(
                            pGirdle.X * topRatio,
                            pGirdle.Y * topRatio,
                            zCulet);
                        break;
                    }

                    case 7: // Ring 7: Shaft bottom perimeter (Z = zBottom)
                    default:
                        pt = new Point3D(
                            pGirdle.X * baseHoleRatio,
                            pGirdle.Y * baseHoleRatio,
                            zBottom);
                        break;
                }

                vertices.Add(pt);
            }
        }

        // Vertex 513: Bottom cap center
        int bottomCenterIdx = vertices.Count;
        vertices.Add(new Point3D(0.0, 0.0, zBottom));

        // Generate faces with strictly consistent outward winding
        // Total faces: 64 (top) + 7 * 128 (sides) + 64 (bottom) = 1024 triangular faces
        var faces = new List<int[]>((RingCount * StationCount * 2));

        // 1. Top cap fan
        for (int i = 0; i < StationCount; i++)
        {
            int nextI = (i + 1) % StationCount;
            int a = 1 + (0 * StationCount) + i;
            int b = 1 + (0 * StationCount) + nextI;
            faces.Add(new[] { 0, a, b });
        }

        // 2. Side swept quad strips
        for (int k = 0; k < RingCount - 1; k++)
        {
            int ringTopOffset = 1 + (k * StationCount);
            int ringBotOffset = 1 + ((k + 1) * StationCount);

            for (int i = 0; i < StationCount; i++)
            {
                int nextI = (i + 1) % StationCount;
                int a = ringTopOffset + i;
                int b = ringTopOffset + nextI;
                int c = ringBotOffset + i;
                int dVert = ringBotOffset + nextI;

                // Two triangles per quad with outward normal orientation
                faces.Add(new[] { a, c, dVert });
                faces.Add(new[] { a, dVert, b });
            }
        }

        // 3. Bottom cap fan
        int lastRingOffset = 1 + ((RingCount - 1) * StationCount);
        for (int i = 0; i < StationCount; i++)
        {
            int nextI = (i + 1) % StationCount;
            int c = lastRingOffset + i;
            int dVert = lastRingOffset + nextI;
            faces.Add(new[] { bottomCenterIdx, dVert, c });
        }

        // Compute outward face normals using Newell's method
        var normals = new Vector3D[faces.Count];
        for (int f = 0; f < faces.Count; f++)
        {
            normals[f] = GemMesh.ComputeFaceNormal(vertices, faces[f]);
        }

        return new GemMesh
        {
            CutType = gem.CutType,
            DiameterMm = gem.DiameterMm,
            Vertices = vertices,
            Faces = faces,
            Normals = normals
        };
    }

    /// <summary>
    /// Generates transformed cutter instances for an entire collection of gems
    /// and combines them into a single unified cutter mesh for high-performance single-pass subtraction.
    /// </summary>
    /// <param name="gems">Collection of gem dimensions and their respective 4x4 placement transforms.</param>
    /// <param name="parameters">Parametric cutter configuration parameters.</param>
    /// <returns>Unified GemMesh containing all transformed cutter solids.</returns>
    public static GemMesh CreateBatchCutterMesh(
        IReadOnlyList<(GemDimensions gem, Matrix4x4D transform)> gems,
        CutterParameters parameters)
    {
        if (gems == null) throw new ArgumentNullException(nameof(gems));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (gems.Count == 0)
        {
            return new GemMesh();
        }

        if (gems.Count == 1)
        {
            var singleMesh = CreateGemCutterMesh(gems[0].gem, parameters);
            return singleMesh.Transform(gems[0].transform);
        }

        int estimatedVertices = gems.Count * (1 + (RingCount * StationCount) + 1);
        int estimatedFaces = gems.Count * (RingCount * StationCount * 2);

        var batchVertices = new List<Point3D>(estimatedVertices);
        var batchFaces = new List<int[]>(estimatedFaces);
        var batchNormals = new List<Vector3D>(estimatedFaces);

        for (int k = 0; k < gems.Count; k++)
        {
            var (gemDim, transform) = gems[k];
            var singleCutter = CreateGemCutterMesh(gemDim, parameters).Transform(transform);

            int vertexOffset = batchVertices.Count;
            batchVertices.AddRange(singleCutter.Vertices);

            for (int f = 0; f < singleCutter.Faces.Count; f++)
            {
                var originalFace = singleCutter.Faces[f];
                var offsetFace = new int[originalFace.Length];
                for (int v = 0; v < originalFace.Length; v++)
                {
                    offsetFace[v] = originalFace[v] + vertexOffset;
                }
                batchFaces.Add(offsetFace);
            }

            batchNormals.AddRange(singleCutter.Normals);
        }

        return new GemMesh
        {
            CutType = gems[0].gem.CutType,
            DiameterMm = gems[0].gem.DiameterMm,
            Vertices = batchVertices,
            Faces = batchFaces,
            Normals = batchNormals
        };
    }

    /// <summary>
    /// Dual-execution RhinoCommon Brep wrapper for procedural cutter solid generation.
    /// Returns null safely when running in headless mode outside of Rhino hosting.
    /// </summary>
    /// <param name="gem">Canonical gem dimensions.</param>
    /// <param name="parameters">Parametric cutter configuration parameters.</param>
    /// <param name="transform">Optional 4x4 placement transform.</param>
    /// <returns>Closed solid Brep cutter or null if Rhino native runtime is unavailable.</returns>
    public static Brep? CreateGemCutterBrep(
        GemDimensions gem,
        CutterParameters parameters,
        Matrix4x4D? transform = null)
    {
        if (gem == null) throw new ArgumentNullException(nameof(gem));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var mesh = CreateGemCutterMesh(gem, parameters);
        var rhinoMesh = new Rhino.Geometry.Mesh();

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            rhinoMesh.Vertices.Add(v.X, v.Y, v.Z);
        }

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            rhinoMesh.Faces.AddFace(face[0], face[1], face[2]);
        }

        rhinoMesh.Normals.ComputeNormals();
        rhinoMesh.Compact();

        Brep? brep = null;

        // Attempt lofting closed ring curves for smooth NURBS representation
        try
        {
            var ringCurves = new List<Curve>(RingCount);
            for (int k = 0; k < RingCount; k++)
            {
                var pts = new List<Point3d>(StationCount + 1);
                for (int i = 0; i <= StationCount; i++)
                {
                    int stationIdx = i % StationCount;
                    var v = mesh.Vertices[1 + (k * StationCount) + stationIdx];
                    pts.Add(new Point3d(v.X, v.Y, v.Z));
                }
                var poly = new Polyline(pts);
                ringCurves.Add(poly.ToNurbsCurve());
            }

            var lofts = Brep.CreateFromLoft(ringCurves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            if (lofts != null && lofts.Length > 0)
            {
                var capped = lofts[0].CapPlanarHoles(0.001);
                if (capped != null && capped.IsValid && capped.IsSolid)
                {
                    brep = capped;
                }
            }
        }
        catch
        {
            // Fallback to polygon mesh conversion
        }

        if (brep == null)
        {
            brep = Brep.CreateFromMesh(rhinoMesh, false);
        }

        if (brep != null && transform.HasValue)
        {
            var m = transform.Value;
            var xform = new Transform
            {
                M00 = m.M00, M01 = m.M01, M02 = m.M02, M03 = m.M03,
                M10 = m.M10, M11 = m.M11, M12 = m.M12, M13 = m.M13,
                M20 = m.M20, M21 = m.M21, M22 = m.M22, M23 = m.M23,
                M30 = m.M30, M31 = m.M31, M32 = m.M32, M33 = m.M33
            };
            brep.Transform(xform);
        }

        if (brep != null)
        {
            var metadata = ParametricMetadata.Attach(brep);
            metadata.Parameters.Set("SettingType", "Cutter");
            metadata.Parameters.Set("GirdleClearanceMm", parameters.GirdleClearanceMm);
            metadata.Parameters.Set("SeatAngle", parameters.SeatAngle.ToString());
            metadata.Parameters.Set("CrownHeightExtensionMm", parameters.CrownHeightExtensionMm);
            metadata.Parameters.Set("PavilionOffsetPercent", parameters.PavilionOffsetPercent);
            metadata.Parameters.Set("LightHoleDiameterRatio", parameters.LightHoleDiameterRatio);
            metadata.Parameters.Set("LightHoleDepthExtensionMm", parameters.LightHoleDepthExtensionMm);
            metadata.Parameters.Set("TaperShaft", parameters.TaperShaft);
        }

        return brep;
    }

    /// <summary>
    /// High-level boolean helper that subtracts a single cutter from a target metal solid
    /// using adaptive tolerance fallbacks.
    /// </summary>
    /// <param name="targetMetal">The solid Brep metal geometry to carve.</param>
    /// <param name="cutter">The cutter Brep geometry.</param>
    /// <param name="baseTolerance">Initial document tolerance in millimeters (default 0.001 mm).</param>
    /// <returns>The carved metal Brep, or targetMetal if subtraction could not be performed.</returns>
    public static Brep ApplyCutterDifference(
        Brep targetMetal,
        Brep cutter,
        double baseTolerance = 0.001)
    {
        if (targetMetal == null) throw new ArgumentNullException(nameof(targetMetal));
        if (cutter == null) throw new ArgumentNullException(nameof(cutter));
        return ApplyCutterDifference(targetMetal, new[] { cutter }, baseTolerance);
    }

    /// <summary>
    /// High-level boolean helper that subtracts multiple cutters from a target metal solid
    /// using multi-pass adaptive tolerance fallbacks and sequential degradation.
    /// </summary>
    /// <param name="targetMetal">The solid Brep metal geometry to carve.</param>
    /// <param name="cutters">Collection of cutter Brep geometries.</param>
    /// <param name="baseTolerance">Initial document tolerance in millimeters (default 0.001 mm).</param>
    /// <returns>The carved metal Brep, or targetMetal if subtraction could not be completed.</returns>
    public static Brep ApplyCutterDifference(
        Brep targetMetal,
        IEnumerable<Brep> cutters,
        double baseTolerance = 0.001)
    {
        if (targetMetal == null) throw new ArgumentNullException(nameof(targetMetal));
        if (cutters == null) throw new ArgumentNullException(nameof(cutters));

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return targetMetal;
        }

        var validCutters = cutters.Where(c => c != null && c.IsValid).ToList();
        if (validCutters.Count == 0)
        {
            return targetMetal;
        }

        double[] tolerances = new[] { baseTolerance, baseTolerance * 2.0, baseTolerance * 5.0, baseTolerance * 10.0, 0.05, 0.1 };

        // Attempt 1: Batch subtraction across adaptive tolerances
        foreach (double tol in tolerances)
        {
            try
            {
                var diff = Brep.CreateBooleanDifference(new[] { targetMetal }, validCutters, tol);
                if (diff != null && diff.Length > 0 && diff[0].IsValid && diff[0].IsSolid)
                {
                    return diff[0];
                }
            }
            catch
            {
                // Continue to next tolerance
            }
        }

        // Attempt 2: Sequential subtraction cutter-by-cutter with adaptive tolerance
        Brep current = targetMetal;
        bool anySuccess = false;

        foreach (var cutter in validCutters)
        {
            foreach (double tol in tolerances)
            {
                try
                {
                    var diff = Brep.CreateBooleanDifference(current, cutter, tol);
                    if (diff != null && diff.Length > 0 && diff[0].IsValid && diff[0].IsSolid)
                    {
                        current = diff[0];
                        anySuccess = true;
                        break;
                    }
                }
                catch
                {
                    // Continue to next tolerance
                }
            }
        }

        if (anySuccess && current.IsValid && current.IsSolid)
        {
            return current;
        }

        return targetMetal;
    }
}
