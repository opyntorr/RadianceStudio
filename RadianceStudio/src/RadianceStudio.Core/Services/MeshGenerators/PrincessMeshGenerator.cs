namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 49-facet Princess cut (square modified brilliant) procedural mesh generator.
/// </summary>
public class PrincessMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Princess;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double a = diameterMm * 0.5; // half-width
        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);
        double zStar = (g * 0.5) + 0.55 * hc;
        double zK = -((g * 0.5) + 0.70 * hp);

        double maxCornerRadius = a * Math.Sqrt(2.0) * (1.0 - 0.02); // 2% corner chamfer

        Point3D EvalSquarePerimeter(double phi, double scale, double z)
        {
            double cosVal = Math.Cos(phi);
            double sinVal = Math.Sin(phi);
            double denom = Math.Max(Math.Abs(cosVal), Math.Abs(sinVal));
            if (denom < 1e-12) denom = 1e-12;

            double r = a / denom;
            if (r > maxCornerRadius)
            {
                r = maxCornerRadius;
            }

            return new Point3D(r * cosVal * scale, r * sinVal * scale, z);
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double cornerChamfer = 0.02; // 2% corner chamfer per specs/gem_proportions.json

        // Exact analytical coordinates for strictly planar crown bezel facets:
        double tc = (zStar - zGirdle) / (zTable - zGirdle);
        double sx = a * (1.0 - tc * (1.0 - tableScale));
        double sy = sx * (1.0 - 2.0 * cornerChamfer);

        // Exact analytical coordinates for strictly planar pavilion main facets:
        double tp = (zK - zGirdle) / (zCulet - zGirdle);
        double kx = a * (1.0 - tp);
        double ky = kx * (1.0 - 2.0 * cornerChamfer);

        var vertices = new List<Point3D>(41);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V16: Girdle vertices (16)
        for (int k = 0; k < 16; k++)
        {
            double phi = k * Math.PI / 8.0;
            vertices.Add(EvalSquarePerimeter(phi, 1.0, zGirdle));
        }

        // V17..V24: Table (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = j * Math.PI / 4.0;
            vertices.Add(EvalSquarePerimeter(phi, tableScale, zTable));
        }

        // V25..V32: Star vertices (8) - strictly planar bezel quads
        var starOffsets = new (double X, double Y)[]
        {
            ( sx,  sy), // V25: j=0
            ( sy,  sx), // V26: j=1
            (-sy,  sx), // V27: j=2
            (-sx,  sy), // V28: j=3
            (-sx, -sy), // V29: j=4
            (-sy, -sx), // V30: j=5
            ( sy, -sx), // V31: j=6
            ( sx, -sy), // V32: j=7
        };
        for (int j = 0; j < 8; j++)
        {
            vertices.Add(new Point3D(starOffsets[j].X, starOffsets[j].Y, zStar));
        }

        // V33..V40: Lower girdle ridge vertices (8) - strictly planar pavilion main quads
        var kOffsets = new (double X, double Y)[]
        {
            ( kx,  ky), // V33: j=0
            ( ky,  kx), // V34: j=1
            (-ky,  kx), // V35: j=2
            (-kx,  ky), // V36: j=3
            (-kx, -ky), // V37: j=4
            (-ky, -kx), // V38: j=5
            ( ky, -kx), // V39: j=6
            ( kx, -ky), // V40: j=7
        };
        for (int j = 0; j < 8; j++)
        {
            vertices.Add(new Point3D(kOffsets[j].X, kOffsets[j].Y, zK));
        }

        var faces = new List<int[]>(49);

        // 1. Table Face (1 face)
        faces.Add(new[] { 17, 18, 19, 20, 21, 22, 23, 24 });

        // 2. 8 Star Faces (8 faces)
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + j, 17 + ((j + 1) % 8) });
        }

        // 3. 8 Bezel Faces (8 faces)
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + ((j + 7) % 8), 1 + 2 * j, 25 + j });
        }

        // 4. 8 Upper Girdle Faces (8 faces)
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 25 + j, 1 + 2 * j, 1 + (2 * j + 1), 1 + ((2 * j + 2) % 16) });
        }

        // 5. 16 Lower Girdle Faces (16 faces)
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + j, 1 + (2 * j + 1) });
            faces.Add(new[] { 1 + (2 * j + 1), 33 + j, 1 + ((2 * j + 2) % 16) });
        }

        // 6. 8 Pavilion Main Faces (8 faces)
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + ((j + 7) % 8), 0, 33 + j });
        }

        return (vertices, faces);
    }
}
