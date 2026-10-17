namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 57-facet Oval Brilliant cut procedural mesh generator (aspect ratio 1.45).
/// </summary>
public class OvalMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Oval;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double rx0 = diameterMm * 0.5;
        double ry0 = rx0 * dim.AspectRatio;
        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);

        double rxTable = rx0 * (dim.TableWidthPercent / 100.0);
        double ryTable = ry0 * (dim.TableWidthPercent / 100.0);

        double rxStar = rxTable + 0.5 * (rx0 - rxTable);
        double ryStar = ryTable + 0.5 * (ry0 - ryTable);
        double zStar = (g * 0.5) + 0.60 * hc;

        double rxK = rx0 * 0.22;
        double ryK = ry0 * 0.22;
        double zK = -((g * 0.5) + 0.75 * hp);

        var vertices = new List<Point3D>(41);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V16: Elliptical Girdle vertices (16)
        for (int k = 0; k < 16; k++)
        {
            double angle = k * Math.PI / 8.0;
            vertices.Add(new Point3D(rx0 * Math.Cos(angle), ry0 * Math.Sin(angle), zGirdle));
        }

        // V17..V24: Table (octagon, 8)
        for (int j = 0; j < 8; j++)
        {
            double angle = j * Math.PI / 4.0;
            vertices.Add(new Point3D(rxTable * Math.Cos(angle), ryTable * Math.Sin(angle), zTable));
        }

        // V25..V32: Star vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double angle = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(new Point3D(rxStar * Math.Cos(angle), ryStar * Math.Sin(angle), zStar));
        }

        // V33..V40: Lower girdle ridge vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double angle = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(new Point3D(rxK * Math.Cos(angle), ryK * Math.Sin(angle), zK));
        }

        var faces = new List<int[]>(57);

        // 1. Table Face (Face 0)
        faces.Add(new[] { 17, 18, 19, 20, 21, 22, 23, 24 });

        // 2. 8 Star Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + j, 17 + ((j + 1) % 8) });
        }

        // 3. 8 Bezel Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + ((j + 7) % 8), 1 + 2 * j, 25 + j });
        }

        // 4. 16 Upper Girdle Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 25 + j, 1 + 2 * j, 1 + (2 * j + 1) });
            faces.Add(new[] { 25 + j, 1 + (2 * j + 1), 1 + ((2 * j + 2) % 16) });
        }

        // 5. 16 Lower Girdle Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + j, 1 + (2 * j + 1) });
            faces.Add(new[] { 1 + (2 * j + 1), 33 + j, 1 + ((2 * j + 2) % 16) });
        }

        // 6. 8 Pavilion Main Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + ((j + 7) % 8), 0, 33 + j });
        }

        return (vertices, faces);
    }
}
