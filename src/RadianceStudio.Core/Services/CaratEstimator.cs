namespace RadianceStudio.Core.Services;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Models;

/// <summary>
/// Computational estimator for gemstone volume (mm³) and carat weights across canonical gem materials.
/// Uses the signed tetrahedron divergence method for watertight polyhedral mesh volume integration.
/// </summary>
public static class CaratEstimator
{
    /// <summary>
    /// Supported standard gem materials with physical specific gravity / density values (g/cm³).
    /// </summary>
    public enum GemMaterial
    {
        Diamond,
        Sapphire,
        Emerald,
        Moissanite,
        CubicZirconia
    }

    private static readonly Dictionary<GemMaterial, double> Densities = new()
    {
        [GemMaterial.Diamond] = 3.52,
        [GemMaterial.Sapphire] = 4.00,
        [GemMaterial.Emerald] = 2.72,
        [GemMaterial.Moissanite] = 3.22,
        [GemMaterial.CubicZirconia] = 5.80
    };

    /// <summary>
    /// Computes the exact volume of a closed 2-manifold gem mesh in cubic millimeters (mm³)
    /// using the signed tetrahedron Gauss divergence theorem.
    /// </summary>
    public static double ComputeVolumeMm3(GemMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var vertices = mesh.Vertices;
        double totalVolume = 0.0;

        foreach (var face in mesh.Faces)
        {
            if (face.Length < 3) continue;

            var v0 = vertices[face[0]];
            // Fan triangulation from vertex 0 of each polygon face
            for (int i = 1; i < face.Length - 1; i++)
            {
                var v1 = vertices[face[i]];
                var v2 = vertices[face[i + 1]];

                // Signed volume of tetrahedron formed by origin (0,0,0) and (v0, v1, v2):
                // V_tet = (v0 · (v1 × v2)) / 6.0
                double crossX = (v1.Y * v2.Z) - (v1.Z * v2.Y);
                double crossY = (v1.Z * v2.X) - (v1.X * v2.Z);
                double crossZ = (v1.X * v2.Y) - (v1.Y * v2.X);

                double tetVolume = ((v0.X * crossX) + (v0.Y * crossY) + (v0.Z * crossZ)) / 6.0;
                totalVolume += tetVolume;
            }
        }

        return Math.Abs(totalVolume);
    }

    /// <summary>
    /// Estimates carat weight of the gem mesh for the specified material density.
    /// Carats = Volume (mm³) × Density (g/cm³) / 1000 × 5.0.
    /// </summary>
    public static double EstimateCarats(GemMesh mesh, GemMaterial material = GemMaterial.Diamond)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        double volumeMm3 = ComputeVolumeMm3(mesh);
        double density = GetDensity(material);

        return (volumeMm3 * density / 1000.0) * 5.0;
    }

    /// <summary>
    /// Gets the standard physical density (g/cm³) for the given gem material.
    /// </summary>
    public static double GetDensity(GemMaterial material) => Densities[material];
}
