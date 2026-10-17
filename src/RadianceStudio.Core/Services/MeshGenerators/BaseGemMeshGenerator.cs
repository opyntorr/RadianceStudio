namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Abstract base class for all canonical procedural gem mesh generators.
/// </summary>
public abstract class BaseGemMeshGenerator : IGemMeshGenerator
{
    public abstract GemCutType CutType { get; }

    public GemMesh GenerateCanonicalMesh() => GenerateMesh(1.0);

    public GemMesh GenerateCanonicalMesh(GemCutType cutType)
    {
        if (cutType != CutType)
        {
            throw new ArgumentException($"Generator {GetType().Name} handles {CutType}, but {cutType} was requested.", nameof(cutType));
        }
        return GenerateCanonicalMesh();
    }

    public GemMesh GenerateMesh(GemCutType cutType, double diameterMm)
    {
        if (cutType != CutType)
        {
            throw new ArgumentException($"Generator {GetType().Name} handles {CutType}, but {cutType} was requested.", nameof(cutType));
        }
        return GenerateMesh(diameterMm);
    }

    public GemMesh GenerateMesh(double diameterMm)
    {
        if (double.IsNaN(diameterMm) || double.IsInfinity(diameterMm))
        {
            throw new ArgumentException("Diameter must be a valid finite number.", nameof(diameterMm));
        }

        if (diameterMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(diameterMm), "Diameter must be strictly positive.");
        }

        var (vertices, faces) = BuildTopology(diameterMm);

        var normals = new Vector3D[faces.Count];
        for (int i = 0; i < faces.Count; i++)
        {
            normals[i] = GemMesh.ComputeFaceNormal(vertices, faces[i]);
        }

        return new GemMesh
        {
            CutType = CutType,
            DiameterMm = diameterMm,
            Vertices = vertices,
            Faces = faces,
            Normals = normals
        };
    }

    protected abstract (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm);
}
