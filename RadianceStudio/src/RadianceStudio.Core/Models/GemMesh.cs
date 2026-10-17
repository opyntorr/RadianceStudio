namespace RadianceStudio.Core.Models;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;

/// <summary>
/// Complete 3D polyhedral representation of a procedural gem.
/// </summary>
public record GemMesh
{
    public IReadOnlyList<Point3D> Vertices { get; init; } = Array.Empty<Point3D>();
    public IReadOnlyList<Vector3D> Normals { get; init; } = Array.Empty<Vector3D>();
    public IReadOnlyList<int[]> Faces { get; init; } = Array.Empty<int[]>();
    public int FacetCount => Faces.Count;
    public GemCutType CutType { get; init; }
    public double DiameterMm { get; init; } = 1.0;

    /// <summary>
    /// Gets the axis-aligned bounding box of the mesh.
    /// </summary>
    public (Point3D Min, Point3D Max) GetBounds()
    {
        if (Vertices.Count == 0)
        {
            return (new Point3D(0, 0, 0), new Point3D(0, 0, 0));
        }

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (var v in Vertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Y > maxY) maxY = v.Y;
            if (v.Z < minZ) minZ = v.Z;
            if (v.Z > maxZ) maxZ = v.Z;
        }

        return (new Point3D(minX, minY, minZ), new Point3D(maxX, maxY, maxZ));
    }

    /// <summary>
    /// Computes the outward-pointing unit normal for a facet using Newell's method.
    /// </summary>
    public static Vector3D ComputeFaceNormal(IReadOnlyList<Point3D> vertices, int[] faceIndices)
    {
        if (faceIndices == null || faceIndices.Length < 3)
        {
            throw new ArgumentException("Face must contain at least 3 vertices.", nameof(faceIndices));
        }

        double nx = 0, ny = 0, nz = 0;
        int n = faceIndices.Length;

        for (int i = 0; i < n; i++)
        {
            var current = vertices[faceIndices[i]];
            var next = vertices[faceIndices[(i + 1) % n]];

            nx += (current.Y - next.Y) * (current.Z + next.Z);
            ny += (current.Z - next.Z) * (current.X + next.X);
            nz += (current.X - next.X) * (current.Y + next.Y);
        }

        var normal = new Vector3D(nx, ny, nz);
        double len = normal.Length;
        return len > 1e-12 ? normal.Normalize() : new Vector3D(0, 0, 1);
    }

    /// <summary>
    /// Returns a new GemMesh transformed by the specified 4x4 matrix.
    /// </summary>
    public GemMesh Transform(Matrix4x4D matrix)
    {
        var transformedVertices = new Point3D[Vertices.Count];
        for (int i = 0; i < Vertices.Count; i++)
        {
            transformedVertices[i] = matrix.Transform(Vertices[i]);
        }

        var transformedNormals = new Vector3D[Faces.Count];
        for (int i = 0; i < Faces.Count; i++)
        {
            transformedNormals[i] = ComputeFaceNormal(transformedVertices, Faces[i]);
        }

        return new GemMesh
        {
            CutType = CutType,
            DiameterMm = DiameterMm,
            Vertices = transformedVertices,
            Faces = Faces,
            Normals = transformedNormals
        };
    }
}
