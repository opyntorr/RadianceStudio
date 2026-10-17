namespace RadianceStudio.Core.Engines;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services;
using Rhino.Geometry;

/// <summary>
/// Computational engine for jewelry manufacturing, CAM preparation, wall thickness verification,
/// binary STL watertight mesh export, and comprehensive Bill of Materials (BOM) reporting.
/// Adheres to the dual-engine pattern: pure C# evaluation for fast headless testing + RhinoCommon wrappers.
/// </summary>
public static class ManufacturingEngine
{
    private static readonly PreciousMetalAlloy[] AllAlloys = Enum.GetValues<PreciousMetalAlloy>();

    #region Metal Weight Calculations

    /// <summary>
    /// Computes physical cast weights and estimated metal costs across all precious metal alloys and casting wax
    /// for a given solid volume in cubic millimeters.
    /// </summary>
    /// <param name="metalVolumeMm3">Solid metal volume in cubic millimeters (mm³).</param>
    /// <param name="spotPricesPerGram">Optional dictionary of market spot prices per gram keyed by alloy.</param>
    /// <returns>List of weight and cost breakdowns for each alloy.</returns>
    public static List<MetalWeightInfo> CalculateMetalWeights(
        double metalVolumeMm3,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null)
    {
        if (metalVolumeMm3 < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(metalVolumeMm3), "Metal volume cannot be negative.");
        }

        var results = new List<MetalWeightInfo>(AllAlloys.Length);

        foreach (var alloy in AllAlloys)
        {
            double pricePerGram = 0.0;
            if (spotPricesPerGram != null && spotPricesPerGram.TryGetValue(alloy, out double price))
            {
                pricePerGram = price;
            }

            results.Add(MetalWeightInfo.FromVolume(alloy, metalVolumeMm3, pricePerGram));
        }

        return results;
    }

    /// <summary>
    /// Computes physical cast weights and estimated metal costs for a watertight mesh representation.
    /// Calculates mesh volume using the signed tetrahedron Gauss Divergence Theorem.
    /// </summary>
    /// <param name="metalMesh">Closed 2-manifold metal mesh.</param>
    /// <param name="spotPricesPerGram">Optional dictionary of market spot prices per gram keyed by alloy.</param>
    /// <returns>List of weight and cost breakdowns for each alloy.</returns>
    public static List<MetalWeightInfo> CalculateMetalWeights(
        GemMesh metalMesh,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null)
    {
        ArgumentNullException.ThrowIfNull(metalMesh);

        double volumeMm3 = CaratEstimator.ComputeVolumeMm3(metalMesh);
        return CalculateMetalWeights(volumeMm3, spotPricesPerGram);
    }

    /// <summary>
    /// Computes metal weights for a RhinoCommon Mesh geometry when Rhino native runtime is available.
    /// </summary>
    public static List<MetalWeightInfo> CalculateMetalWeights(
        Mesh rhinoMesh,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null)
    {
        ArgumentNullException.ThrowIfNull(rhinoMesh);

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return new List<MetalWeightInfo>();
        }

        var gemMesh = ConvertRhinoMeshToGemMesh(rhinoMesh);
        return CalculateMetalWeights(gemMesh, spotPricesPerGram);
    }

    /// <summary>
    /// Computes metal weights for a RhinoCommon Brep solid when Rhino native runtime is available.
    /// </summary>
    public static List<MetalWeightInfo> CalculateMetalWeights(
        Brep rhinoBrep,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null)
    {
        ArgumentNullException.ThrowIfNull(rhinoBrep);

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return new List<MetalWeightInfo>();
        }

        double volume = rhinoBrep.GetVolume();
        if (double.IsNaN(volume) || volume < 0.0)
        {
            volume = 0.0;
        }

        return CalculateMetalWeights(volume, spotPricesPerGram);
    }

    /// <summary>
    /// Computes casting wax-to-metal weight multiplier factor for an alloy (ratio of metal density to wax density).
    /// </summary>
    public static double GetWaxToMetalMultiplier(PreciousMetalAlloy alloy) => alloy.GetWaxToMetalMultiplier();

    #endregion

    #region Wall Thickness Inspection

    /// <summary>
    /// Evaluates structural wall thickness across a mesh using inverted ray-casting along inward vertex normals.
    /// Identifies thin spots that fail minimum casting thickness constraints.
    /// </summary>
    /// <param name="metalMesh">The polyhedral mesh to inspect.</param>
    /// <param name="minGeneralWallMm">Required minimum thickness for general walls, shanks, and under-galleries (default: 0.80 mm).</param>
    /// <param name="minProngNeckMm">Required minimum thickness for slender prong necks and tips (default: 0.60 mm).</param>
    /// <param name="featureClassifier">Optional custom classifier assigning feature names to spatial locations.</param>
    /// <param name="onlyViolations">If true, returns only diagnostics where MeasuredThickness &lt; RequiredMinimum.</param>
    /// <returns>List of wall thickness diagnostics for evaluated surface locations.</returns>
    public static List<WallThicknessDiagnostic> InspectWallThickness(
        GemMesh metalMesh,
        double minGeneralWallMm = 0.80,
        double minProngNeckMm = 0.60,
        Func<Point3D, string>? featureClassifier = null,
        bool onlyViolations = false)
    {
        ArgumentNullException.ThrowIfNull(metalMesh);

        var diagnostics = new List<WallThicknessDiagnostic>();
        if (metalMesh.Vertices.Count == 0 || metalMesh.Faces.Count == 0)
        {
            return diagnostics;
        }

        // 1. Triangulate mesh polygons and index which vertices belong to which triangles
        var triangles = BuildTriangles(metalMesh);
        if (triangles.Count == 0)
        {
            return diagnostics;
        }

        // 2. Compute outward vertex normals by area/polygon weighting
        var vertexNormals = ComputeVertexNormals(metalMesh);

        // 3. Cache triangle bounding boxes for rapid ray-box intersection culling
        var triangleBoxes = new (Point3D Min, Point3D Max)[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
        {
            triangleBoxes[i] = ComputeTriangleBounds(triangles[i].V0, triangles[i].V1, triangles[i].V2);
        }

        var bounds = metalMesh.GetBounds();

        // 4. Inverted ray-casting per vertex
        for (int i = 0; i < metalMesh.Vertices.Count; i++)
        {
            var v = metalMesh.Vertices[i];
            var n = vertexNormals[i];

            if (n.LengthSquared < 1e-12)
            {
                continue;
            }

            var inwardDir = new Vector3D(-n.X, -n.Y, -n.Z);
            var rayOrigin = v + (inwardDir * 1e-5);

            double closestHit = double.MaxValue;

            for (int t = 0; t < triangles.Count; t++)
            {
                var tri = triangles[t];

                // Skip any triangle sharing the current vertex to eliminate self-intersection
                if (tri.Idx0 == i || tri.Idx1 == i || tri.Idx2 == i)
                {
                    continue;
                }

                // Rapid AABB test before detailed Möller-Trumbore
                if (!RayIntersectsAabb(rayOrigin, inwardDir, triangleBoxes[t].Min, triangleBoxes[t].Max, closestHit))
                {
                    continue;
                }

                if (RayTriangleIntersect(rayOrigin, inwardDir, tri.V0, tri.V1, tri.V2, out double hitDistance))
                {
                    if (hitDistance < closestHit)
                    {
                        closestHit = hitDistance;
                    }
                }
            }

            // Only report if an opposing surface was successfully intersected
            if (closestHit < double.MaxValue)
            {
                string feature = featureClassifier?.Invoke(v) ?? ClassifyFeature(v, bounds);
                double required = feature.Equals("Prong Neck", StringComparison.OrdinalIgnoreCase)
                    ? minProngNeckMm
                    : minGeneralWallMm;

                bool isViolation = closestHit < required;

                if (!onlyViolations || isViolation)
                {
                    diagnostics.Add(new WallThicknessDiagnostic
                    {
                        Location = v,
                        Normal = n,
                        MeasuredThicknessMm = closestHit,
                        RequiredMinimumMm = required,
                        IsViolation = isViolation,
                        FeatureDescription = feature
                    });
                }
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Convenience helper returning exclusively wall thickness violations on the mesh.
    /// </summary>
    public static List<WallThicknessDiagnostic> GetWallThicknessViolations(
        GemMesh metalMesh,
        double minGeneralWallMm = 0.80,
        double minProngNeckMm = 0.60,
        Func<Point3D, string>? featureClassifier = null)
    {
        return InspectWallThickness(metalMesh, minGeneralWallMm, minProngNeckMm, featureClassifier, onlyViolations: true);
    }

    /// <summary>
    /// RhinoCommon Mesh wall thickness inspection wrapper.
    /// </summary>
    public static List<WallThicknessDiagnostic> InspectWallThickness(
        Mesh rhinoMesh,
        double minGeneralWallMm = 0.80,
        double minProngNeckMm = 0.60,
        Func<Point3D, string>? featureClassifier = null,
        bool onlyViolations = false)
    {
        ArgumentNullException.ThrowIfNull(rhinoMesh);

        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return new List<WallThicknessDiagnostic>();
        }

        var gemMesh = ConvertRhinoMeshToGemMesh(rhinoMesh);
        return InspectWallThickness(gemMesh, minGeneralWallMm, minProngNeckMm, featureClassifier, onlyViolations);
    }

    #endregion

    #region Bill of Materials (BOM) Generation

    /// <summary>
    /// Compiles a comprehensive Bill of Materials (BOM) report including casting estimates, gemstone tally,
    /// and structural wall thickness quality diagnostics.
    /// </summary>
    /// <param name="projectTitle">Project name or reference SKU.</param>
    /// <param name="metalVolumeMm3">Total solid metal volume in cubic millimeters.</param>
    /// <param name="gems">Gemstone dimensions and material assignments.</param>
    /// <param name="diagnostics">Optional list of wall thickness diagnostics.</param>
    /// <param name="spotPricesPerGram">Optional dictionary of market spot prices per gram keyed by alloy.</param>
    /// <returns>Structured JewelryBomReport instance.</returns>
    public static JewelryBomReport GenerateBomReport(
        string projectTitle,
        double metalVolumeMm3,
        IReadOnlyList<(GemDimensions gem, CaratEstimator.GemMaterial material)> gems,
        IReadOnlyList<WallThicknessDiagnostic>? diagnostics = null,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null)
    {
        var metalWeights = CalculateMetalWeights(metalVolumeMm3, spotPricesPerGram);

        int totalGemCount = 0;
        double totalCaratWeight = 0.0;
        var lineItems = new List<GemBomLineItem>();

        if (gems != null && gems.Count > 0)
        {
            totalGemCount = gems.Count;

            // Group by cut type, material, and diameter rounded to 2 decimal places
            var groups = gems.GroupBy(g => (g.gem.CutType, g.material, Diameter: Math.Round(g.gem.DiameterMm, 2)));

            foreach (var grp in groups)
            {
                int count = grp.Count();
                double avgDiameter = grp.Average(g => g.gem.DiameterMm);
                double groupCarats = 0.0;

                foreach (var (gem, mat) in grp)
                {
                    var gemMesh = GemMeshFactory.Create(gem.CutType, gem.DiameterMm);
                    double c = CaratEstimator.EstimateCarats(gemMesh, mat);
                    groupCarats += c;
                }

                totalCaratWeight += groupCarats;

                lineItems.Add(new GemBomLineItem
                {
                    CutType = grp.Key.CutType,
                    GemMaterial = grp.Key.material,
                    Count = count,
                    AverageDiameterMm = avgDiameter,
                    TotalCarats = groupCarats
                });
            }
        }

        var violations = diagnostics?.Where(d => d.IsViolation).ToList() ?? new List<WallThicknessDiagnostic>();
        bool isProductionReady = violations.Count == 0;

        return new JewelryBomReport
        {
            ProjectTitle = string.IsNullOrWhiteSpace(projectTitle) ? "Untitled Jewelry Design" : projectTitle,
            CreatedUtc = DateTime.UtcNow,
            MetalWeights = metalWeights,
            TotalGemCount = totalGemCount,
            TotalCaratWeight = totalCaratWeight,
            GemBreakdown = lineItems,
            WallThicknessViolations = violations,
            IsProductionReady = isProductionReady
        };
    }

    /// <summary>
    /// Generates a complete BOM report directly from a GemMesh and gem list, automatically performing volume
    /// integration and wall thickness inspection.
    /// </summary>
    public static JewelryBomReport GenerateBomReport(
        string projectTitle,
        GemMesh metalMesh,
        IReadOnlyList<(GemDimensions gem, CaratEstimator.GemMaterial material)> gems,
        IReadOnlyDictionary<PreciousMetalAlloy, double>? spotPricesPerGram = null,
        double minGeneralWallMm = 0.80,
        double minProngNeckMm = 0.60)
    {
        ArgumentNullException.ThrowIfNull(metalMesh);

        double volumeMm3 = CaratEstimator.ComputeVolumeMm3(metalMesh);
        var diagnostics = InspectWallThickness(metalMesh, minGeneralWallMm, minProngNeckMm);
        return GenerateBomReport(projectTitle, volumeMm3, gems, diagnostics, spotPricesPerGram);
    }

    #endregion

    #region Watertight Binary STL Exporter

    /// <summary>
    /// Validates mesh manifoldness and exports standard binary STL format byte payload.
    /// </summary>
    /// <param name="mesh">The polyhedral mesh to export.</param>
    /// <param name="headerText">Descriptive header text (truncated and padded to exactly 80 bytes).</param>
    /// <param name="validateManifold">If true, enforces 2-manifold watertightness before exporting.</param>
    /// <returns>Binary STL byte array.</returns>
    public static byte[] ExportWatertightStl(
        GemMesh mesh,
        string headerText = "RadianceStudio Watertight STL Export",
        bool validateManifold = true)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (validateManifold)
        {
            if (!ValidateMeshManifoldness(mesh, out var errors))
            {
                throw new InvalidOperationException($"Mesh manifoldness validation failed: {string.Join("; ", errors)}");
            }
        }

        var triangles = BuildTriangles(mesh);
        uint triangleCount = (uint)triangles.Count;

        // Binary STL specification:
        // 80 bytes header + 4 bytes triangle count + 50 bytes per triangle
        int totalBytes = 80 + 4 + (triangles.Count * 50);

        using var ms = new MemoryStream(totalBytes);
        using var writer = new BinaryWriter(ms);

        // 1. Write 80-byte header
        byte[] headerBytes = new byte[80];
        if (!string.IsNullOrEmpty(headerText))
        {
            byte[] textBytes = Encoding.ASCII.GetBytes(headerText);
            int copyLen = Math.Min(textBytes.Length, 80);
            Array.Copy(textBytes, headerBytes, copyLen);
        }
        writer.Write(headerBytes);

        // 2. Write triangle count (uint32 little-endian)
        writer.Write(triangleCount);

        // 3. Write each facet (50 bytes each)
        foreach (var tri in triangles)
        {
            // Compute triangle normal
            var edge1 = tri.V1 - tri.V0;
            var edge2 = tri.V2 - tri.V0;
            var normal = edge1.Cross(edge2);
            if (normal.LengthSquared > 1e-12)
            {
                normal = normal.Normalize();
            }
            else
            {
                normal = Vector3D.UnitZ;
            }

            // Normal vector (float32 x 3)
            writer.Write((float)normal.X);
            writer.Write((float)normal.Y);
            writer.Write((float)normal.Z);

            // Vertex 0 (float32 x 3)
            writer.Write((float)tri.V0.X);
            writer.Write((float)tri.V0.Y);
            writer.Write((float)tri.V0.Z);

            // Vertex 1 (float32 x 3)
            writer.Write((float)tri.V1.X);
            writer.Write((float)tri.V1.Y);
            writer.Write((float)tri.V1.Z);

            // Vertex 2 (float32 x 3)
            writer.Write((float)tri.V2.X);
            writer.Write((float)tri.V2.Y);
            writer.Write((float)tri.V2.Z);

            // Attribute byte count (uint16 = 0)
            writer.Write((ushort)0);
        }

        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Validates mesh 2-manifold topological integrity:
    /// - Checks that each undirected edge is shared by exactly 2 faces.
    /// - Checks minimum non-degenerate facet count.
    /// </summary>
    public static bool ValidateMeshManifoldness(GemMesh mesh, out List<string> errors)
    {
        errors = new List<string>();

        if (mesh == null)
        {
            errors.Add("Mesh is null.");
            return false;
        }

        if (mesh.Vertices.Count < 4)
        {
            errors.Add($"Mesh contains only {mesh.Vertices.Count} vertices; minimum 4 required for 3D closed manifold.");
        }

        if (mesh.Faces.Count < 4)
        {
            errors.Add($"Mesh contains only {mesh.Faces.Count} faces; minimum 4 required for 3D closed manifold.");
        }

        // Count edge occurrences across all triangulated faces
        var edgeCounts = new Dictionary<(int Min, int Max), int>();

        foreach (var face in mesh.Faces)
        {
            if (face.Length < 3) continue;

            for (int i = 0; i < face.Length; i++)
            {
                int u = face[i];
                int v = face[(i + 1) % face.Length];

                if (u == v) continue;

                var key = u < v ? (u, v) : (v, u);
                edgeCounts[key] = edgeCounts.GetValueOrDefault(key, 0) + 1;
            }
        }

        int openBoundaryEdges = 0;
        int nonManifoldEdges = 0;

        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value == 1)
            {
                openBoundaryEdges++;
            }
            else if (kvp.Value > 2)
            {
                nonManifoldEdges++;
            }
        }

        if (openBoundaryEdges > 0)
        {
            errors.Add($"Mesh has {openBoundaryEdges} open boundary edge(s); model is not watertight.");
        }

        if (nonManifoldEdges > 0)
        {
            errors.Add($"Mesh has {nonManifoldEdges} non-manifold edge(s) shared by more than 2 faces.");
        }

        return errors.Count == 0;
    }

    #endregion

    #region ShrinkWrap CAM Resolution Wrapping

    /// <summary>
    /// Dual-engine RhinoCommon ShrinkWrap voxelization wrapper for preparing casting meshes.
    /// Operates only when Rhino native runtime is loaded.
    /// </summary>
    public static Mesh? CreateShrinkWrapMesh(
        IEnumerable<Mesh> inputMeshes,
        CastingResolutionProfile resolution = CastingResolutionProfile.StandardCasting,
        double offsetMm = 0.0)
    {
        if (!RailEngine.IsRhinoNativeAvailable)
        {
            return null;
        }

        var parameters = new ShrinkWrapParameters
        {
            TargetEdgeLength = resolution.GetVoxelSizeMm(),
            Offset = offsetMm
        };

        return Mesh.ShrinkWrap(inputMeshes, parameters);
    }

    #endregion

    #region Internal Geometric Helper Methods

    private readonly record struct TriangleInfo(Point3D V0, Point3D V1, Point3D V2, int Idx0, int Idx1, int Idx2);

    private static List<TriangleInfo> BuildTriangles(GemMesh mesh)
    {
        var triangles = new List<TriangleInfo>();
        var verts = mesh.Vertices;

        foreach (var face in mesh.Faces)
        {
            if (face.Length < 3) continue;

            int i0 = face[0];
            var v0 = verts[i0];

            for (int i = 1; i < face.Length - 1; i++)
            {
                int i1 = face[i];
                int i2 = face[i + 1];

                triangles.Add(new TriangleInfo(v0, verts[i1], verts[i2], i0, i1, i2));
            }
        }

        return triangles;
    }

    private static Vector3D[] ComputeVertexNormals(GemMesh mesh)
    {
        var normals = new Vector3D[mesh.Vertices.Count];

        for (int f = 0; f < mesh.Faces.Count; f++)
        {
            var face = mesh.Faces[f];
            if (face.Length < 3) continue;

            var fn = (f < mesh.Normals.Count && !mesh.Normals[f].IsZero())
                ? mesh.Normals[f]
                : GemMesh.ComputeFaceNormal(mesh.Vertices, face);

            for (int i = 0; i < face.Length; i++)
            {
                int vIdx = face[i];
                normals[vIdx] = normals[vIdx] + fn;
            }
        }

        for (int i = 0; i < normals.Length; i++)
        {
            if (normals[i].LengthSquared > 1e-12)
            {
                normals[i] = normals[i].Normalize();
            }
            else
            {
                normals[i] = Vector3D.UnitZ;
            }
        }

        return normals;
    }

    private static (Point3D Min, Point3D Max) ComputeTriangleBounds(Point3D a, Point3D b, Point3D c)
    {
        double minX = Math.Min(a.X, Math.Min(b.X, c.X));
        double maxX = Math.Max(a.X, Math.Max(b.X, c.X));
        double minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        double maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
        double minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z));
        double maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));

        return (new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
    }

    private static bool RayIntersectsAabb(
        Point3D rayOrigin,
        Vector3D rayDir,
        Point3D boxMin,
        Point3D boxMax,
        double maxDistance)
    {
        double tMin = 0.0;
        double tMax = maxDistance;

        // X slab
        if (Math.Abs(rayDir.X) < 1e-12)
        {
            if (rayOrigin.X < boxMin.X || rayOrigin.X > boxMax.X) return false;
        }
        else
        {
            double invD = 1.0 / rayDir.X;
            double t1 = (boxMin.X - rayOrigin.X) * invD;
            double t2 = (boxMax.X - rayOrigin.X) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        // Y slab
        if (Math.Abs(rayDir.Y) < 1e-12)
        {
            if (rayOrigin.Y < boxMin.Y || rayOrigin.Y > boxMax.Y) return false;
        }
        else
        {
            double invD = 1.0 / rayDir.Y;
            double t1 = (boxMin.Y - rayOrigin.Y) * invD;
            double t2 = (boxMax.Y - rayOrigin.Y) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        // Z slab
        if (Math.Abs(rayDir.Z) < 1e-12)
        {
            if (rayOrigin.Z < boxMin.Z || rayOrigin.Z > boxMax.Z) return false;
        }
        else
        {
            double invD = 1.0 / rayDir.Z;
            double t1 = (boxMin.Z - rayOrigin.Z) * invD;
            double t2 = (boxMax.Z - rayOrigin.Z) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        return tMax >= 1e-5;
    }

    private static bool RayTriangleIntersect(
        Point3D rayOrigin,
        Vector3D rayDir,
        Point3D v0,
        Point3D v1,
        Point3D v2,
        out double hitDistance)
    {
        hitDistance = 0.0;
        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var h = rayDir.Cross(edge2);
        double a = edge1.Dot(h);

        if (Math.Abs(a) < 1e-12)
        {
            return false;
        }

        double f = 1.0 / a;
        var s = rayOrigin - v0;
        double u = f * s.Dot(h);

        if (u < -1e-6 || u > 1.0 + 1e-6)
        {
            return false;
        }

        var q = s.Cross(edge1);
        double v = f * rayDir.Dot(q);

        if (v < -1e-6 || (u + v) > 1.0 + 1e-6)
        {
            return false;
        }

        double t = f * edge2.Dot(q);
        if (t > 1e-5)
        {
            hitDistance = t;
            return true;
        }

        return false;
    }

    private static string ClassifyFeature(Point3D location, (Point3D Min, Point3D Max) bounds)
    {
        double height = bounds.Max.Z - bounds.Min.Z;
        if (height < 1e-6)
        {
            return "General Wall";
        }

        double relativeZ = (location.Z - bounds.Min.Z) / height;

        // If top portion of piece and isolated slender column/prong
        if (relativeZ > 0.82)
        {
            return "Prong Neck";
        }

        // Bottom portion corresponds to ring shank base
        if (relativeZ < 0.18)
        {
            return "Shank Base";
        }

        // Mid section under gem seat
        if (relativeZ >= 0.50 && relativeZ <= 0.82)
        {
            return "Under-Gallery Wall";
        }

        return "General Wall";
    }

    private static GemMesh ConvertRhinoMeshToGemMesh(Mesh mesh)
    {
        var vertices = new Point3D[mesh.Vertices.Count];
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            vertices[i] = new Point3D(v.X, v.Y, v.Z);
        }

        var faces = new List<int[]>(mesh.Faces.Count);
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            var f = mesh.Faces[i];
            if (f.IsQuad)
            {
                faces.Add(new[] { f.A, f.B, f.C, f.D });
            }
            else
            {
                faces.Add(new[] { f.A, f.B, f.C });
            }
        }

        return new GemMesh
        {
            Vertices = vertices,
            Faces = faces
        };
    }

    #endregion
}
