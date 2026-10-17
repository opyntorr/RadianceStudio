namespace RadianceStudio.Core.Models;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Geometry;

/// <summary>
/// Represents a single polygonal facet of a 3D gem mesh.
/// </summary>
public readonly record struct Face3D
{
    public IReadOnlyList<int> VertexIndices { get; init; }
    public Vector3D Normal { get; init; }

    public Face3D(IReadOnlyList<int> vertexIndices, Vector3D normal)
    {
        if (vertexIndices == null || vertexIndices.Count < 3)
        {
            throw new ArgumentException("A polygon face must have at least 3 vertex indices.", nameof(vertexIndices));
        }

        VertexIndices = vertexIndices;
        Normal = normal;
    }
}
