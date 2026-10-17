namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 57-facet Marquise (navette) cut procedural mesh generator (aspect ratio 1.95).
/// </summary>
public class MarquiseMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Marquise;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double a = diameterMm * 0.5; // semi-width along X
        double b = a * dim.AspectRatio; // semi-length along Y (1.95 * a)

        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);
        double zStar = (g * 0.5) + 0.60 * hc;
        double zK = -((g * 0.5) + 0.75 * hp);

        // Circular arc parameters for lens shape
        double rArc = (a * a + b * b) / (2.0 * a);
        double xCenter = rArc - a;

        Point3D EvalNavette(double phi, double scale, double z)
        {
            double y = b * Math.Sin(phi);
            double cosVal = Math.Cos(phi);
            double sign = cosVal >= 0 ? 1.0 : -1.0;
            double disc = Math.Max(0.0, rArc * rArc - y * y);
            double x = sign * (-xCenter + Math.Sqrt(disc));
            return new Point3D(x * scale, y * scale, z);
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double starScale = tableScale + 0.5 * (1.0 - tableScale);
        double kScale = 0.22;

        var vertices = new List<Point3D>(41);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V16: Girdle vertices (16)
        for (int k = 0; k < 16; k++)
        {
            double phi = k * Math.PI / 8.0;
            vertices.Add(EvalNavette(phi, 1.0, zGirdle));
        }

        // V17..V24: Table (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = j * Math.PI / 4.0;
            vertices.Add(EvalNavette(phi, tableScale, zTable));
        }

        // V25..V32: Star vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(EvalNavette(phi, starScale, zStar));
        }

        // V33..V40: Lower girdle ridge vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(EvalNavette(phi, kScale, zK));
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
