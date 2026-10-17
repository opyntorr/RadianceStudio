namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 50-facet Emerald cut (step cut) procedural mesh generator (aspect ratio 1.40).
/// Generates 3 crown steps (24 trapezoids + 1 table = 25) and 3 pavilion steps (24 trapezoids + 1 culet = 25).
/// </summary>
public class EmeraldMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Emerald;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double a = diameterMm * 0.5; // semi-width along X (0.5 mm)
        double b = a * dim.AspectRatio; // semi-length along Y (0.70 mm)

        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);

        double chamfer = diameterMm * 0.06; // 6% corner chamfer

        // Generates an 8-gon ring of vertices scaled and translated to height z
        Point3D[] CreateRing(double scale, double z)
        {
            double curA = a * scale;
            double curB = b * scale;
            double curC = chamfer * scale;

            return new Point3D[8]
            {
                new Point3D(curA, curB - curC, z),         // 0: right-top
                new Point3D(curA - curC, curB, z),         // 1: top-right
                new Point3D(-curA + curC, curB, z),        // 2: top-left
                new Point3D(-curA, curB - curC, z),        // 3: left-top
                new Point3D(-curA, -curB + curC, z),       // 4: left-bottom
                new Point3D(-curA + curC, -curB, z),       // 5: bottom-left
                new Point3D(curA - curC, -curB, z),        // 6: bottom-right
                new Point3D(curA, -curB + curC, z)         // 7: right-bottom
            };
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double c1Scale = tableScale + 0.33 * (1.0 - tableScale);
        double c2Scale = tableScale + 0.66 * (1.0 - tableScale);

        double zC1 = zGirdle + 0.66 * hc;
        double zC2 = zGirdle + 0.33 * hc;

        double p1Scale = 0.75;
        double p2Scale = 0.50;
        double culetScale = 0.20;

        double zP1 = zGirdle - 0.33 * hp;
        double zP2 = zGirdle - 0.66 * hp;

        var tableRing = CreateRing(tableScale, zTable);
        var c1Ring = CreateRing(c1Scale, zC1);
        var c2Ring = CreateRing(c2Scale, zC2);
        var girdleRing = CreateRing(1.0, zGirdle);
        var p1Ring = CreateRing(p1Scale, zP1);
        var p2Ring = CreateRing(p2Scale, zP2);
        var culetRing = CreateRing(culetScale, zCulet);

        var vertices = new List<Point3D>(56);
        // Indices:
        // 0..7: TableRing
        // 8..15: C1Ring
        // 16..23: C2Ring
        // 24..31: GirdleRing
        // 32..39: P1Ring
        // 40..47: P2Ring
        // 48..55: CuletRing
        vertices.AddRange(tableRing);
        vertices.AddRange(c1Ring);
        vertices.AddRange(c2Ring);
        vertices.AddRange(girdleRing);
        vertices.AddRange(p1Ring);
        vertices.AddRange(p2Ring);
        vertices.AddRange(culetRing);

        var faces = new List<int[]>(50);

        // 1. Table Face (Face 0, 8-gon)
        faces.Add(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });

        // 2. Crown Step 1 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { j, 8 + j, 8 + next, next });
        }

        // 3. Crown Step 2 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 8 + j, 16 + j, 16 + next, 8 + next });
        }

        // 4. Crown Step 3 (8 trapezoids bordering girdle)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 16 + j, 24 + j, 24 + next, 16 + next });
        }

        // 5. Pavilion Step 1 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 24 + j, 32 + j, 32 + next, 24 + next });
        }

        // 6. Pavilion Step 2 (8 trapezoids)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 32 + j, 40 + j, 40 + next, 32 + next });
        }

        // 7. Pavilion Step 3 (8 trapezoids converging to culet)
        for (int j = 0; j < 8; j++)
        {
            int next = (j + 1) % 8;
            faces.Add(new[] { 40 + j, 48 + j, 48 + next, 40 + next });
        }

        // 8. Culet Face (1 face at zCulet, CCW viewed from exterior)
        faces.Add(new[] { 55, 54, 53, 52, 51, 50, 49, 48 });

        return (vertices, faces);
    }
}
