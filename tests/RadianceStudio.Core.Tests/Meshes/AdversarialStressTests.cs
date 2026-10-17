namespace RadianceStudio.Core.Tests.Meshes;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Xunit;

public class AdversarialStressTests
{
    private static readonly GemCutType[] AllCuts =
    [
        GemCutType.RoundBrilliant,
        GemCutType.Princess,
        GemCutType.Oval,
        GemCutType.Pear,
        GemCutType.Emerald,
        GemCutType.Marquise
    ];

    #region 1. Extreme Diameters & Boundary Probing

    [Theory]
    [InlineData(0.001)]    // 1 micrometer
    [InlineData(0.01)]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(100.0)]
    [InlineData(1000.0)]   // 1 meter diameter gem
    [InlineData(1e-5)]
    [InlineData(1e5)]
    public void AllCuts_GenerateValidFiniteGeometry_AcrossExtremeScales(double diameterMm)
    {
        foreach (var cut in AllCuts)
        {
            var mesh = GemMeshFactory.Create(cut, diameterMm);

            Assert.Equal(cut, mesh.CutType);
            Assert.Equal(diameterMm, mesh.DiameterMm);
            Assert.NotEmpty(mesh.Vertices);
            Assert.NotEmpty(mesh.Faces);
            Assert.NotEmpty(mesh.Normals);

            // Verify all vertices are finite and not NaN
            foreach (var v in mesh.Vertices)
            {
                Assert.False(double.IsNaN(v.X) || double.IsInfinity(v.X), $"NaN/Inf X at cut {cut}, scale {diameterMm}");
                Assert.False(double.IsNaN(v.Y) || double.IsInfinity(v.Y), $"NaN/Inf Y at cut {cut}, scale {diameterMm}");
                Assert.False(double.IsNaN(v.Z) || double.IsInfinity(v.Z), $"NaN/Inf Z at cut {cut}, scale {diameterMm}");
            }

            // Verify all normals are unit length and finite
            foreach (var n in mesh.Normals)
            {
                Assert.False(double.IsNaN(n.X) || double.IsInfinity(n.X));
                Assert.False(double.IsNaN(n.Y) || double.IsInfinity(n.Y));
                Assert.False(double.IsNaN(n.Z) || double.IsInfinity(n.Z));
                Assert.True(Math.Abs(n.Length - 1.0) < 1e-4, $"Normal length {n.Length} deviated from 1.0 at cut {cut}");
            }

            // Bounds check
            var (min, max) = mesh.GetBounds();
            Assert.True(max.X > min.X);
            Assert.True(max.Y > min.Y);
            Assert.True(max.Z > min.Z);
            Assert.True(min.Z < 0, "Culet must be below girdle plane (Z < 0)");
            Assert.True(max.Z > 0, "Table must be above girdle plane (Z > 0)");
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.0001)]
    [InlineData(-1.0)]
    [InlineData(-1000.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AllCuts_ThrowExpectedExceptions_OnZeroNegativeOrNonFiniteDiameters(double invalidDiameter)
    {
        foreach (var cut in AllCuts)
        {
            Assert.ThrowsAny<ArgumentException>(() => GemMeshFactory.Create(cut, invalidDiameter));
        }
    }

    #endregion

    #region 2. Mesh Watertightness & 2-Manifold Euler Characteristic

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    public void Mesh_IsStrictly2Manifold_AndSatisfiesEulerCharacteristic(GemCutType cutType)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        var vertices = mesh.Vertices;
        var faces = mesh.Faces;

        int V = vertices.Count;
        int F = faces.Count;

        // Map each undirected edge to list of (FaceIndex, DirectedEdge)
        var edgeMap = new Dictionary<(int, int), List<(int faceIdx, int from, int to)>>();

        for (int f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            int n = face.Length;
            Assert.True(n >= 3, $"Face {f} has fewer than 3 vertices.");

            for (int i = 0; i < n; i++)
            {
                int u = face[i];
                int v = face[(i + 1) % n];

                Assert.True(u >= 0 && u < V, $"Vertex index {u} out of range in face {f}");
                Assert.True(v >= 0 && v < V, $"Vertex index {v} out of range in face {f}");
                Assert.NotEqual(u, v); // no self-loop edge

                var canonicalEdge = u < v ? (u, v) : (v, u);
                if (!edgeMap.TryGetValue(canonicalEdge, out var occurrences))
                {
                    occurrences = new List<(int, int, int)>();
                    edgeMap[canonicalEdge] = occurrences;
                }
                occurrences.Add((f, u, v));
            }
        }

        int E = edgeMap.Count;

        // Verify Euler characteristic for closed surface of genus 0 (sphere topology):
        // chi = V - E + F == 2
        int chi = V - E + F;
        Assert.True(chi == 2, $"Cut {cutType}: Euler characteristic chi = V({V}) - E({E}) + F({F}) = {chi}, expected 2");

        // Verify 2-manifold edge property:
        // Every undirected edge must belong to EXACTLY TWO faces, with opposite directed orientation
        foreach (var kvp in edgeMap)
        {
            var (edge, occurrences) = (kvp.Key, kvp.Value);
            Assert.True(occurrences.Count == 2,
                $"Cut {cutType}: Edge ({edge.Item1}, {edge.Item2}) is shared by {occurrences.Count} faces (expected exactly 2). Face indices: {string.Join(", ", occurrences.Select(o => o.faceIdx))}");

            var occ1 = occurrences[0];
            var occ2 = occurrences[1];

            // Manifold orientation check: one directed (u -> v), other (v -> u)
            bool oppositeOrientation = (occ1.from == occ2.to) && (occ1.to == occ2.from);
            Assert.True(oppositeOrientation,
                $"Cut {cutType}: Edge ({edge.Item1}, {edge.Item2}) has inconsistent orientation between face {occ1.faceIdx} ({occ1.from}->{occ1.to}) and face {occ2.faceIdx} ({occ2.from}->{occ2.to})");
        }

        // Verify vertex manifold star: for every vertex, incident faces form a single closed umbrella/cone
        for (int vIdx = 0; vIdx < V; vIdx++)
        {
            var outgoingEdges = new List<int>();
            var incomingEdges = new List<int>();

            foreach (var kvp in edgeMap)
            {
                foreach (var occ in kvp.Value)
                {
                    if (occ.from == vIdx) outgoingEdges.Add(occ.to);
                    if (occ.to == vIdx) incomingEdges.Add(occ.from);
                }
            }

            Assert.Equal(outgoingEdges.Count, incomingEdges.Count);
            Assert.True(outgoingEdges.Count >= 3, $"Vertex {vIdx} has valence {outgoingEdges.Count} < 3 at cut {cutType}");
        }
    }

    #endregion

    #region 3. Normal Consistency & Outward Orientation

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    public void Mesh_NormalsPointStrictlyOutwardFromInterior(GemCutType cutType)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        var vertices = mesh.Vertices;
        var (min, max) = mesh.GetBounds();

        // Interior reference point: centroid of the gem interior
        Point3D interiorCenter = new Point3D(
            (min.X + max.X) * 0.5,
            (min.Y + max.Y) * 0.5,
            (min.Z + max.Z) * 0.5
        );

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            var normal = mesh.Normals[f];

            // Compute face centroid
            double cx = 0, cy = 0, cz = 0;
            foreach (int vIdx in face)
            {
                cx += vertices[vIdx].X;
                cy += vertices[vIdx].Y;
                cz += vertices[vIdx].Z;
            }
            Point3D faceCentroid = new Point3D(cx / face.Length, cy / face.Length, cz / face.Length);

            Vector3D outwardRay = (faceCentroid - interiorCenter).Normalize();
            double dot = normal.Dot(outwardRay);

            // Every facet normal must have positive dot product with the ray from the center to its centroid
            Assert.True(dot > 0.05,
                $"Cut {cutType}, Face {f}: normal ({normal.X:F3}, {normal.Y:F3}, {normal.Z:F3}) has non-positive outward dot {dot:F4} with ray from interior center.");
        }
    }

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    public void Mesh_FacetPlanarityCheck(GemCutType cutType)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        var vertices = mesh.Vertices;

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            if (face.Length <= 3) continue; // Triangles are inherently planar

            var normal = mesh.Normals[f];
            Point3D v0 = vertices[face[0]];

            // Maximum out-of-plane deviation
            double maxDist = 0.0;
            for (int i = 1; i < face.Length; i++)
            {
                Vector3D diff = vertices[face[i]] - v0;
                double dist = Math.Abs(normal.Dot(diff));
                if (dist > maxDist) maxDist = dist;
            }

            // Facets on 1.0mm gem must remain planar within 0.03 mm (3% of gem width)
            Assert.True(maxDist < 0.03,
                $"Cut {cutType}, Face {f} (size {face.Length}): non-planar deviation = {maxDist:F5} mm exceeds 0.03 mm threshold");
        }
    }

    #endregion

    #region 4. Pipeline Integration & Double-Scaling Anomaly Check

    [Theory]
    [InlineData(GemCutType.Oval, 1.45)]
    [InlineData(GemCutType.Pear, 1.55)]
    [InlineData(GemCutType.Emerald, 1.40)]
    [InlineData(GemCutType.Marquise, 1.95)]
    public void EndToEndPipeline_CanonicalMeshTransformedByPlacementMatrix_PreservesExpectedAspectRatio(
        GemCutType cutType, double expectedAspectRatio)
    {
        var mesh = GemMeshFactory.CreateCanonical(cutType);
        var line = new LineCurve3D(new Point3D(0, 0, 0), new Point3D(0, 10, 0));
        var engine = new GemEngine();
        double gemSizeMm = 1.0;

        // Matrix obtained directly from GemEngine end-to-end placement calculation
        var placement = engine.CalculatePlacements(line, null, cutType, gemSizeMm, spacingMm: 0.5)[0];
        var matrix = new Matrix4x4D(placement.Matrix4x4);
        var transformedMesh = mesh.Transform(matrix);

        var (min, max) = transformedMesh.GetBounds();
        double width = max.X - min.X;
        double length = max.Y - min.Y;
        double actualAspectRatio = length / width;

        // The transformed 1.0mm gem must have length/width ratio equal to expectedAspectRatio (e.g. 1.45).
        // If double-scaling occurs, actualAspectRatio will be expectedAspectRatio^2 (e.g. 2.1025 for Oval, 3.8025 for Marquise).
        Assert.True(Math.Abs(actualAspectRatio - expectedAspectRatio) < 0.05,
            $"Double-scaling defect detected! Cut {cutType} canonical aspect ratio is {expectedAspectRatio:F2}, but transformed mesh has aspect ratio {actualAspectRatio:F4} (deviating by {Math.Abs(actualAspectRatio - expectedAspectRatio):F4}, matching {expectedAspectRatio}^2 = {expectedAspectRatio * expectedAspectRatio:F4})");
    }

    #endregion

    #region 5. Matrix Invertibility & Condition Numbers for Extreme Aspect Ratios

    [Theory]
    [InlineData(0.001, 1.0)]      // Tiny gem
    [InlineData(1000.0, 1.0)]     // Huge gem
    [InlineData(1.0, 0.01)]       // Extremely thin aspect ratio
    [InlineData(1.0, 100.0)]      // Extremely elongated aspect ratio
    [InlineData(0.001, 0.001)]    // Micro-thin gem
    [InlineData(1000.0, 1000.0)]  // Giant elongated gem
    public void Matrix4x4D_PlacementMatrix_IsInvertible_WithValidDeterminant(double gemSizeMm, double aspectRatio)
    {
        var frame = OrthonormalFrame.Create(Vector3D.UnitX, Vector3D.UnitZ);
        Point3D pos = new Point3D(10.0, -20.0, 5.0);

        var matrix = Matrix4x4D.CreatePlacement(frame, pos, gemSizeMm, aspectRatio, rotationAngleDeg: 35.0);

        double det3x3 = matrix.Determinant3x3();
        Assert.False(double.IsNaN(det3x3));
        Assert.False(double.IsInfinity(det3x3));

        // Theoretical determinant: gemSize * (gemSize * aspectRatio) * gemSize = gemSize^3 * aspectRatio
        double expectedDet = gemSizeMm * gemSizeMm * gemSizeMm * aspectRatio;
        double relDiff = Math.Abs(det3x3 - expectedDet) / expectedDet;

        Assert.True(relDiff < 1e-4, $"Determinant {det3x3} deviated from expected {expectedDet} (relDiff: {relDiff})");

        // Verify matrix transforms points consistently
        Point3D testPt = new Point3D(1, 2, 3);
        Point3D transformed = matrix.Transform(testPt);
        Assert.False(double.IsNaN(transformed.X));
        Assert.False(double.IsNaN(transformed.Y));
        Assert.False(double.IsNaN(transformed.Z));
    }

    [Fact]
    public void OrthonormalFrame_HandlesNearCollinearAndDegenerateTangents()
    {
        // Tangent almost parallel to candidate normal
        Vector3D t = new Vector3D(0, 0, 1);
        Vector3D n = new Vector3D(1e-8, 1e-8, 1); // nearly identical direction

        var frame = OrthonormalFrame.Create(t, n);

        Assert.True(frame.IsOrthonormal(1e-5), "Frame must be orthonormal even under near-collinear input.");
        Assert.True(Math.Abs(frame.Determinant - 1.0) < 1e-5);
    }

    [Fact]
    public void OrthonormalFrame_HandlesZeroCandidateNormal()
    {
        Vector3D t = new Vector3D(1, 2, 3).Normalize();
        Vector3D n = Vector3D.Zero;

        var frame = OrthonormalFrame.Create(t, n);

        Assert.True(frame.IsOrthonormal(1e-5), "Frame must fallback to valid orthonormal basis when normal is zero.");
        Assert.True(Math.Abs(frame.Determinant - 1.0) < 1e-5);
    }

    #endregion

    #region 6. Placement Engine Edge Cases

    [Fact]
    public void GemEngine_HandlesZeroUsableLengthGracefully()
    {
        var engine = new GemEngine();
        int count = engine.CalculateStoneCount(0.5, 1.0, 0.2); // usable < gemSize
        Assert.Equal(0, count);

        var distances = engine.CalculateArcDistances(0.5, 1.0, 0.2, 0.0);
        Assert.Empty(distances);
    }

    [Fact]
    public void GemEngine_HandlesNegativeSpacing_PaveSetting()
    {
        var engine = new GemEngine();
        // Negative spacing (e.g. -0.2mm overlap in pavé setting): step = 1.0 - 0.2 = 0.8
        int count = engine.CalculateStoneCount(10.0, 1.0, -0.2);
        Assert.Equal(12, count);

        var distances = engine.CalculateArcDistances(10.0, 1.0, -0.2, 0.0);
        Assert.Equal(12, distances.Count);
        Assert.Equal(0.5, distances[0], 4);
        Assert.Equal(0.5 + 0.8, distances[1], 4);
    }

    [Fact]
    public void GemEngine_ThrowsWhenStepIsNonPositive()
    {
        var engine = new GemEngine();
        Assert.Throws<ArgumentException>(() => engine.CalculateStoneCount(10.0, 1.0, -1.0));
        Assert.Throws<ArgumentException>(() => engine.CalculateStoneCount(10.0, 1.0, -1.5));
    }

    #endregion
}
