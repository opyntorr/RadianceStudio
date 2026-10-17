namespace RadianceStudio.Core.Services;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Models;
using RadianceStudio.Core.Services.MeshGenerators;

/// <summary>
/// Factory and registry for canonical gem mesh generators.
/// </summary>
public static class GemMeshFactory
{
    private static readonly Dictionary<GemCutType, IGemMeshGenerator> _generators = new()
    {
        [GemCutType.RoundBrilliant] = new RoundBrilliantMeshGenerator(),
        [GemCutType.Princess] = new PrincessMeshGenerator(),
        [GemCutType.Oval] = new OvalMeshGenerator(),
        [GemCutType.Pear] = new PearMeshGenerator(),
        [GemCutType.Emerald] = new EmeraldMeshGenerator(),
        [GemCutType.Marquise] = new MarquiseMeshGenerator(),
        [GemCutType.Baguette] = new BaguetteMeshGenerator(),
        [GemCutType.Cushion] = new CushionMeshGenerator(),
        [GemCutType.Asscher] = new AsscherMeshGenerator(),
        [GemCutType.Radiant] = new RadiantMeshGenerator(),
        [GemCutType.Heart] = new HeartMeshGenerator(),
        [GemCutType.Trilliant] = new TrilliantMeshGenerator(),
        [GemCutType.TaperedBaguette] = new TaperedBaguetteMeshGenerator()
    };

    public static IGemMeshGenerator GetGenerator(GemCutType cutType)
    {
        if (_generators.TryGetValue(cutType, out var generator))
        {
            return generator;
        }

        throw new NotSupportedException($"Mesh generator for cut type '{cutType}' is not implemented.");
    }

    public static GemMesh CreateCanonical(GemCutType cutType) =>
        GetGenerator(cutType).GenerateCanonicalMesh();

    public static GemMesh Create(GemCutType cutType, double diameterMm) =>
        GetGenerator(cutType).GenerateMesh(diameterMm);
}
