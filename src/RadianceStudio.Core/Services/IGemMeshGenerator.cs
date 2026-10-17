namespace RadianceStudio.Core.Services;

using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;

/// <summary>
/// Defines the contract for procedural gem mesh generators.
/// </summary>
public interface IGemMeshGenerator
{
    /// <summary>
    /// Cut type produced by this generator.
    /// </summary>
    GemCutType CutType { get; }

    /// <summary>
    /// Generates canonical 1.0mm normalized gem mesh centered at origin.
    /// </summary>
    GemMesh GenerateCanonicalMesh();

    /// <summary>
    /// Generates gem mesh linearly scaled to the specified diameter in millimeters.
    /// </summary>
    GemMesh GenerateMesh(double diameterMm);

    /// <summary>
    /// Generates canonical 1.0mm normalized gem mesh for the specified cut type.
    /// </summary>
    GemMesh GenerateCanonicalMesh(GemCutType cutType);

    /// <summary>
    /// Generates gem mesh linearly scaled to the specified diameter for the specified cut type.
    /// </summary>
    GemMesh GenerateMesh(GemCutType cutType, double diameterMm);
}
