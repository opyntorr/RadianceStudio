namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 59-facet Heart cut procedural mesh generator (aspect ratio 1.0).
/// Generates an authentic heart silhouette with top cleft and bottom apex point.
/// Features dual split-ridge bezels (at cleft j=2 and point j=6) yielding 59 facets.
/// </summary>
public class HeartMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Heart;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);
        double zStar = (g * 0.5) + 0.60 * hc;
        double zK = -((g * 0.5) + 0.75 * hp);

        static (double X, double Y) EvalRaw(double phi)
        {
            double t = (Math.PI * 0.5) - phi;
            while (t < 0.0) t += 2.0 * Math.PI;
            while (t >= 2.0 * Math.PI) t -= 2.0 * Math.PI;

            double sinT = Math.Sin(t);
            double cosT = Math.Cos(t);
            double cos2T = Math.Cos(2.0 * t);
            double cos3T = Math.Cos(3.0 * t);
            double cos4T = Math.Cos(4.0 * t);

            double x = 16.0 * sinT * sinT * sinT;
            double y = 13.0 * cosT - 5.0 * cos2T - 2.0 * cos3T - cos4T;
            return (x, y);
        }

        // Evaluate discrete girdle bounds across 16 vertices to normalize canonical dimensions
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        for (int k = 0; k < 16; k++)
        {
            double phi = k * Math.PI / 8.0;
            var (rx, ry) = EvalRaw(phi);
            if (rx < minX) minX = rx;
            if (rx > maxX) maxX = rx;
            if (ry < minY) minY = ry;
            if (ry > maxY) maxY = ry;
        }

        double spanX = maxX - minX;
        double spanY = maxY - minY;
        double midY = (maxY + minY) * 0.5;

        Point3D EvalHeart(double phi, double scale, double z)
        {
            var (rx, ry) = EvalRaw(phi);
            double normX = (rx / spanX) * diameterMm;
            double normY = ((ry - midY) / spanY) * diameterMm;

            return new Point3D(normX * scale, normY * scale, z);
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double starScale = tableScale + 0.5 * (1.0 - tableScale);
        double kScale = 0.22;

        var vertices = new List<Point3D>(41);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V16: Heart Girdle vertices (16)
        for (int k = 0; k < 16; k++)
        {
            double phi = k * Math.PI / 8.0;
            vertices.Add(EvalHeart(phi, 1.0, zGirdle));
        }

        // V17..V24: Table (octagon, 8)
        for (int j = 0; j < 8; j++)
        {
            double phi = j * Math.PI / 4.0;
            vertices.Add(EvalHeart(phi, tableScale, zTable));
        }

        // V25..V32: Star vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(EvalHeart(phi, starScale, zStar));
        }

        // V33..V40: Lower girdle ridge vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(EvalHeart(phi, kScale, zK));
        }

        var faces = new List<int[]>(59);

        // 1. Table Face (Face 0)
        faces.Add(new[] { 17, 18, 19, 20, 21, 22, 23, 24 });

        // 2. 8 Star Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + j, 17 + ((j + 1) % 8) });
        }

        // 3. 10 Bezel Faces (split at j=2 cleft and j=6 apex point into 2 triangles each)
        for (int j = 0; j < 8; j++)
        {
            if (j == 2) // Cleft at +Y: split quad [19, 26, 5, 27] into 2 triangles sharing ridge [19, 5]
            {
                faces.Add(new[] { 19, 26, 5 });
                faces.Add(new[] { 19, 5, 27 });
            }
            else if (j == 6) // Point at -Y: split quad [23, 30, 13, 31] into 2 triangles sharing ridge [23, 13]
            {
                faces.Add(new[] { 23, 30, 13 });
                faces.Add(new[] { 23, 13, 31 });
            }
            else
            {
                faces.Add(new[] { 17 + j, 25 + ((j + 7) % 8), 1 + 2 * j, 25 + j });
            }
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
