namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;

/// <summary>
/// Computational geometry engine for dense pavé stone packing, interstitial bead placement,
/// and border festoon cutting on freeform 3D surfaces.
/// Operates in dual mode: pure C# closed 2-manifold GemMesh and RhinoCommon Brep wrapper.
/// </summary>
public static class PaveEngine
{
    private const int SphereLatitudeRings = 8;
    private const int SphereLongitudeSectors = 16;

    /// <summary>
    /// Calculates dense stone placements across a parameterized surface domain with Lloyd's relaxation.
    /// </summary>
    /// <param name="surface">Parametric 3D reference surface.</param>
    /// <param name="uMin">Minimum domain coordinate in U.</param>
    /// <param name="uMax">Maximum domain coordinate in U.</param>
    /// <param name="vMin">Minimum domain coordinate in V.</param>
    /// <param name="vMax">Maximum domain coordinate in V.</param>
    /// <param name="parameters">Parametric pavé configuration.</param>
    /// <returns>Collection of positioned and oriented stone instances.</returns>
    public static IReadOnlyList<PaveStonePlacement> CalculatePavePlacements(
        ISurface3D surface,
        double uMin,
        double uMax,
        double vMin,
        double vMax,
        PaveParameters parameters)
    {
        if (surface == null) throw new ArgumentNullException(nameof(surface));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        if (uMin > uMax) (uMin, uMax) = (uMax, uMin);
        if (vMin > vMax) (vMin, vMax) = (vMax, vMin);

        double diameter = parameters.StoneDiameterMm;
        double spacing = parameters.StoneSpacingMm;
        double borderOffset = parameters.BorderOffsetMm;
        double targetCenterDistance = diameter + spacing;
        double borderMargin = (diameter * 0.5) + borderOffset;

        // Metric scale evaluation at domain center
        double uMid = (uMin + uMax) * 0.5;
        double vMid = (vMin + vMax) * 0.5;
        var (suMid, svMid) = EvaluateDerivatives(surface, uMid, vMid);
        double ku = Math.Max(suMid.Length, 1e-4);
        double kv = Math.Max(svMid.Length, 1e-4);

        double marginU = borderMargin / ku;
        double marginV = borderMargin / kv;

        double uLow = uMin + marginU;
        double uHigh = uMax - marginU;
        double vLow = vMin + marginV;
        double vHigh = vMax - marginV;

        if (uHigh < uLow || vHigh < vLow)
        {
            return Array.Empty<PaveStonePlacement>();
        }

        double stepU = targetCenterDistance / ku;
        double stepV = targetCenterDistance / kv;

        double deltaU = stepU;
        double deltaV = stepV;
        bool allowShift = false;

        switch (parameters.PatternType)
        {
            case PavePatternType.Hexagonal:
                // Equilateral triangular grid: row spacing = target * sqrt(3)/2
                deltaV = (targetCenterDistance * (Math.Sqrt(3.0) * 0.5)) / kv;
                deltaU = stepU;
                allowShift = true;
                break;

            case PavePatternType.OffsetLinear:
                deltaV = stepV;
                deltaU = stepU;
                allowShift = true;
                break;

            case PavePatternType.Grid:
                deltaV = stepV;
                deltaU = stepU;
                allowShift = false;
                break;

            case PavePatternType.Fishtail:
                deltaV = (targetCenterDistance * (Math.Sqrt(3.0) * 0.5)) / kv;
                deltaU = stepU;
                allowShift = true;
                break;
        }

        double vSpan = vHigh - vLow;
        int rowCount = (int)Math.Floor((vSpan + 1e-9) / deltaV) + 1;
        if (rowCount < 1) rowCount = 1;

        double uSpan = uHigh - uLow;
        var candidateCoords = new List<(double U, double V)>();

        for (int r = 0; r < rowCount; r++)
        {
            double v = vLow + (r * deltaV);
            if (v > vHigh + 1e-9) break;

            bool isShifted = allowShift && (r % 2 == 1);
            double shift = isShifted ? (0.5 * deltaU) : 0.0;
            double uFirst = uLow + shift;
            if (uFirst > uHigh) continue;

            int colCount = (int)Math.Floor((uHigh - uFirst + 1e-9) / deltaU) + 1;
            for (int c = 0; c < colCount; c++)
            {
                double u = uFirst + (c * deltaU);
                if (u >= uLow - 1e-9 && u <= uHigh + 1e-9)
                {
                    candidateCoords.Add((u, v));
                }
            }
        }

        if (candidateCoords.Count == 0)
        {
            return Array.Empty<PaveStonePlacement>();
        }

        // Apply Lloyd's relaxation on surface
        var relaxedCoords = RelaxStonePositions(
            surface,
            candidateCoords,
            uLow,
            uHigh,
            vLow,
            vHigh,
            targetCenterDistance,
            parameters.RelaxationIterations);

        // Strict non-collision guarantee: min center distance >= targetCenterDistance - 1e-4
        var resolvedCoords = ResolveCollisions(
            surface,
            relaxedCoords,
            uLow,
            uHigh,
            vLow,
            vHigh,
            targetCenterDistance - 1e-4);

        // Build result placements
        var placements = new List<PaveStonePlacement>(resolvedCoords.Count);
        for (int i = 0; i < resolvedCoords.Count; i++)
        {
            var (u, v) = resolvedCoords[i];
            var center = surface.PointAt(u, v);
            var normal = surface.NormalAt(u, v);
            if (normal.IsZero()) normal = Vector3D.UnitZ;
            else normal = normal.Normalize();

            var tangent = surface.TangentAt(u, v);
            if (tangent.IsZero() || Math.Abs(tangent.Dot(normal)) > 0.999)
            {
                tangent = normal.Cross(Vector3D.UnitX);
                if (tangent.IsZero()) tangent = normal.Cross(Vector3D.UnitY);
            }
            tangent = tangent.Normalize();

            var frame = OrthonormalFrame.Create(tangent, normal);
            var transform = Matrix4x4D.CreatePlacement(frame, center, diameter);

            placements.Add(new PaveStonePlacement
            {
                Index = i,
                Center = center,
                Normal = frame.Normal,
                Tangent = frame.Tangent,
                Transform = transform,
                DiameterMm = diameter,
                U = u,
                V = v
            });
        }

        return placements;
    }

    /// <summary>
    /// Calculates micro-bead retention grain positions at interstitial centroids and outer perimeter edges.
    /// </summary>
    /// <param name="stones">Placed stone instances.</param>
    /// <param name="parameters">Parametric pavé configuration.</param>
    /// <returns>Collection of positioned retention beads.</returns>
    public static IReadOnlyList<PaveBeadPlacement> CalculateBeadPlacements(
        IReadOnlyList<PaveStonePlacement> stones,
        PaveParameters parameters)
    {
        if (stones == null || stones.Count == 0)
        {
            return Array.Empty<PaveBeadPlacement>();
        }
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        parameters.Validate();

        double beadDiameter = parameters.BeadDiameterMm;
        double beadRadius = beadDiameter * 0.5;
        double lift = Math.Max(0.05, parameters.BeadHeightMm - beadRadius);
        double targetCenterDistance = parameters.StoneDiameterMm + parameters.StoneSpacingMm;
        double minCenterToStone = parameters.StoneDiameterMm * 0.5;

        var beadCandidates = new List<PaveBeadPlacement>();

        if (stones.Count == 1)
        {
            var s = stones[0];
            var frame = OrthonormalFrame.Create(s.Tangent, s.Normal);
            double r = (s.DiameterMm * 0.5) + (parameters.StoneSpacingMm * 0.5);
            double[] angles = { Math.PI * 0.25, Math.PI * 0.75, Math.PI * 1.25, Math.PI * 1.75 };
            foreach (double a in angles)
            {
                var offset = (frame.Binormal * Math.Cos(a)) + (frame.Tangent * Math.Sin(a));
                var center = s.Center + (offset * r) + (frame.Normal * lift);
                beadCandidates.Add(new PaveBeadPlacement
                {
                    Center = center,
                    Normal = frame.Normal,
                    DiameterMm = beadDiameter,
                    AssociatedStoneIndices = new List<int> { 0 }
                });
            }
            return beadCandidates;
        }

        // Triangulate stone centers in (u, v) parameter domain
        var triangles = TriangulateDelaunay2D(stones);

        if (triangles.Count == 0)
        {
            // Collinear stones fallback (e.g. 1D strip)
            for (int i = 0; i < stones.Count - 1; i++)
            {
                var s1 = stones[i];
                var s2 = stones[i + 1];
                var mid = MidPoint(s1.Center, s2.Center);
                var edge = s2.Center - s1.Center;
                var avgNormal = (s1.Normal + s2.Normal).Normalize();
                var lat = avgNormal.Cross(edge);
                if (!lat.IsZero()) lat = lat.Normalize();
                else lat = s1.Tangent;

                double latOffset = (s1.DiameterMm * 0.5) + (parameters.StoneSpacingMm * 0.5);
                beadCandidates.Add(new PaveBeadPlacement
                {
                    Center = mid + (lat * latOffset) + (avgNormal * lift),
                    Normal = avgNormal,
                    DiameterMm = beadDiameter,
                    AssociatedStoneIndices = new List<int> { s1.Index, s2.Index }
                });
                beadCandidates.Add(new PaveBeadPlacement
                {
                    Center = mid - (lat * latOffset) + (avgNormal * lift),
                    Normal = avgNormal,
                    DiameterMm = beadDiameter,
                    AssociatedStoneIndices = new List<int> { s1.Index, s2.Index }
                });
            }
            return FilterCollisions(beadCandidates, stones, minCenterToStone);
        }

        // 1. Interstitial centroids between 3 stones
        double maxInterstitialEdge = targetCenterDistance * 1.85;
        var validTriangles = new List<(int A, int B, int C)>();

        foreach (var (i, j, k) in triangles)
        {
            var pI = stones[i].Center;
            var pJ = stones[j].Center;
            var pK = stones[k].Center;

            double dIJ = pI.DistanceTo(pJ);
            double dJK = pJ.DistanceTo(pK);
            double dKI = pK.DistanceTo(pI);

            if (dIJ <= maxInterstitialEdge && dJK <= maxInterstitialEdge && dKI <= maxInterstitialEdge)
            {
                validTriangles.Add((i, j, k));
                var centroid = Centroid(pI, pJ, pK);
                var avgNormal = (stones[i].Normal + stones[j].Normal + stones[k].Normal) * (1.0 / 3.0);
                if (avgNormal.IsZero()) avgNormal = stones[i].Normal;
                else avgNormal = avgNormal.Normalize();

                var beadCenter = centroid + (avgNormal * lift);
                beadCandidates.Add(new PaveBeadPlacement
                {
                    Center = beadCenter,
                    Normal = avgNormal,
                    DiameterMm = beadDiameter,
                    AssociatedStoneIndices = new List<int> { stones[i].Index, stones[j].Index, stones[k].Index }
                });
            }
        }

        // Merge interstitial beads that are too close (e.g. quad splits)
        var mergedBeads = MergeNearbyBeads(beadCandidates, targetCenterDistance * 0.45);

        // 2. Perimeter beads along exposed outer boundary edges
        var edgeCount = new Dictionary<(int, int), (int OtherVertex, int TriangleCount)>();
        foreach (var (i, j, k) in validTriangles)
        {
            RegisterEdge(edgeCount, i, j, k);
            RegisterEdge(edgeCount, j, k, i);
            RegisterEdge(edgeCount, k, i, j);
        }

        foreach (var kvp in edgeCount)
        {
            if (kvp.Value.TriangleCount == 1)
            {
                int u = kvp.Key.Item1;
                int v = kvp.Key.Item2;
                int opp = kvp.Value.OtherVertex;

                var pU = stones[u].Center;
                var pV = stones[v].Center;
                var pOpp = stones[opp].Center;

                if (pU.DistanceTo(pV) <= maxInterstitialEdge)
                {
                    var mid = MidPoint(pU, pV);
                    var normal = (stones[u].Normal + stones[v].Normal).Normalize();
                    var away = mid - pOpp;
                    var tangentAway = away - (normal * away.Dot(normal));
                    if (!tangentAway.IsZero())
                    {
                        var outDir = tangentAway.Normalize();
                        double outDist = (parameters.StoneDiameterMm * 0.30) + (parameters.StoneSpacingMm * 0.5);
                        var center = mid + (outDir * outDist) + (normal * lift);

                        mergedBeads.Add(new PaveBeadPlacement
                        {
                            Center = center,
                            Normal = normal,
                            DiameterMm = beadDiameter,
                            AssociatedStoneIndices = new List<int> { stones[u].Index, stones[v].Index }
                        });
                    }
                }
            }
        }

        return FilterCollisions(mergedBeads, stones, minCenterToStone);
    }

    /// <summary>
    /// Generates a single closed, watertight 2-manifold spherical bead mesh (genus 0, Euler characteristic chi = 2).
    /// </summary>
    public static GemMesh CreateBeadMesh(PaveBeadPlacement bead)
    {
        if (bead == null) throw new ArgumentNullException(nameof(bead));

        double radius = bead.DiameterMm * 0.5;
        var center = bead.Center;
        var normal = bead.Normal.IsZero() ? Vector3D.UnitZ : bead.Normal.Normalize();

        Vector3D uDir;
        if (Math.Abs(normal.Z) < 0.999)
        {
            uDir = new Vector3D(0, 0, 1).Cross(normal).Normalize();
        }
        else
        {
            uDir = Vector3D.UnitX;
        }
        var vDir = normal.Cross(uDir).Normalize();

        int rings = SphereLatitudeRings;
        int sectors = SphereLongitudeSectors;
        int vertexCount = 2 + ((rings - 1) * sectors);
        var vertices = new List<Point3D>(vertexCount);

        // Vertex 0: North pole
        vertices.Add(center + (normal * radius));

        // Intermediate rings
        for (int r = 1; r < rings; r++)
        {
            double phi = Math.PI * ((double)r / rings);
            double z = radius * Math.Cos(phi);
            double rxy = radius * Math.Sin(phi);

            for (int s = 0; s < sectors; s++)
            {
                double theta = 2.0 * Math.PI * ((double)s / sectors);
                double x = rxy * Math.Cos(theta);
                double y = rxy * Math.Sin(theta);

                vertices.Add(center + (uDir * x) + (vDir * y) + (normal * z));
            }
        }

        // South pole
        int southPoleIndex = vertices.Count;
        vertices.Add(center - (normal * radius));

        var faces = new List<int[]>();

        // Top cap triangles
        for (int s = 0; s < sectors; s++)
        {
            int sNext = (s + 1) % sectors;
            int v1 = 1 + s;
            int v2 = 1 + sNext;
            faces.Add(new[] { 0, v1, v2 });
        }

        // Middle quad bands
        for (int r = 1; r < rings - 1; r++)
        {
            int ringStart = 1 + ((r - 1) * sectors);
            int nextRingStart = 1 + (r * sectors);

            for (int s = 0; s < sectors; s++)
            {
                int sNext = (s + 1) % sectors;
                int i00 = ringStart + s;
                int i01 = ringStart + sNext;
                int i10 = nextRingStart + s;
                int i11 = nextRingStart + sNext;

                faces.Add(new[] { i01, i00, i10, i11 });
            }
        }

        // Bottom cap triangles
        int lastRingStart = 1 + ((rings - 2) * sectors);
        for (int s = 0; s < sectors; s++)
        {
            int sNext = (s + 1) % sectors;
            int vBot = lastRingStart + s;
            int vBotNext = lastRingStart + sNext;
            faces.Add(new[] { vBotNext, vBot, southPoleIndex });
        }

        var normals = new Vector3D[faces.Count];
        for (int f = 0; f < faces.Count; f++)
        {
            normals[f] = GemMesh.ComputeFaceNormal(vertices, faces[f]);
        }

        return new GemMesh
        {
            CutType = GemCutType.RoundBrilliant,
            DiameterMm = bead.DiameterMm,
            Vertices = vertices,
            Faces = faces,
            Normals = normals
        };
    }

    /// <summary>
    /// Generates composite closed mesh solid containing all retention beads.
    /// </summary>
    public static GemMesh CreateBeadMesh(IReadOnlyList<PaveBeadPlacement> beads)
    {
        if (beads == null || beads.Count == 0)
        {
            return new GemMesh();
        }

        if (beads.Count == 1)
        {
            return CreateBeadMesh(beads[0]);
        }

        var allVertices = new List<Point3D>();
        var allFaces = new List<int[]>();

        foreach (var bead in beads)
        {
            var singleMesh = CreateBeadMesh(bead);
            int offset = allVertices.Count;
            allVertices.AddRange(singleMesh.Vertices);

            foreach (var face in singleMesh.Faces)
            {
                var reindexed = new int[face.Length];
                for (int i = 0; i < face.Length; i++)
                {
                    reindexed[i] = face[i] + offset;
                }
                allFaces.Add(reindexed);
            }
        }

        var normals = new Vector3D[allFaces.Count];
        for (int f = 0; f < allFaces.Count; f++)
        {
            normals[f] = GemMesh.ComputeFaceNormal(allVertices, allFaces[f]);
        }

        return new GemMesh
        {
            CutType = GemCutType.RoundBrilliant,
            DiameterMm = beads[0].DiameterMm,
            Vertices = allVertices,
            Faces = allFaces,
            Normals = normals
        };
    }

    /// <summary>
    /// Generates individual closed 2-manifold bead meshes for each bead placement.
    /// </summary>
    public static IReadOnlyList<GemMesh> CreateBeadMeshes(IReadOnlyList<PaveBeadPlacement> beads)
    {
        if (beads == null || beads.Count == 0)
        {
            return Array.Empty<GemMesh>();
        }

        var meshes = new List<GemMesh>(beads.Count);
        foreach (var b in beads)
        {
            meshes.Add(CreateBeadMesh(b));
        }
        return meshes;
    }

    /// <summary>
    /// Generates U-cut scalloped festoon cutters along the lateral edges of a pavé strip.
    /// </summary>
    public static GemMesh CreateFishtailCutterMesh(
        IReadOnlyList<PaveStonePlacement> stones,
        PaveParameters parameters)
    {
        if (stones == null || stones.Count == 0)
        {
            return new GemMesh();
        }
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        double cutterWidth = parameters.StoneDiameterMm * 0.60;
        double cutterDepth = parameters.StoneDiameterMm * 0.50;
        double cutterHeight = parameters.StoneDiameterMm * 0.80;

        return CreateFishtailCutterMesh(stones, parameters, cutterWidth, cutterDepth, cutterHeight);
    }

    /// <summary>
    /// Generates U-cut scalloped festoon cutters along the lateral edges of a pavé strip with custom dimensions.
    /// </summary>
    public static GemMesh CreateFishtailCutterMesh(
        IReadOnlyList<PaveStonePlacement> stones,
        PaveParameters parameters,
        double cutterWidthMm,
        double cutterDepthMm,
        double cutterHeightMm)
    {
        if (stones == null || stones.Count == 0)
        {
            return new GemMesh();
        }

        var allVertices = new List<Point3D>();
        var allFaces = new List<int[]>();

        // Sort stones along U
        var sortedStones = stones.OrderBy(s => s.U).ToList();
        double lateralOffset = (parameters.StoneDiameterMm * 0.5) + parameters.BorderOffsetMm;

        for (int i = 0; i < sortedStones.Count; i++)
        {
            var stone = sortedStones[i];
            var frame = OrthonormalFrame.Create(stone.Tangent, stone.Normal);

            // Left lateral scallop
            var leftCenter = stone.Center - (frame.Binormal * lateralOffset);
            AddBoxSolid(allVertices, allFaces, leftCenter, cutterWidthMm, cutterDepthMm, cutterHeightMm, frame.Tangent, frame.Binormal, frame.Normal);

            // Right lateral scallop
            var rightCenter = stone.Center + (frame.Binormal * lateralOffset);
            AddBoxSolid(allVertices, allFaces, rightCenter, cutterWidthMm, cutterDepthMm, cutterHeightMm, frame.Tangent, frame.Binormal, frame.Normal);
        }

        var normals = new Vector3D[allFaces.Count];
        for (int f = 0; f < allFaces.Count; f++)
        {
            normals[f] = GemMesh.ComputeFaceNormal(allVertices, allFaces[f]);
        }

        return new GemMesh
        {
            CutType = GemCutType.RoundBrilliant,
            DiameterMm = parameters.StoneDiameterMm,
            Vertices = allVertices,
            Faces = allFaces,
            Normals = normals
        };
    }

    #region RhinoCommon Native Wrappers

    /// <summary>
    /// Dual execution RhinoCommon wrapper evaluating stone placements across a native BrepFace.
    /// </summary>
    public static IReadOnlyList<PaveStonePlacement> CalculatePavePlacements(
        BrepFace face,
        PaveParameters parameters)
    {
        if (face == null) throw new ArgumentNullException(nameof(face));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return Array.Empty<PaveStonePlacement>();
        }

        var uDom = face.Domain(0);
        var vDom = face.Domain(1);
        return CalculatePavePlacements(face, uDom.Min, uDom.Max, vDom.Min, vDom.Max, parameters);
    }

    /// <summary>
    /// Dual execution RhinoCommon wrapper evaluating stone placements across a native BrepFace domain.
    /// </summary>
    public static IReadOnlyList<PaveStonePlacement> CalculatePavePlacements(
        BrepFace face,
        double uMin,
        double uMax,
        double vMin,
        double vMax,
        PaveParameters parameters)
    {
        if (face == null) throw new ArgumentNullException(nameof(face));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return Array.Empty<PaveStonePlacement>();
        }

        var surfaceAdapter = new RhinoBrepFaceSurface3D(face);
        return CalculatePavePlacements(surfaceAdapter, uMin, uMax, vMin, vMax, parameters);
    }

    /// <summary>
    /// Dual execution RhinoCommon wrapper creating solid Brep spheres for all retention beads.
    /// </summary>
    public static IReadOnlyList<Brep>? CreateBeadBreps(IReadOnlyList<PaveBeadPlacement> beads)
    {
        if (beads == null) throw new ArgumentNullException(nameof(beads));
        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var breps = new List<Brep>(beads.Count);
        foreach (var b in beads)
        {
            var sphere = new Sphere(new Point3d(b.Center.X, b.Center.Y, b.Center.Z), b.DiameterMm * 0.5);
            var brep = sphere.ToBrep();
            if (brep != null)
            {
                breps.Add(brep);
            }
        }
        return breps;
    }

    private sealed class RhinoBrepFaceSurface3D : ISurface3D
    {
        private readonly BrepFace _face;

        public RhinoBrepFaceSurface3D(BrepFace face)
        {
            _face = face;
        }

        public Vector3D NormalAtPoint(Point3D point)
        {
            var pt = new Point3d(point.X, point.Y, point.Z);
            if (_face.ClosestPoint(pt, out double u, out double v))
            {
                var n = _face.NormalAt(u, v);
                return new Vector3D(n.X, n.Y, n.Z).Normalize();
            }
            return Vector3D.UnitZ;
        }

        public Point3D PointAt(double u, double v)
        {
            var pt = _face.PointAt(u, v);
            return new Point3D(pt.X, pt.Y, pt.Z);
        }

        public Vector3D NormalAt(double u, double v)
        {
            var n = _face.NormalAt(u, v);
            return new Vector3D(n.X, n.Y, n.Z).Normalize();
        }

        public Vector3D TangentAt(double u, double v)
        {
            if (_face.Evaluate(u, v, 1, out _, out var derivs) && derivs != null && derivs.Length > 0)
            {
                var du = derivs[0];
                var t = new Vector3D(du.X, du.Y, du.Z);
                if (!t.IsZero()) return t.Normalize();
            }
            var normal = NormalAt(u, v);
            var fallback = normal.Cross(Vector3D.UnitX);
            if (fallback.IsZero()) fallback = normal.Cross(Vector3D.UnitY);
            return fallback.Normalize();
        }
    }

    #endregion

    #region Internal Geometric Helper Methods

    private static Point3D MidPoint(Point3D a, Point3D b) =>
        new Point3D((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5, (a.Z + b.Z) * 0.5);

    private static Point3D Centroid(Point3D a, Point3D b, Point3D c) =>
        new Point3D((a.X + b.X + c.X) / 3.0, (a.Y + b.Y + c.Y) / 3.0, (a.Z + b.Z) / 3.0);

    private static (Vector3D Su, Vector3D Sv) EvaluateDerivatives(ISurface3D surface, double u, double v)
    {
        const double h = 1e-4;
        var p0 = surface.PointAt(u, v);
        var pu = surface.PointAt(u + h, v);
        var pv = surface.PointAt(u, v + h);
        var su = (pu - p0) * (1.0 / h);
        var sv = (pv - p0) * (1.0 / h);
        return (su, sv);
    }

    private static List<(double U, double V)> RelaxStonePositions(
        ISurface3D surface,
        List<(double U, double V)> initialCoords,
        double uLow,
        double uHigh,
        double vLow,
        double vHigh,
        double targetDistance,
        int iterations)
    {
        if (iterations <= 0 || initialCoords.Count <= 1)
        {
            return new List<(double U, double V)>(initialCoords);
        }

        var coords = initialCoords.Select(c => (c.U, c.V)).ToList();
        int n = coords.Count;

        for (int iter = 0; iter < iterations; iter++)
        {
            var points3D = new Point3D[n];
            for (int i = 0; i < n; i++)
            {
                points3D[i] = surface.PointAt(coords[i].U, coords[i].V);
            }

            var deltaUV = new (double du, double dv)[n];

            for (int i = 0; i < n; i++)
            {
                var f3D = Vector3D.Zero;
                int neighbors = 0;

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    double dist = points3D[i].DistanceTo(points3D[j]);
                    if (dist < targetDistance && dist > 1e-9)
                    {
                        double overlap = targetDistance - dist;
                        var dir = (points3D[i] - points3D[j]) * (1.0 / dist);
                        f3D += dir * (overlap * 0.5);
                        neighbors++;
                    }
                }

                if (neighbors > 0)
                {
                    var (su, sv) = EvaluateDerivatives(surface, coords[i].U, coords[i].V);
                    double lenSuSq = su.LengthSquared;
                    double lenSvSq = sv.LengthSquared;

                    if (lenSuSq > 1e-12 && lenSvSq > 1e-12)
                    {
                        double du = f3D.Dot(su) / lenSuSq;
                        double dv = f3D.Dot(sv) / lenSvSq;

                        // Damped displacement to ensure stability
                        double maxStep = 0.25 * targetDistance;
                        du = Math.Clamp(du, -maxStep, maxStep);
                        dv = Math.Clamp(dv, -maxStep, maxStep);

                        deltaUV[i] = (du, dv);
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                double uNew = Math.Clamp(coords[i].U + deltaUV[i].du, uLow, uHigh);
                double vNew = Math.Clamp(coords[i].V + deltaUV[i].dv, vLow, vHigh);
                coords[i] = (uNew, vNew);
            }
        }

        return coords;
    }

    private static List<(double U, double V)> ResolveCollisions(
        ISurface3D surface,
        List<(double U, double V)> coords,
        double uLow,
        double uHigh,
        double vLow,
        double vHigh,
        double minAllowedDistance)
    {
        var result = new List<(double U, double V)>(coords.Count);
        var resultPoints = new List<Point3D>(coords.Count);

        for (int i = 0; i < coords.Count; i++)
        {
            var pt = surface.PointAt(coords[i].U, coords[i].V);
            bool collides = false;

            for (int j = 0; j < resultPoints.Count; j++)
            {
                if (pt.DistanceTo(resultPoints[j]) < minAllowedDistance)
                {
                    collides = true;
                    break;
                }
            }

            if (!collides)
            {
                result.Add(coords[i]);
                resultPoints.Add(pt);
            }
        }

        return result;
    }

    private static List<(int A, int B, int C)> TriangulateDelaunay2D(IReadOnlyList<PaveStonePlacement> stones)
    {
        int n = stones.Count;
        if (n < 3) return new List<(int, int, int)>();

        double minU = double.MaxValue, maxU = double.MinValue;
        double minV = double.MaxValue, maxV = double.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (stones[i].U < minU) minU = stones[i].U;
            if (stones[i].U > maxU) maxU = stones[i].U;
            if (stones[i].V < minV) minV = stones[i].V;
            if (stones[i].V > maxV) maxV = stones[i].V;
        }

        double dx = maxU - minU;
        double dy = maxV - minV;
        double dMax = Math.Max(Math.Max(dx, dy), 1.0) * 100.0;
        double midU = (minU + maxU) * 0.5;
        double midV = (minV + maxV) * 0.5;

        // Super-triangle vertices
        var pts = new List<(double U, double V)>(n + 3);
        for (int i = 0; i < n; i++) pts.Add((stones[i].U, stones[i].V));

        int st0 = n;
        int st1 = n + 1;
        int st2 = n + 2;
        pts.Add((midU - (2.0 * dMax), midV - dMax));
        pts.Add((midU, midV + (2.0 * dMax)));
        pts.Add((midU + (2.0 * dMax), midV - dMax));

        var triangles = new List<(int A, int B, int C)> { (st0, st1, st2) };

        for (int i = 0; i < n; i++)
        {
            var p = pts[i];
            var badTriangles = new List<(int A, int B, int C)>();

            foreach (var tri in triangles)
            {
                if (InCircumcircle(p, pts[tri.A], pts[tri.B], pts[tri.C]))
                {
                    badTriangles.Add(tri);
                }
            }

            // Find boundary of polygonal hole
            var polygon = new List<(int From, int To)>();
            for (int j = 0; j < badTriangles.Count; j++)
            {
                var t = badTriangles[j];
                AddOrRemoveEdge(polygon, t.A, t.B);
                AddOrRemoveEdge(polygon, t.B, t.C);
                AddOrRemoveEdge(polygon, t.C, t.A);
            }

            foreach (var bad in badTriangles)
            {
                triangles.Remove(bad);
            }

            foreach (var edge in polygon)
            {
                triangles.Add((edge.From, edge.To, i));
            }
        }

        // Remove triangles that share super-triangle vertices
        var validTriangles = new List<(int A, int B, int C)>();
        foreach (var t in triangles)
        {
            if (t.A < n && t.B < n && t.C < n)
            {
                validTriangles.Add(t);
            }
        }

        return validTriangles;
    }

    private static bool InCircumcircle(
        (double U, double V) p,
        (double U, double V) a,
        (double U, double V) b,
        (double U, double V) c)
    {
        double ax = a.U - p.U;
        double ay = a.V - p.V;
        double bx = b.U - p.U;
        double by = b.V - p.V;
        double cx = c.U - p.U;
        double cy = c.V - p.V;

        double det = ((ax * ax + ay * ay) * (bx * cy - cx * by))
                   - ((bx * bx + by * by) * (ax * cy - cx * ay))
                   + ((cx * cx + cy * cy) * (ax * by - bx * ay));

        // CCW orientation check
        double ccw = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        return ccw > 0 ? det > 1e-12 : det < -1e-12;
    }

    private static void AddOrRemoveEdge(List<(int From, int To)> edges, int v1, int v2)
    {
        int existing = edges.FindIndex(e => (e.From == v1 && e.To == v2) || (e.From == v2 && e.To == v1));
        if (existing >= 0)
        {
            edges.RemoveAt(existing);
        }
        else
        {
            edges.Add((v1, v2));
        }
    }

    private static void RegisterEdge(Dictionary<(int, int), (int OtherVertex, int TriangleCount)> edgeMap, int u, int v, int opp)
    {
        var key = u < v ? (u, v) : (v, u);
        if (edgeMap.TryGetValue(key, out var val))
        {
            edgeMap[key] = (opp, val.TriangleCount + 1);
        }
        else
        {
            edgeMap[key] = (opp, 1);
        }
    }

    private static List<PaveBeadPlacement> MergeNearbyBeads(List<PaveBeadPlacement> beads, double threshold)
    {
        var merged = new List<PaveBeadPlacement>();
        var visited = new bool[beads.Count];

        for (int i = 0; i < beads.Count; i++)
        {
            if (visited[i]) continue;
            visited[i] = true;

            var cluster = new List<PaveBeadPlacement> { beads[i] };
            for (int j = i + 1; j < beads.Count; j++)
            {
                if (!visited[j] && beads[i].Center.DistanceTo(beads[j].Center) < threshold)
                {
                    visited[j] = true;
                    cluster.Add(beads[j]);
                }
            }

            if (cluster.Count == 1)
            {
                merged.Add(cluster[0]);
            }
            else
            {
                double cx = cluster.Average(b => b.Center.X);
                double cy = cluster.Average(b => b.Center.Y);
                double cz = cluster.Average(b => b.Center.Z);
                var avgNormal = cluster.Aggregate(Vector3D.Zero, (acc, b) => acc + b.Normal).Normalize();
                var stoneIndices = cluster.SelectMany(b => b.AssociatedStoneIndices).Distinct().ToList();

                merged.Add(new PaveBeadPlacement
                {
                    Center = new Point3D(cx, cy, cz),
                    Normal = avgNormal,
                    DiameterMm = cluster[0].DiameterMm,
                    AssociatedStoneIndices = stoneIndices
                });
            }
        }

        return merged;
    }

    private static List<PaveBeadPlacement> FilterCollisions(
        List<PaveBeadPlacement> beads,
        IReadOnlyList<PaveStonePlacement> stones,
        double minDistance)
    {
        var valid = new List<PaveBeadPlacement>(beads.Count);
        foreach (var bead in beads)
        {
            bool collides = false;
            foreach (var stone in stones)
            {
                if (bead.Center.DistanceTo(stone.Center) < minDistance - 1e-4)
                {
                    collides = true;
                    break;
                }
            }

            if (!collides)
            {
                valid.Add(bead);
            }
        }
        return valid;
    }

    private static void AddBoxSolid(
        List<Point3D> vertices,
        List<int[]> faces,
        Point3D center,
        double width,
        double depth,
        double height,
        Vector3D dirX,
        Vector3D dirY,
        Vector3D dirZ)
    {
        double hx = width * 0.5;
        double hy = depth * 0.5;
        double hz = height * 0.5;

        int offset = vertices.Count;

        // 8 Vertices
        vertices.Add(center - (dirX * hx) - (dirY * hy) - (dirZ * hz)); // 0: ---
        vertices.Add(center + (dirX * hx) - (dirY * hy) - (dirZ * hz)); // 1: +--
        vertices.Add(center + (dirX * hx) + (dirY * hy) - (dirZ * hz)); // 2: ++-
        vertices.Add(center - (dirX * hx) + (dirY * hy) - (dirZ * hz)); // 3: -+-
        vertices.Add(center - (dirX * hx) - (dirY * hy) + (dirZ * hz)); // 4: --+
        vertices.Add(center + (dirX * hx) - (dirY * hy) + (dirZ * hz)); // 5: +-+
        vertices.Add(center + (dirX * hx) + (dirY * hy) + (dirZ * hz)); // 6: +++
        vertices.Add(center - (dirX * hx) + (dirY * hy) + (dirZ * hz)); // 7: -++

        // 6 Quads with strictly opposite edge windings (Euler chi = 2)
        faces.Add(new[] { offset + 3, offset + 2, offset + 1, offset + 0 }); // Bottom (-Z)
        faces.Add(new[] { offset + 4, offset + 5, offset + 6, offset + 7 }); // Top (+Z)
        faces.Add(new[] { offset + 0, offset + 1, offset + 5, offset + 4 }); // Front (-Y)
        faces.Add(new[] { offset + 1, offset + 2, offset + 6, offset + 5 }); // Right (+X)
        faces.Add(new[] { offset + 2, offset + 3, offset + 7, offset + 6 }); // Back (+Y)
        faces.Add(new[] { offset + 3, offset + 0, offset + 4, offset + 7 }); // Left (-X)
    }

    #endregion
}
